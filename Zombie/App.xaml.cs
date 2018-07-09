using System.ServiceModel;
using System.Windows;
using Zombie.Utilities;
using ZombieUtilities;

namespace Zombie
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static ZombieSettings Settings { get; set; }
        public static ZombieDispatcher ZombieDispatcher { get; set; } = new ZombieDispatcher();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var pipeFactory = new ChannelFactory<IZombieTalker>(new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/PipeGetSettings"));
            ZombieDispatcher.GetSettingsTalker = pipeFactory.CreateChannel();

            var pipeFactory1 = new ChannelFactory<IZombieTalker>(new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/PipeSetSettings"));
            ZombieDispatcher.SetSettingsTalker = pipeFactory1.CreateChannel();

            // (Konrad) Get latest settings from ZombieService
            Settings = ZombieDispatcher.GetSettingsTalker.GetSettings();
            //var result = ZombieDispatcher.SetSettingsTalker.SetSettings(Settings);

            // (Konrad) Create the startup window
            var m = new ZombieModel();
            var vm = new ZombieViewModel(Settings, m)
            {
                Runner = new UpdateRunner(Settings, m)
            };
            var wnd = new ZombieView
            {
                DataContext = vm
            };
            vm.Win = wnd;
            wnd.Show();
        }
    }

    public class ZombieDispatcher
    {
        public IZombieTalker GetSettingsTalker { get; set; }
        public IZombieTalker SetSettingsTalker { get; set; }
    }
}
