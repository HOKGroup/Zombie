using System.Windows.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Zombie.Utilities;

namespace Zombie.Controls
{
    public class GitHubViewModel : ViewModelBase
    {
        public ZombieModel Model { get; set; }
        public RelayCommand<UserControl> WindowClosed { get; set; }
        public RelayCommand RefreshConnection { get; set; }

        private ZombieSettings _settings;
        public ZombieSettings Settings
        {
            get { return _settings; }
            set { _settings = value; RaisePropertyChanged(() => Settings); }
        }

        public GitHubViewModel(ZombieSettings settings, ZombieModel model)
        {
            Settings = settings;
            Model = model;

            WindowClosed = new RelayCommand<UserControl>(OnWindowClosed);
            RefreshConnection = new RelayCommand(OnRefreshConnection);

            Messenger.Default.Register<ReleaseDownloaded>(this, OnReleaseDownloaded);
        }

        private void OnReleaseDownloaded(ReleaseDownloaded obj)
        {
            Settings.LatestRelease = obj.Result == ConnectionResult.Failure 
                ? null 
                : obj.Release;
        }

        private void OnRefreshConnection()
        {
            Model.RetrieveRelease(Settings);
        }

        private void OnWindowClosed(UserControl obj)
        {
            // (Konrad) Unregisters any Messanger handlers.
            Cleanup();
        }
    }
}
