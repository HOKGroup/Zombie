using System;
using System.Threading;
using NLog;
using Zombie.Utilities;

namespace Zombie
{
    public class UpdateRunner
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public readonly Timer Timer;

        public UpdateRunner(ZombieSettings settings, ZombieModel model)
        {
            // (Konrad) We can launch the updater immediately.
            var interval = FrequencyUtils.TimeSpanFromFrequency(settings.CheckFrequency);
            Timer = new Timer(x =>
            {
                try
                {
                    model.ProcessLatestRelease(settings);
                }
                catch (Exception e)
                {
                    _logger.Fatal(e.Message);
                }
                
            }, null, TimeSpan.Zero, interval);
        }
    }
}
