using System;
using System.IO;
using NLog;

namespace Zombie.Utilities
{
    public static class FileUtils
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetZombieDownloadsDirectory()
        {
            var location = System.Reflection.Assembly.GetEntryAssembly().Location;
            var directoryPath = Path.GetDirectoryName(location);
            var dir = Path.Combine(directoryPath, "downloads");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            return dir;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public static string GetZombieTempDirectory()
        //{
        //    var tempDir = Path.Combine(Path.GetTempPath(), "Zombie");
        //    if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
        //    return tempDir;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static bool Copy(string source, string destination)
        {
            if (!File.Exists(source))
            {
                _logger.Error("File path doesn't exists.");
                return false;
            }
            try
            {
                File.SetAttributes(source, FileAttributes.Normal);
                File.Copy(source, destination, true);
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool DeleteFile(string filePath)
        {
            if (!File.Exists(filePath)) return true;

            try
            {
                File.Delete(filePath);
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
        /// <param name="path"></param>
        public static bool DeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                _logger.Error("Directory path doesn't exists.");
                return false;
            }
            try
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(path, true);
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                return false;
            }
            return true;
        }
    }
}
