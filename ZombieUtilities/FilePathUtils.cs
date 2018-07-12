using System;
using System.Linq;
using System.Management;

namespace Zombie.Utilities
{
    public static class FilePathUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string ReplaceUserSpecificPath(string filePath)
        {
            var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (filePath.StartsWith(userPath))
            {
                filePath = filePath.Replace(userPath, "%userpath%");
            }

            return filePath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string CreateUserSpecificPath(string filePath)
        {
            // (Konrad) Since WindowsService runs in a System context, most of the usual info like Environment.UserName
            // is not available. This method will return a user name in a Network\username format so we need to parse it.
            var searcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem");
            var networkUsername = (string)searcher.Get().Cast<ManagementBaseObject>().First()["UserName"];
            var user = networkUsername.Substring(networkUsername.LastIndexOf('\\') + 1);
            var userPath = @"C:\Users\" + user;
            if (filePath.StartsWith("%userpath%"))
            {
                filePath = filePath.Replace("%userpath%", userPath);
            }

            return filePath;
        }
    }
}
