#region References

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Zombie.Utilities;
using GongSolutions.Wpf.DragDrop;
using Zombie.Properties;
using DragDropEffects = System.Windows.DragDropEffects;
using IDropTarget = GongSolutions.Wpf.DragDrop.IDropTarget;

#endregion

namespace Zombie.Controls
{
    public class LocationsViewModel : ViewModelBase, IDropTarget
    {
        #region Properties

        public RelayCommand AddDirectoryPath { get; set; }
        public RelayCommand RemoveDirectoryPath { get; set; }

        private Location _locationObject;
        public Location LocationObject
        {
            get { return _locationObject; }
            set { _locationObject = value; RaisePropertyChanged(() => LocationObject); }
        }

        private ObservableCollection<AssetViewModel> _assets;
        public ObservableCollection<AssetViewModel> Assets
        {
            get { return _assets; }
            set { _assets = value; RaisePropertyChanged(() => Assets); }
        }

        private int _assetCount;
        public int AssetCount
        {
            get { return _assetCount; }
            set { _assetCount = value; RaisePropertyChanged(() => AssetCount); }
        }

        #endregion

        public LocationsViewModel(Location location, bool addPlaceholder = true)
        {
            LocationObject = location;

            if (addPlaceholder)
            {
                Assets = new ObservableCollection<AssetViewModel>
                {
                    new AssetViewModel(new AssetObject { Name = Resources.Asset_PlaceholderName })
                    {
                        Parent = this,
                        IsPlaceholder = true
                    }
                };
            }
            else
            {
                Assets = new ObservableCollection<AssetViewModel>();
            }

            Assets.CollectionChanged += AssetsOnCollectionChanged;

            AddDirectoryPath = new RelayCommand(OnAddDirectoryPath);
            RemoveDirectoryPath = new RelayCommand(OnRemoveDirectoryPath);
        }

        #region Command Handlers

        private void OnRemoveDirectoryPath()
        {
            Messenger.Default.Send(new LocationRemoved { Removed = this });
        }

        private void OnAddDirectoryPath()
        {
            var dialog = new FolderBrowserDialog
            {
                ShowNewFolderButton = true
            };
            var result = dialog.ShowDialog();

            LocationObject.DirectoryPath = result == DialogResult.OK
                ? FilePathUtils.ReplaceUserSpecificPath(dialog.SelectedPath)
                : dialog.SelectedPath;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Sets the Source Assets Count excluding Placeholders. Used by the UI to set the counter label.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssetsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            AssetCount = _assets.Count(x => !x.IsPlaceholder);
        }

        #endregion

        #region Utilities

        public void AddPlaceholder()
        {
            Assets.Add(new AssetViewModel(new AssetObject { Name = Resources.Asset_PlaceholderName })
            {
                Parent = this,
                IsPlaceholder = true
            });
        }

        public void RemovePlaceholder()
        {
            var p = Assets.FirstOrDefault(x => x.Asset.Name == Resources.Asset_PlaceholderName);
            if (p != null) Assets.Remove(p);
        }

        #endregion

        #region Drag and Drop Handlers

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            var sourceItem = dropInfo.Data as AssetViewModel;
            var targetItem = dropInfo.TargetItem as AssetViewModel;

            if (sourceItem == null || targetItem == null) return;
            if (sourceItem.Asset.Name == Resources.Asset_PlaceholderName)
            {
                dropInfo.Effects = DragDropEffects.None;
                return;
            }

            // (Konrad) Handle CTRL + Drag as copy.
            if ((dropInfo.KeyStates & DragDropKeyStates.ControlKey) != 0)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Copy;
                return;
            }
            
            // (Konrad) Handle Drag as move.
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Move;
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var sourceItem = dropInfo.Data as AssetViewModel;
            var targetItem = dropInfo.TargetItem as AssetViewModel;

            if (sourceItem == null || targetItem == null) return;
            if (sourceItem.Asset.Name == Resources.Asset_PlaceholderName) return;

            if ((dropInfo.KeyStates & DragDropKeyStates.ControlKey) != 0)
            {
                // (Konrad) It is being copied so no need to delete from source.
            }
            else
            {
                var sourceCollection = (ListCollectionView) dropInfo.DragInfo.SourceCollection;
                sourceCollection.Remove(sourceItem);

                // (Konrad) In case that we removed last item, let's add a placeholder.
                if (sourceCollection.Count == 0)
                {
                    sourceCollection.AddNewItem(
                        new AssetViewModel(new AssetObject {Name = Resources.Asset_PlaceholderName})
                        {
                            Parent = sourceItem.Parent,
                            IsPlaceholder = true
                        });
                }
            }

            sourceItem.Parent = targetItem.Parent;
            if (!targetItem.Parent.Assets.Contains(sourceItem))
            {
                // (Konrad) We need to create a new VM here otherwise all references point to the same object causing weird UI behavior.
                targetItem.Parent.Assets.Add(new AssetViewModel(sourceItem.Asset) { Parent = sourceItem.Parent });
            }

            // (Konrad) If we are adding an item to location, we can remove placeholder
            if (targetItem.Parent.Assets.Count >= 2)
            {
                targetItem.Parent.RemovePlaceholder();
            }
        }

        #endregion
    }
}
