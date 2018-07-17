using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Zombie.Utilities;
using ZombieUtilities.Client;

namespace ZombieService.Host
{
    [ServiceContract(
        Namespace = "http://ZombieService.Host",
        SessionMode = SessionMode.Required,
        CallbackContract = typeof(IZombieContract))]
    public interface IZombieService
    {
        [OperationContract(IsOneWay = false, IsInitiating = true)]
        void Subscribe();

        [OperationContract(IsOneWay = false, IsInitiating = true)]
        void Unsubscribe();

        [OperationContract(IsOneWay = false)]
        void PublishGuiUpdate(GuiUpdate update);

        [OperationContract]
        ZombieSettings GetSettings();

        [OperationContract]
        bool SetSettings(ZombieSettings settings);

        [OperationContract]
        void ExecuteUpdate();

        [OperationContract]
        void ChangeFrequency(Frequency frequency);
    }

    [ServiceContract]
    public interface IZombieContract
    {
        [OperationContract(IsOneWay = true)]
        void GuiUpdate(GuiUpdate update);
    }
}
