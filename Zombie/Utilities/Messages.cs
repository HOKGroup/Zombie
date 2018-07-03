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
    /// Message sent when Release finished downloading.
    /// </summary>
    public class ReleaseDownloaded
    {
        public ReleaseObject Release { get; set; }
        public ZombieSettings Settings { get; set; }
        public ConnectionResult Result { get; set; }
    }

    /// <summary>
    /// Messege sent to prompt settings to be stored.
    /// </summary>
    public class StoreSettings
    {  
    }

    /// <summary>
    /// Message sent to prompt UpdaterRunner to change its interval.
    /// </summary>
    public class ChangeFrequency
    {
        public Frequency Frequency { get; set; }
    }

    /// <summary>
    /// Message sent to prompt UI to update its status label.
    /// </summary>
    public class UpdateStatus
    {
        public string Status { get; set; }
    }

    public enum ConnectionResult
    {
        Success,
        Failure
    }
}
