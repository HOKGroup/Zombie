#region References

using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Zombie.Utilities;
using ZombieUtilities.Client;

#endregion

namespace Zombie.Controls
{
    public class GeneralViewModel : ViewModelBase
    {
        #region Properties

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public ZombieModel Model { get; set; }
        public RelayCommand SaveSettingsLocal { get; set; }
        public RelayCommand SaveSettingsRemote { get; set; }
        public RelayCommand FrequencyChanged { get; set; }
        public RelayCommand PushToGitHub { get; set; }

        private ZombieSettings _settings;
        public ZombieSettings Settings
        {
            get { return _settings; }
            set { _settings = value; RaisePropertyChanged(() => Settings); }
        }

        #endregion

        public GeneralViewModel(ZombieSettings settings)
        {
            Settings = settings;

            SaveSettingsLocal = new RelayCommand(OnSaveSettingsLocal);
            SaveSettingsRemote = new RelayCommand(OnSaveSettingsRemote);
            FrequencyChanged = new RelayCommand(OnFrequencyChanged);
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
                case Status.UpToDate:
                    Settings = obj.Settings;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Command Handlers

        private static void OnPushToGitHub()
        {
            Messenger.Default.Send(new StoreSettings { Type = SettingsType.GitHub });
        }

        private static void OnSaveSettingsRemote()
        {
            Messenger.Default.Send(new StoreSettings { Type = SettingsType.Remote });
        }

        private static void OnSaveSettingsLocal()
        {
            Messenger.Default.Send(new StoreSettings { Type = SettingsType.Local });
        }

        private void OnFrequencyChanged()
        {
            try
            {
                App.Client.ChangeFrequency(Settings.Frequency);
            }
            catch (Exception e)
            {
                _logger.Fatal(e);
            }
        }

        #endregion
    }
}
