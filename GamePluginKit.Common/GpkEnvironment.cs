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
using static System.Environment;

#if REFERENCES_UNITY
using UnityEngine;
#endif

namespace GamePluginKit
{
    public static class GpkEnvironment
    {
        public static string RootPath
        {
            get
            {
                var gpkDir = GetEnvironmentVariable("GamePluginKitDir", EnvironmentVariableTarget.User);

                if (Directory.Exists(gpkDir))
                    return gpkDir;

                var appData = GetFolderPath(SpecialFolder.LocalApplicationData);
                return Path.Combine(appData, "GamePluginKit");
            }
        }

        public static string ToolsPath => Path.Combine(RootPath, "Tools");

        public static string CorePath => Path.Combine(RootPath, "Core");

        public static string PluginsPath => Path.Combine(RootPath, "Plugins");

#if REFERENCES_UNITY
        public static string ManagedPath => Path.Combine(Application.dataPath, "Managed");

        public static string ModsPath => Path.Combine(Application.dataPath, "Mods");
#endif
    }
}
