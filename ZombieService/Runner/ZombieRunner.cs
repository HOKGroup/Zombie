using System;
using System.Threading;
using NLog;
using Zombie.Utilities;
using ZombieService.Runner;

namespace Zombie
{
    public class ZombieRunner
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public readonly Timer Timer;

        public ZombieRunner(ZombieSettings settings)
        {
            // (Konrad) We can launch the updater immediately.
            var interval = FrequencyUtils.TimeSpanFromFrequency(settings.Frequency);
            Timer = new Timer(x =>
            {
                try
                {
                    RunnerUtils.GetLatestRelease(settings);
                }
                catch (Exception e)
                {
                    _logger.Fatal(e.Message);
                }
                
            }, null, TimeSpan.Zero, interval);
        }
    }
}
