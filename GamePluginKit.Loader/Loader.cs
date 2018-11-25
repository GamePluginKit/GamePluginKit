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
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using Object = UnityEngine.Object;

using Mono.Cecil;
using GamePluginKit.API;

namespace GamePluginKit
{
    public static class Loader
    {
        static bool Initialized { get; set; }

        static HashSet<string> SearchPaths { get; } = new HashSet<string>();

        public static void Init()
        {
            if  (Initialized) return;
            else Initialized = true;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string gpkDirectory = Path.Combine(localAppData, "GamePluginKit");

            var coreDir   = Path.Combine(gpkDirectory, "Core");
            var globalDir = Path.Combine(gpkDirectory, "Plugins");

            // We also need to locate the game-specific plugin directory.
            // This is located in a subfolder within the game's data directory.
            var pluginDir = Path.Combine(Application.dataPath, "Mods");

            // Set up the search paths and register the assembly resolver
            SearchPaths.UnionWith(new[] { coreDir, globalDir, pluginDir });
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            // Initialize the root object, which each plugin's object is a child of
            var root = new GameObject("GamePluginKit Root").transform;

            // Root object should be persistent, and inactive while plugins are added
            Object.DontDestroyOnLoad(root.gameObject);

            // Load any plugins from the core assemblies, load global
            // plugins, and finally, load any game-specific plugins.
            LoadPluginAssemblies(root, Directory.GetFiles(coreDir,   "*.dll", SearchOption.AllDirectories));
            LoadPluginAssemblies(root, Directory.GetFiles(globalDir, "*.dll", SearchOption.AllDirectories));
            LoadPluginAssemblies(root, Directory.GetFiles(pluginDir, "*.dll", SearchOption.AllDirectories));
        }

        // It's important that this logic remains in a separate function,
        // otherwise the Mono.Cecil and GamePluginKit.API assemblies
        // will be resolved before the custom resolver is registered.
        static void LoadPluginAssemblies(Transform root, ICollection<string> filePaths)
        {
            using (var resolver = new DefaultAssemblyResolver())
            {
                foreach (string searchPath in SearchPaths)
                    resolver.AddSearchDirectory(searchPath);

                var readParams = new ReaderParameters
                {
                    AssemblyResolver = resolver
                };

                foreach (string filePath in filePaths)
                {
                    // Mono.Cecil is used to inspect the attributes for a very important reason.
                    // If a plugin assembly contains attributes that are not present in the game's
                    // assemblies (for example, TargetFrameworkAttribute when using the old Mono runtime)
                    // the game will throw an exception when calling Assembly.GetCustomAttributes.

                    using (var asmDef = AssemblyDefinition.ReadAssembly(filePath, readParams))
                    {
                        var assembly = Assembly.LoadFrom(filePath);

                        foreach (var attribute in asmDef.CustomAttributes)
                        {
                            try
                            {
                                // Ensure the attribute is a StartupBehaviourAttribute
                                var attrTypeName = Assembly.CreateQualifiedName(
                                    attribute.AttributeType.Resolve()?.Module.Assembly.FullName,
                                    attribute.AttributeType.FullName
                                );

                                if (attrTypeName != typeof(StartupBehaviourAttribute).AssemblyQualifiedName)
                                    continue;

                                // Retrieve the behaviour type
                                var typeRef   = attribute.ConstructorArguments[0].Value as TypeReference;
                                var typeName  = Assembly.CreateQualifiedName(typeRef.Module.Assembly.FullName, typeRef.FullName);
                                var behaviour = Type.GetType(typeName);

                                new GameObject(behaviour.Name, behaviour).transform.SetParent(root);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                        }
                    }
                }
            }
        }

        static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var assemblyFile = assemblyName.Name + ".dll";

            foreach (string directory in SearchPaths)
            {
                if (!Directory.Exists(directory))
                    continue;

                string assemblyPath = Path.Combine(directory, assemblyFile);

                if (!File.Exists(assemblyPath))
                    continue;

                var assembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
                var name     = new AssemblyName(assembly.FullName);

                if (name.Version == assemblyName.Version)
                    return Assembly.LoadFrom(assemblyPath);
            }

            return null;
        }
    }
}
