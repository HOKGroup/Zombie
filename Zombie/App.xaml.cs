using System;
using System.ServiceModel;
using System.Windows;
using NLog;
using Zombie.Utilities;
using ZombieUtilities;

namespace Zombie
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public static ZombieSettings Settings { get; set; } = new ZombieSettings();
        public static ZombieDispatcher ZombieDispatcher { get; set; } = new ZombieDispatcher();
        public static bool ConnectionFailed { get; set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var pipeFactory = new ChannelFactory<IZombieTalker>(new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/PipeGetSettings"));
            ZombieDispatcher.GetSettingsTalker = pipeFactory.CreateChannel();

            var pipeFactory1 = new ChannelFactory<IZombieTalker>(new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/PipeSetSettings"));
            ZombieDispatcher.SetSettingsTalker = pipeFactory1.CreateChannel();

            var pipeFactory2 = new ChannelFactory<IZombieTalker>(new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/PipeExecuteUpdate"));
            ZombieDispatcher.ExecuteUpdateTalker = pipeFactory2.CreateChannel();

            var pipeFactory3 = new ChannelFactory<IZombieTalker>(new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/PipeChangeFrequency"));
            ZombieDispatcher.ChangeFrequencyTalker = pipeFactory3.CreateChannel();

            try
            {
                // (Konrad) Get latest settings from ZombieService
                Settings = ZombieDispatcher.GetSettingsTalker.GetSettings();
            }
            catch (Exception ex)
            {
                ConnectionFailed = true;
                _logger.Fatal(ex);
            }

            // (Konrad) Create the startup window
            var m = new ZombieModel();
            var vm = new ZombieViewModel(Settings, m);
            var view = new ZombieView
            {
                DataContext = vm
            };
            view.Show();
        }
    }

    public class ZombieDispatcher
    {
        public IZombieTalker GetSettingsTalker { get; set; }
        public IZombieTalker SetSettingsTalker { get; set; }
        public IZombieTalker ExecuteUpdateTalker { get; set; }
        public IZombieTalker ChangeFrequencyTalker { get; set; }
    }
}
