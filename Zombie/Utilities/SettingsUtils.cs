using System;
using System.IO;
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
