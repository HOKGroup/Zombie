#region References

using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Zombie.Utilities;
using Zombie.Utilities.Wpf;
using ZombieUtilities.Client;

#endregion

namespace Zombie.Controls
{
    public class GitHubViewModel : ViewModelBase
    {
        #region Properties

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private ZombieModel Model { get; set; }
        public RelayCommand Update { get; set; }
        public RelayCommand DownloadPrerelease { get; set; }
        public RelayCommand PushToGitHub { get; set; }

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

        public GitHubViewModel(ZombieSettings settings, ZombieModel model)
        {
            Settings = settings;
            Model = model;

            Update = new RelayCommand(OnUpdate);
            DownloadPrerelease = new RelayCommand(OnDownloadPrerelease);
            PushToGitHub = new RelayCommand(OnPushToGitHub);

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

        private void OnPushToGitHub()
        {
            Model.PushReleaseToGitHub(Settings);
        }

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

            Model.DownloadPreRelease(Settings);
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
