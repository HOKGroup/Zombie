using GalaSoft.MvvmLight;
using Zombie.Utilities;

namespace Zombie.Controls
{
    public class AssetViewModel : ViewModelBase
    {
        public LocationsViewModel Parent { get; set; }
        public bool IsPlaceholder { get; set; }

        private AssetObject _asset;
        public AssetObject Asset
        {
            get { return _asset; }
            set { _asset = value; RaisePropertyChanged(() => Asset); }
        }

        public AssetViewModel(AssetObject asset)
        {
            Asset = asset;
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
