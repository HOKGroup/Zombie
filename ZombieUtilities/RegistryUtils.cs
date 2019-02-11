using System;
using Microsoft.Win32;
using NLog;

namespace Zombie.Utilities
{
    public enum VersionType
    {
        Zombie,
        Revit
    }

    public static class RegistryUtils
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private const string defaultVersion = "0.0.0.0";
        private const string keyName = "CurrentVersion";
        private const string keyPath = @"SOFTWARE\Zombie";

        private const string zombieKeyName = "ZombieVersion";

        /// <summary>
        /// Sets the Startup path and arguments for ZombieService.
        /// </summary>
        public static void SetImagePath(ZombieSettings settings, string assemblyLocation)
        {
            var value = "\"" + assemblyLocation + "\"" + " \"" + settings.SettingsLocation +"\" \"" + settings.AccessToken + "\"";
            var rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Services\ZombieService", true);
            if (rk != null)
            {
                var existing = rk.GetValue("ImagePath");
                if (Equals(existing, value)) return;
            }

            rk?.SetValue("ImagePath", value);
            rk.Close();
        }

        /// <summary>
        /// Retrieves current version of Zombie from the Registry.
        /// </summary>
        /// <returns>Current Version of Zombie.</returns>
        public static string GetVersion(VersionType versionType)
        {
            try
            {
                string kName;
                switch (versionType)
                {
                    case VersionType.Zombie:
                        kName = zombieKeyName;
                        break;
                    case VersionType.Revit:
                        kName = keyName;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(versionType), versionType, null);
                }

                var key = Registry.LocalMachine.OpenSubKey(keyPath, true);
                if (key == null)
                {
                    // (Konrad) Key doesn't exist let's create it.
                    key = Registry.LocalMachine.CreateSubKey(keyPath);
                    key.SetValue(kName, defaultVersion);
                    key.Close();
                    return defaultVersion;
                }

                var version = key.GetValue(kName) as string;
                key.Close();
                return string.IsNullOrEmpty(version) ? defaultVersion : version;
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                return defaultVersion;
            }
        }

        /// <summary>
        /// Updates the version value in registry.
        /// </summary>
        /// <param name="version">Current installed version.</param>
        public static void SetZombieVersion(string version)
        {
            try
            {
                var key = Registry.LocalMachine.OpenSubKey(keyPath, true) ?? Registry.LocalMachine.CreateSubKey(keyPath);
                key.SetValue(keyName, version);
                key.Close();
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
            }
        }
    }
}
