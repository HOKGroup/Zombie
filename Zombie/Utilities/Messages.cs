﻿using Zombie.Controls;

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

    public class UpdateStatus
    {
        public string Message { get; set; }
    }

    public class PrereleaseDownloaded
    {
        public PrereleaseStatus Status { get; set; }
        public ZombieSettings Settings { get; set; }
    }

    public enum PrereleaseStatus
    {
        Found,
        Failed
    }

    public enum ConnectionStatus
    {
        Success,
        Failed,
        Disconnecting
    }

    public enum SettingsType
    {
        Local,
        Remote,
        GitHub
    }
}
