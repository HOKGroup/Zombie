using System.ServiceModel;
using Zombie.Utilities;
using ZombieService.Host;

namespace ZombieUtilities.Client
{
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
