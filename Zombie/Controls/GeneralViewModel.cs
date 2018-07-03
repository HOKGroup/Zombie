using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Zombie.Utilities;

namespace Zombie.Controls
{
    public class GeneralViewModel : ViewModelBase
    {
        public ZombieModel Model { get; set; }
        public RelayCommand SaveSettings { get; set; }
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

            SaveSettings = new RelayCommand(OnSaveSettings);
            FrequencyChanged = new RelayCommand(OnFrequencyChanged);
        }

        private void OnFrequencyChanged()
        {
            Messenger.Default.Send(new ChangeFrequency
            {
                Frequency = Settings.CheckFrequency
            });
        }

        private static void OnSaveSettings()
        {
            Messenger.Default.Send(new StoreSettings());
        }
    }
}
