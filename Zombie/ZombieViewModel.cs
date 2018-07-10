#region References

using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Zombie.Controls;
using Zombie.Utilities;
using Zombie.Utilities.Wpf;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

#endregion

namespace Zombie
{
    public class ZombieViewModel : ViewModelBase
    {
        #region Properties

        private ZombieModel Model { get; set; }
        private bool Cancel { get; set; } = true;
        private TextBlock Control { get; set; }
        //public UpdateRunner Runner { get; set; }
        public ObservableCollection<TabItem> TabItems { get; set; }
        public RelayCommand WindowClosing { get; set; }
        public RelayCommand<Window> WindowLoaded { get; set; }
        public bool ConnectionFailed { get; set; }

        private ZombieSettings _settings;
        public ZombieSettings Settings
        {
            get { return _settings; }
            set { _settings = value; RaisePropertyChanged(() => Settings); }
        }

        #endregion

        public ZombieViewModel(ZombieSettings settings, ZombieModel model)
        {
            Settings = settings;
            Model = model;

            WindowClosing = new RelayCommand(OnWindowClosing);
            WindowLoaded = new RelayCommand<Window>(OnWindowLoaded);

            var gitHub = new TabItem { Content = new GitHubView { DataContext = new GitHubViewModel(Settings, model) }, Header = "GitHub" };
            var mappings = new TabItem { Content = new MappingsView { DataContext = new MappingsViewModel(Settings, model) }, Header = "Mappings" };
            var general = new TabItem { Content = new GeneralView { DataContext = new GeneralViewModel(Settings, model) }, Header = "General" };
            TabItems = new ObservableCollection<TabItem>
            {
                gitHub,
                mappings,
                general
            };

            Messenger.Default.Register<StoreSettings>(this, OnStoreSettings);
            Messenger.Default.Register<ChangeFrequency>(this, OnChangeFrequency);
            Messenger.Default.Register<UpdateStatus>(this, OnUpdateStatus);
        }

        #region Message Handlers

        private void OnUpdateStatus(UpdateStatus obj)
        {
            // (Konrad) Since the UpdateRunner runs on a pool thread we can't set UI controls from there.
            // One way to set their status is to use a Dispatcher that every UI control has.
            Control?.Dispatcher.Invoke(() => { StatusBarManager.StatusLabel.Text = obj.Status; });
        }

        private void OnStoreSettings(StoreSettings obj)
        {
            foreach (var tab in TabItems)
            {
                if (!(tab.Content is MappingsView)) continue;

                var content = (MappingsView)tab.Content;
                var context = (MappingsViewModel)content.DataContext;
                context.UpdateSettings();
            }

            var dialog = new SaveFileDialog
            {
                FileName = "ZombieSettings.json",
                DefaultExt = ".json",
                Filter = "JSON Files (.json)|*.json"
            };

            var result = dialog.ShowDialog();
            if (result != true)
            {
                StatusBarManager.StatusLabel.Text = "Zombie Settings not saved!";
                return;
            }

            var filePath = dialog.FileName;
            Settings.SettingsLocation = filePath;
            switch (obj.Type)
            {  
                case SettingsType.Local:
                    if (!SettingsUtils.StoreSettings(Settings, true))
                    {
                        StatusBarManager.StatusLabel.Text =
                            "Zombie Settings not saved! Make sure you have write access to chosen path.";
                        return;
                    }
                    break;
                case SettingsType.Remote:
                    if (!SettingsUtils.StoreSettings(Settings))
                    {
                        StatusBarManager.StatusLabel.Text =
                            "Zombie Settings not saved! Make sure you have write access to chosen path.";
                        return;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            StatusBarManager.StatusLabel.Text = "Zombie Settings safely stored!";
        }

        private void OnChangeFrequency(ChangeFrequency obj)
        {
            
        }

        #endregion

        #region Command Handlers

        private void OnWindowClosing()
        {
            foreach (var tab in TabItems)
            {
                if (!(tab.Content is MappingsView)) continue;

                var content = (MappingsView)tab.Content;
                var context = (MappingsViewModel)content.DataContext;
                context.UpdateSettings();
                break;
            }

            try
            {
                App.ZombieDispatcher.SetSettingsTalker.SetSettings(Settings);
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        private void OnWindowLoaded(Window win)
        {
            StatusBarManager.StatusLabel = ((ZombieView)win).statusLabel;
            StatusBarManager.ProgressBar = ((ZombieView)win).progressBar;

            // (Konrad) Store reference to UI Control. It will be needed when
            // settings status messages from a pool thread.
            Control = ((ZombieView) win).statusLabel;

            if (App.ConnectionFailed)
                StatusBarManager.StatusLabel.Text =
                    "Failed to connect to ZombieService. Make sure it's alive and kicking!";
        }

        #endregion
    }
}
