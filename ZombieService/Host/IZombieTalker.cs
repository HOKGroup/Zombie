using System.ServiceModel;
using Zombie.Utilities;
using ZombieUtilities;

namespace ZombieService.Host
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ZombieTalker : IZombieTalker
    {
        public ZombieSettings GetSettings()
        {
            return Program.Settings;
        }

        public bool SetSettings(ZombieSettings settings)
        {
            return false;
        }
    }
}
