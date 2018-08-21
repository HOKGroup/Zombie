using System;
using System.ServiceModel;
using NLog;

namespace ZombieService.Host
{
    public static class HostUtils
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Creates new instance of a service host. 
        /// </summary>
        /// <param name="currentHost">Host variable to be set.</param>
        /// <returns>New instance of the host.</returns>
        public static ServiceHost CreateHost(ServiceHost currentHost = null)
        {
            try
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
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                return currentHost;
            }
        }

        /// <summary>
        /// Terminates the host service when the app is shutting down.
        /// </summary>
        /// <param name="currentHost">Host to be terminated.</param>
        /// <returns>current host is returned if it was not killed.</returns>
        public static ServiceHost TerminateHost(ServiceHost currentHost = null)
        {
            try
            {
                currentHost?.Close();
                return null;
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                return currentHost;
            }
        }
    }
}
