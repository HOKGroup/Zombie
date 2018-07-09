using System;
using System.ServiceModel;
using ZombieUtilities;

namespace ZombieService.Host
{
    public static class HostUtils
    {
        public static ServiceHost CreateHost(ServiceHost currentHost = null)
        {
            currentHost?.Close();
            currentHost = new ServiceHost(typeof(ZombieTalker), new Uri[]{
                new Uri("net.pipe://localhost")
            });

            currentHost.AddServiceEndpoint(typeof(IZombieTalker),
                new NetNamedPipeBinding(),
                "PipeGetSettings");

            currentHost.AddServiceEndpoint(typeof(IZombieTalker),
                new NetNamedPipeBinding(),
                "PipeSetSettings");

            currentHost.Open();
            return currentHost;
        }

        public static ServiceHost TerminateHost(ServiceHost currentHost = null)
        {
            currentHost?.Close();
            return null;
        }
    }
}
