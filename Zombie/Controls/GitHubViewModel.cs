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

            RefreshConnection = new RelayCommand(OnRefreshConnection);

            Messenger.Default.Register<ReleaseDownloaded>(this, OnReleaseDownloaded);
        }

        #region Message Handlers

        private void OnReleaseDownloaded(ReleaseDownloaded obj)
        {
            Settings.LatestRelease = obj.Result == ConnectionResult.Failure
                ? null
                : obj.Release;
        }

        #endregion

        #region Command Handlers

        private void OnRefreshConnection()
        {
            Model.GetLatestRelease(Settings, false);
        }

        #endregion

    }
}
