#region References

using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Zombie.Utilities;
using ZombieUtilities.Host;

#endregion

namespace Zombie.Controls
{
    public class GeneralViewModel : ViewModelBase
    {
        #region Properties

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public RelayCommand SaveSettingsLocal { get; set; }
        public RelayCommand SaveSettingsRemote { get; set; }
        public RelayCommand FrequencyChanged { get; set; }

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
