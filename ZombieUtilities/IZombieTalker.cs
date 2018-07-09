using System.ServiceModel;
using Zombie.Utilities;

namespace ZombieUtilities
{
    [ServiceContract]
    public interface IZombieTalker
    {
        [OperationContract]
        ZombieSettings GetSettings();

        [OperationContract]
        bool SetSettings(ZombieSettings settings);
    }
}
