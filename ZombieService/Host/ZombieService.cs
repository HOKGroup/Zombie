using System;
using System.Reflection;
using System.ServiceModel;
using NLog;
using Zombie.Utilities;
using ZombieUtilities.Client;

namespace ZombieService.Host
{
    public class GuiUpdateEventArgs : EventArgs
    {
        public GuiUpdate Update;
    }

    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerSession, 
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class ZombieService : IZombieService
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public delegate void GuiUpdateEventHandler(object sender, GuiUpdateEventArgs e);
        public static event GuiUpdateEventHandler GuiUpdateEvent;

        private IZombieContract ServiceCallback;
        private GuiUpdateEventHandler GuiUpdateHandler;

        public void Subscribe()
        {
            ServiceCallback = OperationContext.Current.GetCallbackChannel<IZombieContract>();
            GuiUpdateHandler = OnGuiUpdate;
            GuiUpdateEvent += GuiUpdateHandler;
        }

        public void Unsubscribe()
        {
            GuiUpdateEvent -= GuiUpdateHandler;
        }

        public void PublishGuiUpdate(GuiUpdate update)
        {
            var se = new GuiUpdateEventArgs
            {
                Update = update
            };
            GuiUpdateEvent(this, se);
        }

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
            return !settings.StoreSettings || SettingsUtils.StoreSettings(settings);
        }

        public void ExecuteUpdate()
        {
            // (Konrad) User requested an update now, so we can just change the interval
            // to start right away, and execute again in whatever time it was supposed to
            // execute afterwards
            var interval = FrequencyUtils.TimeSpanFromFrequency(Program.Settings.Frequency);
            Program.Runner.Timer.Change(TimeSpan.Zero, interval);
        }

        public void ChangeFrequency(Frequency frequency)
        {
            // (Konrad) Update Service Settings.
            Program.Settings.Frequency = frequency;

            // (Konrad) We should delay the execution by the new interval.
            var interval = FrequencyUtils.TimeSpanFromFrequency(frequency);
            Program.Runner.Timer.Change(interval, interval);
        }

        public void OnGuiUpdate(object sender, GuiUpdateEventArgs se)
        {
            ServiceCallback.GuiUpdate(se.Update);
        }
    }
}
