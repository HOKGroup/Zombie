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
using NLog;
using Zombie.Controls;
using Zombie.Utilities;
using Zombie.Utilities.Wpf;
using ZombieUtilities;
using ZombieUtilities.Client;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

#endregion

namespace Zombie
{
    public class ZombieViewModel : ViewModelBase
    {
        #region Properties

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private ZombieModel Model { get; set; }
        private bool Cancel { get; set; } = true;
        private TextBlock Control { get; set; }
        private Window Win { get; set; }
        public ObservableCollection<TabItem> TabItems { get; set; }
        public RelayCommand<CancelEventArgs> WindowClosing { get; set; }
        public RelayCommand<Window> WindowLoaded { get; set; }
        public RelayCommand<Window> StateChanged { get; set; }
        public bool ConnectionFailed { get; set; }
        public string Title { get; set; }

        #endregion

        public ZombieViewModel(ZombieModel model)
        {
            Model = model;
            Title = "Zombie v." + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            WindowClosing = new RelayCommand<CancelEventArgs>(OnWindowClosing);
            WindowLoaded = new RelayCommand<Window>(OnWindowLoaded);
            StateChanged = new RelayCommand<Window>(OnStateChanged);

            var gitHub = new TabItem
            {
                Content = new GitHubView {DataContext = new GitHubViewModel(model)},
                Header = "GitHub"
            };
            var mappings = new TabItem
            {
                Content = new MappingsView {DataContext = new MappingsViewModel(model)},
                Header = "Mappings"
            };
            var general = new TabItem
            {
                Content = new GeneralView {DataContext = new GeneralViewModel(model)},
                Header = "General"
            };
            TabItems = new ObservableCollection<TabItem>
            {
                gitHub,
                mappings,
                general
            };

            Messenger.Default.Register<StoreSettings>(this, OnStoreSettings);
            Messenger.Default.Register<GuiUpdate>(this, OnGuiUpdate);
            Messenger.Default.Register<UpdateStatus>(this, OnUpdateStatus);
        }

        private static void OnStateChanged(Window win)
        {
            if (win == null) return;
            if (win.WindowState == WindowState.Minimized)
                win.Hide();
        }

        #region Message Handlers

        /// <summary>
        /// This method handles all GUI updates coming from the ZombieService. Since we stored the
        /// reference to a control on this view model, it's a good place to set status manager updates
        /// from here. They have to be dispatched on a UI thread. 
        /// </summary>
        /// <param name="obj"></param>
        private void OnGuiUpdate(GuiUpdate obj)
        {
            // (Konrad) Since the UpdateRunner runs on a pool thread we can't set UI controls from there.
            // One way to set their status is to use a Dispatcher that every UI control has.

            switch (obj.Status)
            {
                case Status.Failed:
                    Control?.Dispatcher.Invoke(() => { StatusBarManager.StatusLabel.Text = obj.Message; });
                    break;
                case Status.Succeeded:
                    Model.Settings = obj.Settings;
                    Control?.Dispatcher.Invoke(() => { StatusBarManager.StatusLabel.Text = obj.Message; });
                    break;
                case Status.UpToDate:
                    Model.Settings = obj.Settings;
                    Control?.Dispatcher.Invoke(() => { StatusBarManager.StatusLabel.Text = obj.Message; });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

            if (obj.Type != SettingsType.GitHub)
            {
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
                Model.Settings.SettingsLocation = filePath;
            }
            
            switch (obj.Type)
            {  
                case SettingsType.Local:
                    if (!SettingsUtils.StoreSettings(Model.Settings, true))
                    {
                        StatusBarManager.StatusLabel.Text =
                            "Zombie Settings not saved! Make sure you have write access to chosen path.";
                        return;
                    }
                    break;
                case SettingsType.Remote:
                    if (!SettingsUtils.StoreSettings(Model.Settings))
                    {
                        StatusBarManager.StatusLabel.Text =
                            "Zombie Settings not saved! Make sure you have write access to chosen path.";
                        return;
                    }
                    break;
                case SettingsType.GitHub:
                    Model.PushSettingsToGitHub(Model.Settings);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            StatusBarManager.StatusLabel.Text = "Zombie Settings safely stored!";
        }

        #endregion

        #region Command Handlers

        private void OnWindowClosing(CancelEventArgs args)
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
                var unused = App.Client.SetSettings(Model.Settings);
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
            }

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

        private void OnUpdateStatus(UpdateStatus obj)
        {
            Control?.Dispatcher.Invoke(() => { StatusBarManager.StatusLabel.Text = obj.Message; });
        }

        #endregion

        #region Startup

        /// <summary>
        /// Method that will by default dock the Window in a System Menu and hide the window.
        /// </summary>
        /// <param name="win">Main Control Window.</param>
        /// <param name="show">If true window will be shown, otherwise only tray icon appears.</param>
        public void Startup(Window win, bool show = true)
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
            var exit = new System.Windows.Forms.MenuItem("Exit (Admin)");
            exit.Click += OnExit;

            var settings = new System.Windows.Forms.MenuItem("Settings (Admin)");
            settings.Click += OnSettings;

            ni.ContextMenu = new System.Windows.Forms.ContextMenu(new[] { exit, settings });


            Model.ShowNotification();
            if (Win == null || !UserUtils.IsAdministrator() || !show) return;

            Win.Show();
            Win.WindowState = WindowState.Normal;
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
