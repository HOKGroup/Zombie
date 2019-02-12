#region References

using System;
using System.ServiceModel;
using System.Windows;
using DesktopNotifications;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Zombie.Controls;
using Zombie.Utilities;
using ZombieService.Host;
using ZombieUtilities.Client;

#endregion

namespace Zombie
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public static ZombieSettings Settings { get; set; } = new ZombieSettings();
        public static ZombieServiceClient Client { get; set; }
        public static bool StopUpdates { get; set; }

        public delegate void GuiUpdateCallbackHandler(GuiUpdate update);
        public static event GuiUpdateCallbackHandler GuiUpdateCallbackEvent;

        [CallbackBehavior(UseSynchronizationContext = false)]
        public class ZombieServiceCallback : IZombieContract
        {
            public void GuiUpdate(GuiUpdate update)
            {
                GuiUpdateCallbackEvent(update);
            }
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            _logger.Info("Zombie is powering up...");
            var connected = false;
            try
            {
                var binding = ServiceUtils.CreateClientBinding(ServiceUtils.FreeTcpPort());
                var endpoint = new EndpointAddress(new Uri("http://localhost:8000/ZombieService/Service.svc"));
                var context = new InstanceContext(new ZombieServiceCallback());
                Client = new ZombieServiceClient(context, binding, endpoint);

                GuiUpdateCallbackHandler callbackHandler = OnGuiUpdate;
                GuiUpdateCallbackEvent += callbackHandler;

                Client.Open();
                Client.Subscribe();

                // (Konrad) Get latest settings from ZombieService
                Settings = Client.GetSettings();

                connected = true;
                _logger.Info("Successfully connected to Zombie Service at http://localhost:8000/ZombieService/Service.svc");
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex.Message);
            }

            // Register AUMID and COM server (for Desktop Bridge apps, this no-ops)
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<MyNotificationActivator>("HOK.Zombie");

            // Register COM server and activator type
            DesktopNotificationManagerCompat.RegisterActivator<MyNotificationActivator>();

            // (Konrad) Create the startup window
            var m = new ZombieModel(Settings);
            var vm = new ZombieViewModel(m);
            var view = new ZombieView
            {
                DataContext = vm
            };

            var show = true;
            if (e.Args.Length == 1)
                show = e.Args[0] == "show";

            vm.Startup(view, show);

            Messenger.Default.Send(connected
                ? new UpdateStatus {Message = "Successfully connected to ZombieService!"}
                : new UpdateStatus {Message = "Connection to ZombieService failed!"});
            _logger.Info("Zombie is up and running!");
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            try
            {
                Messenger.Default.Send(new UpdateStatus {Message = "Disconnecting from ZombieService..."});
                Client.Unsubscribe();
                Client.Close();
                _logger.Info("Exited Zombie!");
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex.Message);
                _logger.Info("Zombie pooped its pants while exiting.");
            }
        }

        private static void OnGuiUpdate(GuiUpdate update)
        {
            if (StopUpdates) return;
            Messenger.Default.Send(update);
        }
    }
}
