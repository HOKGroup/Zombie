using System.Reflection;
using Microsoft.Win32;

namespace Zombie.Utilities
{
    public static class RegistryUtils
    {
        /// <summary>
        /// Sets the Startup path and arguments for Zombie.
        /// </summary>
        public static void SetStartup(ZombieSettings settings)
        {
            var value = "\"" + Assembly.GetExecutingAssembly().Location + "\"" + " \"" + settings.SettingsLocation +"\" \"" + settings.AccessToken + "\"";
            var rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (rk != null)
            {
                var existing = rk.GetValue("Zombie");
                if (Equals(existing, value)) return;
            }

            rk?.SetValue("Zombie", value);
            rk.Close();
        }
    }
}
