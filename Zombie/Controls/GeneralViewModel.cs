#region References

using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Zombie.Utilities;

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

        #endregion

        public GeneralViewModel(ZombieModel model)
        {
            Model = model;

            SaveSettingsLocal = new RelayCommand(OnSaveSettingsLocal);
            SaveSettingsRemote = new RelayCommand(OnSaveSettingsRemote);
            FrequencyChanged = new RelayCommand(OnFrequencyChanged);
            PushToGitHub = new RelayCommand(OnPushToGitHub);
        }

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
                App.Client.ChangeFrequency(Model.Settings.Frequency);
            }
            catch (Exception e)
            {
                _logger.Fatal(e);
            }
        }

        #endregion
    }
}
