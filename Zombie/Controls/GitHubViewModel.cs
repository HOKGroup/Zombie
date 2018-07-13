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

        private bool _isPrereleaseMode;
        public bool IsPrereleaseMode
        {
            get { return _isPrereleaseMode; }
            set { _isPrereleaseMode = value; RaisePropertyChanged(() => IsPrereleaseMode); }
        }

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

        #region Message Handlers

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
                    Settings = obj.Settings;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Command Handlers

        private void OnDownloadPrerelease()
        {
            if (IsPrereleaseMode)
            {
                // (Konrad) If we are already in pre-release mode let's disable it.
                DisablePreReleaseMode();

                // (Konrad) Trigger manual update so that UI updates to whatever current settings are.
                OnUpdate();

                return;
            }

            // (Konrad) Enable pre-release mode. 
            StatusBarManager.StatusLabel.Text = "Entered Pre-Release Mode. Live updates from ZombieService will be aborted.";
            App.StopUpdates = true;
            IsPrereleaseMode = !IsPrereleaseMode;

            var prerelease = GitHubUtils.DownloadPreRelease(Settings);
            if (prerelease == null)
            {
                StatusBarManager.StatusLabel.Text = "Failed to download latest Pre-Release!";
                return;
            }

            StatusBarManager.StatusLabel.Text = "Found new Pre-Release! " + prerelease.TagName;
            Settings.LatestRelease = prerelease;

            Messenger.Default.Send(new GuiUpdate
            {
                Settings = Settings,
                Message = "Found new Pre-Release! " + prerelease.TagName,
                Status = Status.Succeeded
            });
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

        private void DisablePreReleaseMode()
        {
            IsPrereleaseMode = !IsPrereleaseMode;
            App.StopUpdates = false;
        }

        #endregion
    }
}
