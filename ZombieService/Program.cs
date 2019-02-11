using System.ServiceModel;
using System.ServiceProcess;
using Zombie;
using Zombie.Utilities;

namespace ZombieService
{
    public static class Program
    {
        public static ServiceHost Host;
        public static ZombieSettings Settings;
        public static ZombieRunner Runner;
        public static string RecentLog = "";
        public static ZombieService Service;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            Service = new ZombieService(args);
            var ServicesToRun = new ServiceBase[]
            {
                Service
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
