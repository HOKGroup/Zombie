#region References

using System;
using System.ServiceModel;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Zombie.Utilities;
using ZombieUtilities.Host;

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
        public class ZombieServiceCallback : IZombieServiceCallback
        {
            public void GuiUpdate(GuiUpdate update)
            {
                GuiUpdateCallbackEvent(update);
            }
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
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
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex.Message);
            }

            // (Konrad) Create the startup window
            var vm = new ZombieViewModel(Settings);
            var view = new ZombieView
            {
                DataContext = vm
            };
            view.Show();

            Messenger.Default.Send(connected
                ? new UpdateStatus { Message = "Successfully connected to ZombieService!" }
                : new UpdateStatus { Message = "Connection to ZombieService failed!" });
        }

        private static void OnGuiUpdate(GuiUpdate update)
        {
            if (StopUpdates) return;

            Messenger.Default.Send(update);
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
            }
        }
    }
}
