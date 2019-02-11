using System.Security.Principal;

namespace ZombieUtilities
{
    public static class UserUtils
    {
        /// <summary>
        /// Checks if current user is an admin on this machine. Only admins have access to the Settings.
        /// </summary>
        /// <returns>True if current user is an Admin.</returns>
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
