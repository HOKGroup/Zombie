﻿using System;
using System.IO;
using System.Linq;
using System.Management;
using NLog;
using ZombieUtilities;

namespace Zombie.Utilities
{
    public static class FileUtils
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private static bool TryGetUserName(out string username)
        {
            var searcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem");
            var collection = searcher.Get();
            username = (string)collection.Cast<ManagementBaseObject>().First()["UserName"];

            if (!username.Contains("\\"))
                return !string.IsNullOrWhiteSpace(username);

            var parts = username.Split('\\');
            if (parts.Any()) username = parts.Last();

            return !string.IsNullOrWhiteSpace(username);
        }

        /// <summary>
        /// Retrieves a file path to Zombie downloads directory.
        /// Creates a new one if it doesn't exist already.
        /// </summary>
        /// <returns>File path to the directory.</returns>
        public static string GetZombieDownloadsDirectory()
        {
            string dir;
            if (TryGetUserName(out var username))
            {
                dir = Path.Combine(@"C:\Users", username, @"AppData\Roaming", @"Zombie\downloads");
            }
            else
            {
                var location = System.Reflection.Assembly.GetEntryAssembly().Location;
                var directoryPath = Path.GetDirectoryName(location);
                dir = Path.Combine(directoryPath, "Zombie", "downloads");
            }
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Copies a file. 
        /// </summary>
        /// <param name="source">Source file path.</param>
        /// <param name="destination">Destination file path.</param>
        /// <returns>True if file was copied successfully.</returns>
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
        /// Deletes a file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <returns>True if file was deleted or never existed.</returns>
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
        /// Deletes a directory and all its contents.
        /// </summary>
        /// <param name="path">File path to the directory.</param>
        /// <returns>Returns True if directory was deleted or never existed.</returns>
        public static bool DeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                _logger.Info("Directory path doesn't exists: " + path);
                return true;
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

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <param name="path">File path to the new directory.</param>
        /// <returns>True if directory was created.</returns>
        public static bool CreateDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
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
