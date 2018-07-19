#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Zombie.Utilities;
using ZombieUtilities.Client;

#endregion

namespace Zombie.Controls
{
    public class MappingsViewModel : ViewModelBase
    {
        #region Properties

        public ZombieModel Model { get; set; }
        public RelayCommand AddLocation { get; set; }

        private readonly object _sourceLock = new object();
        private ObservableCollection<LocationsViewModel> _sourceLocations = new ObservableCollection<LocationsViewModel>();
        public ObservableCollection<LocationsViewModel> SourceLocations
        {
            get { return _sourceLocations; }
            set { _sourceLocations = value; RaisePropertyChanged(() => SourceLocations); }
        }

        private readonly object _lock = new object();
        private ObservableCollection<LocationsViewModel> _locations = new ObservableCollection<LocationsViewModel>();
        public ObservableCollection<LocationsViewModel> Locations
        {
            get { return _locations; }
            set { _locations = value; RaisePropertyChanged(() => Locations); }
        }

        #endregion

        public MappingsViewModel(ZombieModel model)
        {
            Model = model;

            BindingOperations.EnableCollectionSynchronization(_sourceLocations, _sourceLock);
            BindingOperations.EnableCollectionSynchronization(_locations, _lock);

            AddLocation = new RelayCommand(OnAddLocation);

            Messenger.Default.Register<LocationRemoved>(this, OnLocationRemoved);
            Messenger.Default.Register<GuiUpdate>(this, OnGuiUpdate);

            // (Konrad) Populate UI with stored settings
            PopulateSourceFromSettings(Model.Settings);
            if (Model.Settings.DestinationAssets.Any()) PopulateDestinationFromSettings(Model.Settings);
        }

        #region Message Handlers

