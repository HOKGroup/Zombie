using System;
using System.ServiceModel;
using NLog;

namespace ZombieService.Host
{
    public static class HostUtils
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentHost"></param>
        /// <returns></returns>
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


                _logger.Info("Successfully opened Service Host at http://localhost:8000/ZombieService/Service.svc");
                return currentHost;
            }
            catch (Exception e)
            {
                _logger.Fatal(e.Message);
                return currentHost;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentHost"></param>
        /// <returns></returns>
        public static ServiceHost TerminateHost(ServiceHost currentHost = null)
        {
            try
            {
                currentHost?.Close();
                _logger.Info("Successfully closed Service Host.");
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
