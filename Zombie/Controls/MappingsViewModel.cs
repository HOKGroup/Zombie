#region References

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Zombie.Utilities;

#endregion

namespace Zombie.Controls
{
    public class MappingsViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand AddLocation { get; set; }

        private ZombieSettings _settings;
        public ZombieSettings Settings
        {
            get { return _settings; }
            set { _settings = value; RaisePropertyChanged(() => Settings); }
        }

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

        public MappingsViewModel(ZombieSettings settings)
        {
            Settings = settings;

            BindingOperations.EnableCollectionSynchronization(_sourceLocations, _sourceLock);
            BindingOperations.EnableCollectionSynchronization(_locations, _lock);

            AddLocation = new RelayCommand(OnAddLocation);

            Messenger.Default.Register<LocationRemoved>(this, OnLocationRemoved);
            //Messenger.Default.Register<ReleaseDownloaded>(this, OnReleaseDownloaded);

            // (Konrad) Populate UI with stored settings
            PopulateSourceFromSettings(settings);
            if (settings.DestinationAssets.Any()) PopulateDestinationFromSettings(settings);
        }

        #region Message Handlers

        //private void OnReleaseDownloaded(ReleaseDownloaded obj)
        //{
        //    // TODO: I am also shipping here a new settings object since these could also be updated.
        //    // TODO: We need to use that new Settings to set them on other objects.
        //    //if (obj.Result != ConnectionResult.Success) return;

        //    // (Konrad) Get all allocated assets. They would be allocated if they were
        //    // previously deserialized from Settings, or set on previous cycle.
        //    var allocated = new HashSet<AssetObject>();
        //    foreach (var l in SourceLocations)
        //    {
        //        foreach (var a in l.Assets)
        //        {
        //            allocated.Add(a.Asset);
        //        }
        //    }
        //    foreach (var l in Locations)
        //    {
        //        foreach (var a in l.Assets)
        //        {
        //            allocated.Add(a.Asset);
        //        }
        //    }

        //    // (Konrad) Find out if there are any new assets downloaded that were not accounted for.
        //    // These would be added to the SourceLocations collection.
        //    var added = new HashSet<AssetObject>();
        //    foreach (var a in obj.Release.Assets)
        //    {
        //        if (allocated.Contains(a))
        //        {
        //            allocated.Remove(a);
        //            continue;
        //        }

        //        added.Add(a);
        //    }

        //    // (Konrad) Whatever is left in allocated needs to be deleted.
        //    foreach (var l in Locations)
        //    {
        //        l.Assets = l.Assets.Where(x => !allocated.Contains(x.Asset)).ToObservableCollection();
        //    }

        //    // (Konrad) If any location is now empty let's remove it.
        //    Locations = Locations.Where(x => x.Assets.Any()).ToObservableCollection();

        //    // (Konrad) Add all new assets to source locations.
        //    // Since we are calling this from another thread (Timer runs on a thread pool)
        //    // we need to make sure that the collection is locked.
        //    lock (_lock)
        //    {
        //        SourceLocations.Clear();

        //        var loc = new LocationsViewModel(new Location
        //        {
        //            IsSourceLocation = true,
        //            MaxHeight = 557
        //        }, !added.Any());

        //        foreach (var asset in added)
        //        {
        //            var vm = new AssetViewModel(asset)
        //            {
        //                Parent = loc
        //            };
        //            if (!loc.Assets.Contains(vm)) loc.Assets.Add(vm);
        //        }

        //        SourceLocations.Add(loc);
        //    }
        //}

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
                        new AssetViewModel(x) { Parent = newLocation }));
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
                IsSourceLocation = true,
                MaxHeight = 557
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

            Settings.SourceAssets = sourceAssets;

            var locations = new List<Location>();
            foreach (var l in Locations)
            {
                locations.Add(new Location
                {
                    IsSourceLocation = l.LocationObject.IsSourceLocation,
                    MaxHeight = l.LocationObject.MaxHeight,
                    DirectoryPath = l.LocationObject.DirectoryPath,
                    Assets = l.Assets.Where(x => !x.IsPlaceholder).Select(x => x.Asset).ToList()
                });
            }

            Settings.DestinationAssets = locations;
        }

        #endregion
    }
}
