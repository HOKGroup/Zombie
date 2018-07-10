using System.Reflection;
using System.ServiceModel;
using NLog;
using Zombie.Utilities;
using ZombieService.Runner;
using ZombieUtilities;

namespace ZombieService.Host
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ZombieTalker : IZombieTalker
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ZombieSettings GetSettings()
        {
            return Program.Settings;
        }

        public bool SetSettings(ZombieSettings settings)
        {
            // (Konrad) Update Service Settings.
            Program.Settings = settings;

            // (Konrad) Update Registry
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            RegistryUtils.SetImagePath(settings, assemblyLocation);

            // (Konrad) This flag is true for local settings only.
            if (!settings.StoreSettings || !SettingsUtils.StoreSettings(settings))
            {
                return false;
            }

            return true;
        }

        public void ExecuteUpdate()
        {
            RunnerUtils.GetLatestRelease(Program.Settings);
        }

        public void ChangeFrequency(Frequency frequency)
        {
            // (Konrad) Update Service Settings.
            Program.Settings.Frequency = frequency;

            // (Konrad) We should delay the execution by the new interval.
            var interval = FrequencyUtils.TimeSpanFromFrequency(frequency);
            Program.Runner.Timer.Change(interval, interval);
        }
    }
}
