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
    public class GitHubViewModel : ViewModelBase
    {
        #region Properties

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public RelayCommand Update { get; set; }

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
