#region References

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Zombie.Utilities;
using Zombie.Utilities.Wpf;
using ZombieUtilities;

#endregion

namespace Zombie.Controls
{
    public class AssetViewModel : ViewModelBase
    {
        #region Properties

        public LocationsViewModel Parent { get; set; }
        public bool IsPlaceholder { get; set; }
        public RelayCommand ShowContents { get; set; }

        private AssetObject _asset;
        public AssetObject Asset
        {
            get { return _asset; }
            set { _asset = value; RaisePropertyChanged(() => Asset); }
        }

        private ObservableCollection<AssetViewModel> _contents = new ObservableCollection<AssetViewModel>();
        public ObservableCollection<AssetViewModel> Contents
        {
            get { return _contents; }
            set { _contents = value; RaisePropertyChanged(() => Contents); }
        }

        private bool _isContentVisible;
        public bool IsContentVisible
        {
            get { return _isContentVisible; }
            set { _isContentVisible = value; RaisePropertyChanged(() => IsContentVisible); }
        }

        private bool _isContent;
        public bool IsContent
        {
            get { return _isContent; }
            set { _isContent = value; RaisePropertyChanged(() => IsContent); }
        }

        #endregion

        public AssetViewModel(AssetObject asset)
        {
            Asset = asset;

            ShowContents = new RelayCommand(OnShowContents);
        }

        #region Command Handlers

        private void OnShowContents()
        {
            IsContentVisible = !IsContentVisible;

            if (Contents.Any()) return;

            var dir = Path.Combine(Directory.GetCurrentDirectory(), "downloads");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            try
            {
                var filePath = Path.Combine(dir, Asset.Name);

                // download
                GitHubUtils.DownloadAssets(App.Settings, Asset.Url, filePath);

                // verify
                if (!File.Exists(filePath))
                {
                    StatusBarManager.StatusLabel.Text = "Could not retrieve contents of the Asset!";
                    return;
                }

                using (var zip = ZipFile.Open(filePath, ZipArchiveMode.Read))
                {
                    foreach (var asset in zip.Entries)
                    {
                        if (asset.FullName.Contains("/"))
                        {
                            // (Konrad) Skip files inside of folders.
                            continue;
                        }

                        if (asset.Name == string.Empty)
                        {
                            // (Konrad) It's a directory
                            Contents.Add(new AssetViewModel(new AssetObject
                            {
                                Name = asset.FullName.TrimLastCharacter("/")
                            }) { IsContent = true });
                        }

                        Contents.Add(new AssetViewModel(new AssetObject
                        {
                            Name = asset.Name
                        }) { IsContent = true });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion

        public override bool Equals(object obj)
        {
            var item = obj as AssetViewModel;
            return item != null && Asset.Id.Equals(item.Asset.Id);
        }

        public override int GetHashCode()
        {
            return Asset.Id.GetHashCode();
        }
    }
}
