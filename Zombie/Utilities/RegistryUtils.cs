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
            var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            rk?.SetValue("Zombie",
                "\"" + Assembly.GetExecutingAssembly().Location + "\"" + " \"" + settings.SettingsLocation + "\", \"" +
                settings.AccessToken + "\"");
            rk.Close();
        }
    }
}
