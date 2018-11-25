// Copyright 2018 Benjamin Moir
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace GamePluginKit.Patcher
{
    class Program
    {
        const string LoaderAssemblyName = "GamePluginKit.Loader.dll";

        static readonly string[] EngineAssemblies = new[]
        {
            "UnityEngine.CoreModule.dll", // Modern Unity (2017.2+)
            "UnityEngine.dll"             // Legacy Unity
        };

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                // Batch mode, patch multiple games by passing
                // in the path of each one's data directory.
                foreach (string path in args)
                {
                    if (CheckDataDirectory(path))
                    {
                        try
                        {
                            PatchGame(path);
                        }
                        catch (Exception ex)
                        {
                            throw new PatchException(ex.Message, ex);
                        }
                    }
                }

                return;
            }

            using (var folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.ShowNewFolderButton = false;
                folderBrowser.Description         = "Please select the game's data folder.";

                for (;;)
                {
                    if (folderBrowser.ShowDialog() != DialogResult.OK)
                        return;

                    if (CheckDataDirectory(folderBrowser.SelectedPath))
                        break;
                    else
                        MessageBox.Show("Please select a valid data directory.", "Error");
                }

                try
                {
                    PatchGame(folderBrowser.SelectedPath);
                    MessageBox.Show("Game patched successfully.", "Success");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Game was not patched successfully. " + ex.Message, "Failure");
                    throw new PatchException(ex.Message, ex);
                }
            }
        }

        static void PatchGame(string dataDir)
        {
            string managedDir = Path.Combine(dataDir, "Managed");

            // First we'll need to determine which assembly to patch. Newer versions
            // of Unity use UnityEngine.CoreModule, while older versions use UnityEngine.
            var targetPaths = EngineAssemblies.Select(file => Path.Combine(managedDir, file));
            var targetPath  = targetPaths.FirstOrDefault(path => File.Exists(path));

            if (targetPath == null)
                throw new FileNotFoundException("Could not locate a UnityEngine assembly.");

            // Now we load the target assembly, taking care to ensure it is
            // loaded in memory, so that we can write over it later.
            var targetAssm = AssemblyDefinition.ReadAssembly(targetPath, new ReaderParameters
            {
                InMemory = true
            });

            // We also need to load the injection assembly, so we can inject
            // a call to it into the target assembly
            var injectAssm = AssemblyDefinition.ReadAssembly(LoaderAssemblyName);

            // Now we load the types we need for patching.
            // We inject a type initializer into the UnityEngine.GameObject class,
            // as this ensures it will execute as soon as the game starts.
            var targetType = targetAssm.MainModule.GetType("UnityEngine", "GameObject");
            var injectType = injectAssm.MainModule.GetType("GamePluginKit", "Loader");

            // Now we inject (or retrieve, if it already exists) a static constructor
            // into UnityEngine.GameObject, and locate the method we wish to inject
            // a call to.
            var cctor  = GetOrAddTypeInitializer(targetType);
            var method = injectType.Methods.FirstOrDefault(m => m.Name == "Init");

            // All that's left now is to inject the call and save our changes.
            AddUniqueCall(targetAssm.MainModule.ImportReference(method), cctor.Body);
            targetAssm.Write(targetPath);

            // We also need to copy the plugin loader assembly into the managed directory
            File.Copy(LoaderAssemblyName, Path.Combine(managedDir, LoaderAssemblyName), true);

            // And also make sure the Mods directory exists, for convenience
            if (!Directory.Exists(Path.Combine(dataDir, "Mods")))
                Directory.CreateDirectory(Path.Combine(dataDir, "Mods"));

            // We're done with these
            targetAssm.Dispose(); injectAssm.Dispose();
        }

        static bool CheckDataDirectory(string path)
        {
            string managedDir = Path.Combine(path, "Managed");

            if (!Directory.Exists(managedDir))
                return false;

            return EngineAssemblies.Any(file => File.Exists(Path.Combine(managedDir, file)));
        }

        static void AddUniqueCall(MethodReference method, MethodBody target)
        {
            if (target.Instructions
                .Where(m => m.OpCode == OpCodes.Call)
                .Any  (i => (i.Operand as MethodReference)?.FullName == method.FullName))
            {
                // a call to the method already exists
                return;
            }

            var il = target.GetILProcessor();
            var op = il.Create(OpCodes.Call, method);

            if (target.Instructions.Count == 0)
                il.Append(op);
            else
                il.InsertBefore(target.Instructions[0], op);
        }

        static MethodDefinition GetOrAddTypeInitializer(TypeDefinition type)
        {
            if (TryGetTypeInitializer(type, out var cctor))
                return cctor;

            cctor = CreateTypeInitializer(type.Module);
            type.Methods.Add(cctor);
            return cctor;
        }

        static MethodDefinition CreateTypeInitializer(ModuleDefinition module)
        {
            const MethodAttributes Attributes = 0
                | MethodAttributes.Static
                | MethodAttributes.SpecialName
                | MethodAttributes.RTSpecialName;

            var ret   = module.TypeSystem.Void;
            var cctor = new MethodDefinition(".cctor", Attributes, ret);
            var il    = cctor.Body.GetILProcessor();

            il.Append(il.Create(OpCodes.Ret));
            return cctor;
        }

        static bool TryGetTypeInitializer(TypeDefinition type, out MethodDefinition cctor)
        {
            cctor = type.Methods.FirstOrDefault(m => m.Name == ".cctor");
            return cctor != null;
        }
    }
}
