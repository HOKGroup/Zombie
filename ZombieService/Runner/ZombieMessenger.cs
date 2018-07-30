#region References

using System;
using System.ComponentModel;
using System.ServiceModel;
using System.Threading;
using NLog;
using ZombieService.Host;
using ZombieUtilities.Client;

#endregion

namespace ZombieService.Runner
{
    public class ZombieMessenger
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        // (Konrad) We need to turn off Synchronization Context for this callback
        // since it's being executed from the same thread as the Service is running
        // on and would block the execution of the service causing a deadlock.
        [CallbackBehavior(UseSynchronizationContext = false)]
        public class ZombieServiceCallback : IZombieContract
        {
            private SynchronizationContext _syncContext = AsyncOperationManager.SynchronizationContext;
            public event EventHandler<GuiUpdateEventArgs> ServiceCallbackEvent;

            public void GuiUpdate(GuiUpdate update)
            {
                _syncContext.Post(OnServiceCallbackEvent, new GuiUpdateEventArgs { Update = update });
            }

            // (Konrad) Since there is no subscribers to this event in this app
            // this will result in nothing happening. 
            private void OnServiceCallbackEvent(object state)
            {
                var handler = ServiceCallbackEvent;
                var e = state as GuiUpdateEventArgs;

                handler?.Invoke(this, e);
            }
        }

        /// <summary>
        /// Broadcasts a an event to all clients that are subscribed. This will
        /// cause GUI updates on all Zombie instances.
        /// </summary>
        /// <param name="update">Update message.</param>
        public void Broadcast(GuiUpdate update)
        {
            var binding = ServiceUtils.CreateClientBinding(ServiceUtils.FreeTcpPort());
            var endpoint = new EndpointAddress(new Uri("http://localhost:8000/ZombieService/Service.svc"));
            var context = new InstanceContext(new ZombieServiceCallback());
            var client = new ZombieServiceClient(context, binding, endpoint);

            client.Open();
            client.Subscribe();

            try
            {
                client.PublishGuiUpdate(update);
                client.Unsubscribe();
                client.Close();
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                client.Abort();
            }
        }
    }
}
