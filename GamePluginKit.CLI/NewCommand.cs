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
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using System.Reflection;
using System.Xml;

namespace GamePluginKit.CLI
{
    [Command(Description = "Create a new GPK project")]
    class NewCommand
    {
        [DirectoryExists]
        [Required(ErrorMessage = "Game data path must be specified.")]
        [Option(Description    = "The game's data directory", ShortName = "g")]
        string DataDir { get; }

        [LegalFilePath]
        [Required(ErrorMessage   = "Project name must be specified.")]
        [Argument(0, Description = "Name of the project")]
        string ProjectName { get; }

        [LegalFilePath]
        [Option(Description = "Location to place the generated output")]
        string Output { get; }

        [Option(Description = "The TargetFramework to use")]
        string Framework { get; } = "net46";

        void OnExecute(IConsole console)
        {
            string outputDir = new Uri(Path.GetFullPath(Output ?? ProjectName)).AbsolutePath.TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar
            );

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            if (Directory.GetFiles(outputDir).Length != 0)
            {
                console.ForegroundColor = ConsoleColor.Red;
                console.Error.WriteLine("Output directory must be empty.");
                console.ResetColor();
                return;
            }

            var csproj = GenerateCSProj(Path.Combine(DataDir, "Managed"));
            csproj.Save(Path.Combine(outputDir, ProjectName) + ".csproj");

            var assembly   = Assembly.GetExecutingAssembly();
            var resource   = "GamePluginKit.CLI.EmbeddedResources.PluginTemplate.cs";
            var sourceFile = Path.Combine(outputDir, ProjectName) + ".cs";

            using (var stream = assembly.GetManifestResourceStream(resource))
            using (var output = File.OpenWrite(sourceFile))
                stream.CopyTo(output);
        }

        XmlDocument GenerateCSProj(string managedDir)
        {
            var doc        = new XmlDocument();
            var project    = doc    .AppendChild(doc.CreateElement("Project"))       as XmlElement;
            var properties = project.AppendChild(doc.CreateElement("PropertyGroup")) as XmlElement;
            var items      = project.AppendChild(doc.CreateElement("ItemGroup"))     as XmlElement;

            project.SetAttribute("Sdk", "Microsoft.NET.Sdk");
            properties.AppendChild(doc.CreateElement("OutputType"     )).InnerText = "Library";
            properties.AppendChild(doc.CreateElement("TargetFramework")).InnerText = Framework;
            properties.AppendChild(doc.CreateElement("NoStdLib"       )).InnerText = "True";
            properties.AppendChild(doc.CreateElement("ManagedDir"     )).InnerText = managedDir;

            var package = doc.CreateElement("PackageReference");
            package.SetAttribute("Include", "GamePluginKit.API");
            package.SetAttribute("Version", "*");
            items.AppendChild(package);

            foreach (string filePath in Directory.EnumerateFiles(managedDir, "*.dll"))
            {
                string referenceName = Path.GetFileNameWithoutExtension(filePath);
                string fileName      = Path.GetFileName(filePath);

                var reference = doc.CreateElement("Reference");
                reference.SetAttribute("Include", referenceName);
                reference.AppendChild(doc.CreateElement("HintPath")).InnerText = $"$(ManagedDir)\\{fileName}";
                reference.AppendChild(doc.CreateElement("Private" )).InnerText = "False";

                items.AppendChild(reference);
            }

            return doc;
        }
    }
}
