using Microsoft.Win32;

namespace Zombie.Utilities
{
    public static class RegistryUtils
    {
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
    }
}
