using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Zombie.Utilities;

namespace Zombie.Controls
{
    public class GeneralViewModel : ViewModelBase
    {
        public ZombieModel Model { get; set; }
        public RelayCommand SaveSettingsLocal { get; set; }
        public RelayCommand SaveSettingsRemote { get; set; }
        public RelayCommand FrequencyChanged { get; set; }

        private ZombieSettings _settings;
        public ZombieSettings Settings
        {
            get { return _settings; }
            set { _settings = value; RaisePropertyChanged(() => Settings); }
        }

        public GeneralViewModel(ZombieSettings settings, ZombieModel model)
        {
            Settings = settings;
            Model = model;

            SaveSettingsLocal = new RelayCommand(OnSaveSettingsLocal);
            SaveSettingsRemote = new RelayCommand(OnSaveSettingsRemote);
            FrequencyChanged = new RelayCommand(OnFrequencyChanged);
        }

        #region Command Handlers

        private static void OnSaveSettingsRemote()
        {
            Messenger.Default.Send(new StoreSettings { Type = SettingsType.Remote });
        }

        private void OnFrequencyChanged()
        {
            Messenger.Default.Send(new ChangeFrequency
            {
                Frequency = Settings.CheckFrequency
            });
        }

        private static void OnSaveSettingsLocal()
        {
            Messenger.Default.Send(new StoreSettings { Type = SettingsType.Local });
        }

        #endregion
    }
}
