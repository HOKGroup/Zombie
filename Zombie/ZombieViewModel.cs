#region References

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
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
        private UpdateRunner Runner { get; set; }
        private Window Win { get; set; }
        private bool Cancel { get; set; } = true;
        private TextBlock Control { get; set; }
        public ObservableCollection<TabItem> TabItems { get; set; }
        public RelayCommand<CancelEventArgs> WindowClosing { get; set; }
        public RelayCommand<Window> WindowLoaded { get; set; }

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

            WindowClosing = new RelayCommand<CancelEventArgs>(OnWindowClosing);
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
            if (!Model.StoreSettings(Settings, filePath, true))
            {
                StatusBarManager.StatusLabel.Text =
                    "Zombie Settings not saved! Make sure you have write access to chosen path.";
                return;
            }

            StatusBarManager.StatusLabel.Text = "Zombie Settings safely stored!";
        }

        private void OnChangeFrequency(ChangeFrequency obj)
        {
            var interval = FrequencyUtils.TimeSpanFromFrequency(obj.Frequency);

            // (Konrad) We should delay the execution by the new interval.
            Runner.Timer.Change(interval, interval);
        }

        #endregion

        #region Command Handlers

        private void OnWindowClosing(CancelEventArgs args)
        {
            // (Konrad) If Remote Settings were used
            // We should not be saving them locally but make sure that Startup is up to date.
            if (!Settings.StoreSettings)
            {
                RegistryUtils.SetStartup(Settings);

                if (!Cancel)
                {
                    Cleanup(); // removes Messenger bindings
                    return; // closes Window
                }

                args.Cancel = true;
                Win.Hide();
                return;
            }

            foreach (var tab in TabItems)
            {
                if (!(tab.Content is MappingsView)) continue;

                var content = (MappingsView)tab.Content;
                var context = (MappingsViewModel)content.DataContext;
                context.UpdateSettings();
                break;
            }

            Model.StoreSettings(Settings, Settings.SettingsLocation);
            RegistryUtils.SetStartup(Settings);

            if (!Cancel)
            {
                Cleanup(); // removes Messenger bindings
                return; // closes Window
            }

            args.Cancel = true;
            Win.Hide();
        }

        private void OnWindowLoaded(Window win)
        {
            StatusBarManager.StatusLabel = ((ZombieView)win).statusLabel;
            StatusBarManager.ProgressBar = ((ZombieView)win).progressBar;

            // (Konrad) Store reference to UI Control. It will be needed when
            // settings status messages from a pool thread.
            Control = ((ZombieView) win).statusLabel;
        }

        #endregion

        #region Startup

        /// <summary>
        /// Method that will by default dock the Window in a System Menu and hide the window.
        /// </summary>
        /// <param name="win">Main Control Window.</param>
        public void Startup(Window win)
        {
            Win = win;

            var ni = new System.Windows.Forms.NotifyIcon();
            var sri = Application.GetResourceStream(
                new Uri("pack://application:,,,/Resources/iconsZombie.ico"));
            if (sri != null)
            {
                using (var s = sri.Stream)
                {
                    ni.Icon = new Icon(s);
                }
            }
            ni.Visible = true;
            ni.DoubleClick += delegate
            {
                if (!UserUtils.IsAdministrator()) return;

                win.Show();
                win.WindowState = WindowState.Normal;
            };

            // (Konrad) Add context menu. We are using Forms namespaces here.
            var exit = new System.Windows.Forms.MenuItem("Exit");
            exit.Click += OnExit;

            var settings = new System.Windows.Forms.MenuItem("Settings (Admin)");
            settings.Click += OnSettings;

            ni.ContextMenu = new System.Windows.Forms.ContextMenu(new[] { exit, settings });

            // (Konrad) Launch the UpdateRunner to check for updates.
            Runner = new UpdateRunner(Settings, Model);
        }

        private void OnSettings(object sender, EventArgs e)
        {
            if (Win == null || !UserUtils.IsAdministrator()) return;

            Win.Show();
            Win.WindowState = WindowState.Normal;
        }

        private void OnExit(object sender, EventArgs e)
        {
            Cancel = false;
            Win?.Close();
        }

        #endregion
    }
}
