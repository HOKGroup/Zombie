#region References

using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Zombie.Utilities;
using Zombie.Utilities.Wpf;
using ZombieUtilities.Host;

#endregion

namespace Zombie.Controls
{
    public class GitHubViewModel : ViewModelBase
    {
        #region Properties

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public RelayCommand Update { get; set; }
        public RelayCommand DownloadPrerelease { get; set; }

        private ZombieSettings _settings;
        public ZombieSettings Settings
        {
            get { return _settings; }
            set { _settings = value; RaisePropertyChanged(() => Settings); }
        }

        #endregion

        public GitHubViewModel(ZombieSettings settings)
        {
            Settings = settings;

            Update = new RelayCommand(OnUpdate);
            DownloadPrerelease = new RelayCommand(OnDownloadPrerelease);

            Messenger.Default.Register<GuiUpdate>(this, OnGuiUpdate);
        }

        private void OnGuiUpdate(GuiUpdate obj)
        {
            switch (obj.Status)
            {
                case Status.Failed:
                    break;
                case Status.Succeeded:
                    Settings = obj.Settings;
                    break;
                case Status.UpToDate:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region Command Handlers

        private void OnDownloadPrerelease()
        {
            var prerelease = GitHubUtils.DownloadPreRelease(Settings);
            if (prerelease == null)
            {
                StatusBarManager.StatusLabel.Text = "Failed to download latest Pre-Release!";
                return;
            }

            Settings.LatestRelease = prerelease;
        }

        private static void OnUpdate()
        {
            try
            {
                App.Client.ExecuteUpdate();
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
            }
        }

        #endregion
    }
}
