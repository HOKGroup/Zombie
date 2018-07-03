using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Zombie.Utilities;
using Zombie.Utilities.Wpf;

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

        #endregion

        public AssetViewModel(AssetObject asset)
        {
            Asset = asset;

            ShowContents = new RelayCommand(OnShowContents);
        }

        private void OnShowContents()
        {
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

                var contents = new List<AssetViewModel>();
                using (var zip = ZipFile.Open(filePath, ZipArchiveMode.Read))
                {
                    foreach (var asset in zip.Entries)
                    {
                        contents.Add(new AssetViewModel(new AssetObject {Name = asset.Name}));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

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
