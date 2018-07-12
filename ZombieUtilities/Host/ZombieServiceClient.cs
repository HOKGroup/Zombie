using System.ServiceModel;
using Zombie.Utilities;

namespace ZombieUtilities.Host
{
    [ServiceContract(
        Namespace = "http://ZombieService.Host", 
        ConfigurationName = "ZombieUtilities.Host.IZombieService", 
        CallbackContract = typeof(IZombieServiceCallback), 
        SessionMode = SessionMode.Required)]
    public interface IZombieService
    {
        [OperationContract(
            Action = "http://ZombieService.Host/IZombieService/Subscribe", 
            ReplyAction = "http://ZombieService.Host/IZombieService/SubscribeResponse")]
        void Subscribe();

        [OperationContract(
            Action = "http://ZombieService.Host/IZombieService/Unsubscribe", 
            ReplyAction = "http://ZombieService.Host/IZombieService/UnsubscribeResponse")]
        void Unsubscribe();

        [OperationContract(
            Action = "http://ZombieService.Host/IZombieService/PublishGuiUpdate", 
            ReplyAction = "http://ZombieService.Host/IZombieService/PublishGuiUpdateResponse")]
        void PublishGuiUpdate(GuiUpdate update);

        [OperationContract(
            Action = "http://ZombieService.Host/IZombieService/GetSettings", 
            ReplyAction = "http://ZombieService.Host/IZombieService/GetSettingsResponse")]
        ZombieSettings GetSettings();

        [OperationContract(
            Action = "http://ZombieService.Host/IZombieService/SetSettings", 
            ReplyAction = "http://ZombieService.Host/IZombieService/SetSettingsResponse")]
        bool SetSettings(ZombieSettings settings);

        [OperationContract(
            Action = "http://ZombieService.Host/IZombieService/ExecuteUpdate", 
            ReplyAction = "http://ZombieService.Host/IZombieService/ExecuteUpdateResponse")]
        void ExecuteUpdate();

        [OperationContract(
            Action = "http://ZombieService.Host/IZombieService/ChangeFrequency", 
            ReplyAction = "http://ZombieService.Host/IZombieService/ChangeFrequencyResponse")]
        void ChangeFrequency(Frequency frequency);
    }

    [ServiceContract]
    public interface IZombieServiceCallback
    {
        [OperationContract(IsOneWay = true, Action = "http://ZombieService.Host/IZombieService/GuiUpdate")]
        void GuiUpdate(GuiUpdate update);
    }

    public interface IZombieServiceChannel : IZombieService, IClientChannel
    {
    }

    public class ZombieServiceClient : DuplexClientBase<IZombieService>, IZombieService
    {

        public ZombieServiceClient(InstanceContext callbackInstance) :
                base(callbackInstance)
        {
        }

        public ZombieServiceClient(InstanceContext callbackInstance, string endpointConfigurationName) :
                base(callbackInstance, endpointConfigurationName)
        {
        }

        public ZombieServiceClient(InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress) :
                base(callbackInstance, endpointConfigurationName, remoteAddress)
        {
        }

        public ZombieServiceClient(InstanceContext callbackInstance, string endpointConfigurationName, EndpointAddress remoteAddress) :
                base(callbackInstance, endpointConfigurationName, remoteAddress)
        {
        }

        public ZombieServiceClient(InstanceContext callbackInstance, System.ServiceModel.Channels.Binding binding, EndpointAddress remoteAddress) :
                base(callbackInstance, binding, remoteAddress)
        {
        }

        public void Subscribe()
        {
            Channel.Subscribe();
        }

        public void Unsubscribe()
        {
            Channel.Unsubscribe();
        }

        public void PublishGuiUpdate(GuiUpdate update)
        {
            Channel.PublishGuiUpdate(update);
        }

        public ZombieSettings GetSettings()
        {
            return Channel.GetSettings();
        }

        public bool SetSettings(ZombieSettings settings)
        {
            return Channel.SetSettings(settings);
        }

        public void ExecuteUpdate()
        {
            Channel.ExecuteUpdate();
        }

        public void ChangeFrequency(Frequency frequency)
        {
            Channel.ChangeFrequency(frequency);
        }
    }
}
