using Zombie.Controls;

namespace Zombie.Utilities
{
    /// <summary>
    /// Message sent when Location is being deleted.
    /// </summary>
    public class LocationRemoved
    {
        public LocationsViewModel Removed { get; set; }
    }

    /// <summary>
    /// Messege sent to prompt settings to be stored.
    /// </summary>
    public class StoreSettings
    {
        public SettingsType Type { get; set; }
    }

    public enum SettingsType
    {
        Local,
        Remote
    }
}
