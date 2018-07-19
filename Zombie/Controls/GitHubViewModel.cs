#region References

using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Zombie.Utilities;
using Zombie.Utilities.Wpf;

#endregion

namespace Zombie.Controls
{
    public class GitHubViewModel : ViewModelBase
    {
        #region Properties

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public ZombieModel Model { get; set; }
        public RelayCommand Update { get; set; }
        public RelayCommand DownloadPrerelease { get; set; }
        public RelayCommand PushToGitHub { get; set; }

        private bool _isPrereleaseMode;
        public bool IsPrereleaseMode
        {
            get { return _isPrereleaseMode; }
            set { _isPrereleaseMode = value; RaisePropertyChanged(() => IsPrereleaseMode); }
        }

        #endregion

        public GitHubViewModel(ZombieModel model)
        {
            Model = model;

            Update = new RelayCommand(OnUpdate);
            DownloadPrerelease = new RelayCommand(OnDownloadPrerelease);
            PushToGitHub = new RelayCommand(OnPushToGitHub);

            Messenger.Default.Register<PrereleaseDownloaded>(this, OnPrereleaseDownloaded);
        }

        #region MessageHandlers

        private void OnPrereleaseDownloaded(PrereleaseDownloaded obj)
        {
            switch (obj.Status)
            {
                case PrereleaseStatus.Found:
                    StatusBarManager.StatusLabel.Text = "Entered Pre-Release Mode. Live updates from ZombieService will be aborted.";
                    Model.Settings = obj.Settings;
                    App.StopUpdates = true;
                    IsPrereleaseMode = !IsPrereleaseMode;
                    break;
                case PrereleaseStatus.Failed:
                    StatusBarManager.StatusLabel.Text = "Could not find any Pre-Releases!";
                    App.StopUpdates = false;
                    IsPrereleaseMode = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Command Handlers

        private void OnPushToGitHub()
        {
            Model.PushReleaseToGitHub(Model.Settings);
        }

        private void OnDownloadPrerelease()
        {
            if (IsPrereleaseMode)
            {
                // (Konrad) If we are already in pre-release mode let's disable it.
                DisablePreReleaseMode();

                try
                {
                    Model.Settings = App.Client.GetSettings();
                }
                catch (Exception e)
                {
                    _logger.Fatal(e.Message);
                    StatusBarManager.StatusLabel.Text = "Failed to retrieve Zombie Settings from ZombieService!";
                }

                return;
            }

            Model.DownloadPreRelease(Model.Settings);
        }

        private void OnUpdate()
        {
            // (Konrad) Disable pre-release mode.
            if (IsPrereleaseMode) DisablePreReleaseMode();

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

        #region Utilities

        private void DisablePreReleaseMode()
        {
            IsPrereleaseMode = !IsPrereleaseMode;
            App.StopUpdates = false;
        }

        #endregion
    }
}