        private void OnGuiUpdate(GuiUpdate obj)
        {
            switch (obj.Status)
            {
                case Status.Failed:
                    return;
                case Status.Succeeded:
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                        ProcessSucceeded(obj.Settings);
                    }));
                    return;
                case Status.UpToDate:
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                        ProcessUpToDate(obj.Settings);
                    }));
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        private void ProcessUpToDate(ZombieSettings settings)
        {
            Locations.Clear();
            SourceLocations.Clear();

            Model.Settings = settings;

            // (Konrad) Populate UI with stored settings
            PopulateSourceFromSettings(settings);
            if (settings.DestinationAssets.Any()) PopulateDestinationFromSettings(settings);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        private void ProcessSucceeded(ZombieSettings settings)
        {
            Model.Settings = settings;

            // (Konrad) Get all allocated assets. They would be allocated if they were
            // previously deserialized from Settings, or set on previous cycle.
            var allocated = new HashSet<AssetObject>();
            foreach (var l in SourceLocations)
            {
                foreach (var a in l.Assets)
                {
                    allocated.Add(a.Asset);
                }
            }

            Locations = Locations.Where(x => x.LocationObject.LocationType != LocationType.Trash).ToObservableCollection();
            foreach (var l in Locations)
            {
                foreach (var a in l.Assets)
                {
                    allocated.Add(a.Asset);
                }
            }

            // (Konrad) Find out if there are any new assets downloaded that were not accounted for.
            // These would be added to the SourceLocations collection.
            var added = new HashSet<AssetObject>();
            foreach (var a in settings.LatestRelease.Assets)
            {
                if (allocated.Contains(a))
                {
                    allocated.Remove(a);
                    continue;
                }

                added.Add(a);
            }

            // (Konrad) Whatever is left in allocated needs to be deleted.
            var trashLocations = new ObservableCollection<LocationsViewModel>();
            foreach (var l in Locations)
            {
                var remain = new ObservableCollection<AssetViewModel>();
                var trashLoc = new LocationsViewModel(new Location
                {
                    LocationType = LocationType.Trash,
                    DirectoryPath = l.LocationObject.DirectoryPath
                }, false);

                foreach (var avm in l.Assets)
                {
                    if (!allocated.Contains(avm.Asset))
                    {
                        remain.Add(avm);
                    }
                    else
                    {
                        if (trashLoc.Assets.Contains(avm)) continue;

                        avm.IsPlaceholder = true;
                        trashLoc.Assets.Add(avm);
                    }
                }

                l.Assets = remain;
                trashLocations.Add(trashLoc);
            }

            // (Konrad) Let's put these deleted assets into new deleted locations
            foreach (var lvm in trashLocations)
            {
                Locations.Add(lvm);
            }

            // (Konrad) If any location is now empty let's remove it.
            Locations = Locations.Where(x => x.Assets.Any()).ToObservableCollection();

            // (Konrad) Add all new assets to source locations.
            // Since we are calling this from another thread (Timer runs on a thread pool)
            // we need to make sure that the collection is locked.
            lock (_sourceLock)
            {
                SourceLocations.Clear();

                var loc = new LocationsViewModel(new Location
                {
                    LocationType = LocationType.Source
                }, !added.Any());

                foreach (var asset in added)
                {
                    var vm = new AssetViewModel(asset)
                    {
                        Parent = loc
                    };
                    if (!loc.Assets.Contains(vm)) loc.Assets.Add(vm);
                }

                SourceLocations.Add(loc);
            }
        }

        private void OnLocationRemoved(LocationRemoved obj)
        {
            var assets = obj.Removed.Assets.Where(x => !x.IsPlaceholder);
            foreach (var a in assets)
            {
                if (!SourceLocations.First().Assets.Contains(a)) SourceLocations.First().Assets.Add(a);
            }

            // (Konrad) Remove Placeholder asset if other ass
            if (SourceLocations.First()?.Assets.Count > 1 &&
                SourceLocations.FirstOrDefault()?.Assets.FirstOrDefault(x => x.IsPlaceholder) != null)
                SourceLocations.First().RemovePlaceholder();

            Locations.Remove(obj.Removed);
        }

        #endregion

        #region Command Handlers

        private void OnAddLocation()
        {
            Locations.Add(new LocationsViewModel(new Location()));
        }

        #endregion

        #region Settings Utilities

        /// <summary>
        /// Sets DestinationAssets from a Zombie Settings.
        /// </summary>
        /// <param name="settings">Zombie Settings.</param>
        private void PopulateDestinationFromSettings(ZombieSettings settings)
        {
            foreach (var loc in settings.DestinationAssets)
            {
                var newLocation = new LocationsViewModel(loc);
                var assets =
                    new ObservableCollection<AssetViewModel>(loc.Assets.Select(x =>
                        new AssetViewModel(x)
                        {
                            Parent = newLocation,
                            IsPlaceholder = loc.LocationType == LocationType.Trash
                        }));
                newLocation.Assets = assets;

                Locations.Add(newLocation);
            }
        }

        /// <summary>
        /// Sets Source Assets from a Zombie Settings.
        /// </summary>
        /// <param name="settings">Zombie Settings.</param>
        private void PopulateSourceFromSettings(ZombieSettings settings)
        {
            var loc = new LocationsViewModel(new Location
            {
                LocationType = LocationType.Source
            }, !settings.SourceAssets.Any());

            foreach (var asset in settings.SourceAssets)
            {
                var vm = new AssetViewModel(asset)
                {
                    Parent = loc
                };
                if (!loc.Assets.Contains(vm)) loc.Assets.Add(vm);
            }

            SourceLocations.Add(loc);
        }

        /// <summary>
        /// 
        /// </summary>
        public void UpdateSettings()
        {
            var sourceAssets = new List<AssetObject>();
            foreach (var l in SourceLocations)
            {
                if (l.Assets.All(x => x.IsPlaceholder)) continue;

                sourceAssets.AddRange(l.Assets.Where(x => !x.IsPlaceholder).Select(a => a.Asset));
            }

            Model.Settings.SourceAssets = sourceAssets;

            var locations = new List<Location>();
            foreach (var l in Locations)
            {
                // (Konrad) We can skip placeholders but only if they are not trash
                locations.Add(new Location
                {
                    LocationType = l.LocationObject.LocationType,
                    DirectoryPath = l.LocationObject.DirectoryPath,
                    Assets = l.Assets.Where(x => !(x.IsPlaceholder && l.LocationObject.LocationType != LocationType.Trash)).Select(x => x.Asset).ToList()
                });
            }

            Model.Settings.DestinationAssets = locations;
        }

        #endregion
    }
}
