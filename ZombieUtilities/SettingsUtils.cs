using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using NLog;

namespace Zombie.Utilities
{
    public static class SettingsUtils
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static ZombieSettings SetSettings(IReadOnlyList<string> args)
        {
            ZombieSettings result;
            var arg1 = args.Any() ? args[0] : string.Empty;
            var arg2 = args.Count > 1 ? args[1] : string.Empty;

            var local = File.Exists(arg1);
            if (!string.IsNullOrEmpty(arg1) && !local)
            {
                // (Konrad) Arg1 is a path that doesn't exist locally so it is likely
                // a remote file location (HTTP).
                if (TryGetRemoteSettings(arg1, out var settings))
                {
                    result = settings;
                    result.AccessToken = arg2;
                    result.SettingsLocation = arg1;
                    result.StoreSettings = false;
                }
                else
                {
                    // (Konrad) We have a path in the Arg1 that doesn't exist or failed to 
                    // deserialize so we can treat it as if it didn't exist and override it on close.
                    result = new ZombieSettings
                    {
                        SettingsLocation = arg1,
                        StoreSettings = true
                    };
                }
            }
            else if (!string.IsNullOrEmpty(arg1) && local)
            {
                // (Konrad) Arg1 exists on a user drive or network drive.
                result = TryGetStoredSettings(arg1, out var settings)
                    ? settings
                    : new ZombieSettings();
                result.SettingsLocation = arg1;

                // (Konrad) If AccessToken was in the Settings file we can skip this.
                // If it wasn't it should be set with the Arg2
                if (string.IsNullOrEmpty(result.AccessToken)) result.AccessToken = arg2;
            }
            else
            {
                result = new ZombieSettings
                {
                    SettingsLocation = Path.Combine(Directory.GetCurrentDirectory(), "ZombieSettings.json"),
                    StoreSettings = true
                };
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static bool TryGetRemoteSettings(string path, out ZombieSettings settings)
        {
            settings = null;
            var filePath = Path.Combine(Path.GetTempPath(), "ZombieSettings.json");

            // (Konrad) Remove the existing file.
            // WebClient will not override it. 
            // If we can't delete it we might as well jump back since it won't be overriden. 
            if (!FileUtils.DeleteFile(filePath)) return false;

            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFile(path, filePath);
                }
                catch (Exception e)
                {
                    _logger.Fatal(e.Message);
                    return false;
                }
            }

            if (!File.Exists(filePath)) return false;

            try
            {
                var json = File.ReadAllText(filePath);
                var obj = JsonConvert.DeserializeObject<ZombieSettings>(json);
                settings = obj;
                return true;
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static bool TryGetStoredSettings(string filePath, out ZombieSettings settings)
        {
            settings = null;
            try
            {
                var json = File.ReadAllText(filePath);
                var obj = JsonConvert.DeserializeObject<ZombieSettings>(json);
                settings = obj;
                return true;
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                return false;
            }
        }
    }
}
