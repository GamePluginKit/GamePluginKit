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
