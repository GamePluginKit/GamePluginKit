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
using System.Xml;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

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

        string DetectFramework(string managedDir)
        {
            // todo: netstandard games

            var target = new Version(3, 5);

            foreach (string filePath in Directory.EnumerateFiles(managedDir, "*.dll"))
            {
                var assembly = Assembly.ReflectionOnlyLoadFrom(filePath);

                if (!Version.TryParse(assembly.ImageRuntimeVersion.Substring(1), out var version))
                    continue;

                if (version > target)
                    target = version;
            }

            return $"net{target.Major}{target.Minor}";
        }

        XmlDocument GenerateCSProj(string managedDir)
        {
            var doc     = new XmlDocument();
            var project = doc    .AppendChild(doc.CreateElement("Project"))       as XmlElement;
            var props   = project.AppendChild(doc.CreateElement("PropertyGroup")) as XmlElement;
            var items   = project.AppendChild(doc.CreateElement("ItemGroup"))     as XmlElement;

            project.SetAttribute("Sdk", "Microsoft.NET.Sdk");
            props.AppendChild(doc.CreateElement("OutputType")).InnerText = "Library";
            props.AppendChild(doc.CreateElement("ManagedDir")).InnerText = managedDir;

            props.AppendChild(doc.CreateElement("TargetFramework"      )).InnerText = DetectFramework(managedDir);
            props.AppendChild(doc.CreateElement("FrameworkPathOverride")).InnerText = "$(ManagedDir)";

            props.AppendChild(doc.CreateElement("GenerateTargetFrameworkAttribute")).InnerText = "False";

            var package = doc.CreateElement("PackageReference");
            package.SetAttribute("Include", "GamePluginKit.API");
            package.SetAttribute("Version", "*");
            items.AppendChild(package);

            var reference = doc.CreateElement("Reference");
            reference.SetAttribute("Include", "$(ManagedDir)\\*.dll");
            reference.SetAttribute("Private", "False");
            items.AppendChild(reference);

            return doc;
        }
    }
}
