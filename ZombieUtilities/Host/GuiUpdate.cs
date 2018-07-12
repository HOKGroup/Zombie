using Zombie.Utilities;

namespace ZombieUtilities.Host
{
    public class GuiUpdate
    {
        public Status Status { get; set; }
        public ZombieSettings Settings { get; set; }
        public string Message { get; set; }
    }

    public enum Status
    {
        Failed,
        Succeeded,
        UpToDate
    }
}
