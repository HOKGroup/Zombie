using System;
using System.ServiceModel;

namespace ZombieService.Host
{
    public static class HostUtils
    {
        public static ServiceHost CreateHost(ServiceHost currentHost = null)
        {
            currentHost?.Close();

            var binding = new WSDualHttpBinding
            {
                OpenTimeout = new TimeSpan(0, 1, 0),
                CloseTimeout = new TimeSpan(0, 1, 0),
                SendTimeout = new TimeSpan(0, 1, 0),
                ReceiveTimeout = TimeSpan.MaxValue,
                Security = new WSDualHttpSecurity { Mode = WSDualHttpSecurityMode.None }
            };
            var address = new Uri("http://localhost:8000/ZombieService/Service.svc");

            currentHost = new ServiceHost(typeof(ZombieService), address);
            currentHost.AddServiceEndpoint(typeof(IZombieService), binding, address);
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
