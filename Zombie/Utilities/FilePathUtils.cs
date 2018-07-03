using System;

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
            var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (filePath.StartsWith("%userpath%"))
            {
                filePath = filePath.Replace("%userpath%", userPath);
            }

            return filePath;
        }
    }
}
