#region References

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using NLog;
using Zombie.Utilities;

#endregion

namespace Zombie
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public static ZombieSettings Settings { get; set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var arg1 = GetRemoteSettingsPath(e.Args);
            var local = File.Exists(arg1);
            if (!string.IsNullOrEmpty(arg1) && !local)
            {
                // (Konrad) Arg1 is a path that doesn't exist locally so it is likely
                // a remote file location (HTTP).
                if (SettingsUtils.TryGetRemoteSettings(arg1, out var settings))
                {
                    Settings = settings;
                    Settings.AccessToken = GetAccessToken(e.Args);
                    Settings.SettingsLocation = arg1;
                    Settings.StoreSettings = false;
                }
                else
                {
                    // (Konrad) We have a path in the Arg1 that doesn't exist or failed to 
                    // deserialize so we can treat it as if it didn't exist and override it on close.
                    Settings = new ZombieSettings
                    {
                        SettingsLocation = arg1,
                        StoreSettings = true
                    };
                }
            }
            else if (!string.IsNullOrEmpty(arg1) && local)
            {
                // (Konrad) Arg1 exists on a user drive or network drive.
                Settings = SettingsUtils.TryGetStoredSettings(arg1, out var settings)
                    ? settings
                    : new ZombieSettings();
                Settings.SettingsLocation = arg1;

                // (Konrad) If AccessToken was in the Settings file we can skip this.
                // If it wasn't it should be set with the Arg2
                if (string.IsNullOrEmpty(Settings.AccessToken)) Settings.AccessToken = GetAccessToken(e.Args);
            }
            else
            {
                Settings = new ZombieSettings
                {
                    SettingsLocation = Path.Combine(Directory.GetCurrentDirectory(), "ZombieSettings.json"),
                    StoreSettings = true
                };
            }

            // Create the startup window
            var m = new ZombieModel();
            var vm = new ZombieViewModel(Settings, m);
            var wnd = new ZombieView
            {
                DataContext = vm
            };
            
            wnd.ShowInTaskbar = true;
            vm.Startup(wnd);
        }

        #region Utilities

        private static string GetRemoteSettingsPath(IReadOnlyList<string> args)
        {
            return args.Any() ? args[0] : string.Empty;
        }

        private static string GetAccessToken(IReadOnlyList<string> args)
        {
            return args.Count > 1 ? args[1] : string.Empty;
        }

        #endregion
    }
}
