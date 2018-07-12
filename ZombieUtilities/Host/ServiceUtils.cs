using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;
using System.Xml;

namespace ZombieUtilities.Host
{
    public class ServiceUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static WSDualHttpBinding CreateClientBinding(int port)
        {
            var baseAddress = $"http://localhost:{port}/ZombieService/Service.svc";
            var binding = new WSDualHttpBinding
            {
                OpenTimeout = new TimeSpan(0, 1, 0),
                CloseTimeout = new TimeSpan(0, 1, 0),
                SendTimeout = new TimeSpan(0, 1, 0),
                ReceiveTimeout = TimeSpan.MaxValue,
                BypassProxyOnLocal = false,
                TransactionFlow = false,
                HostNameComparisonMode = HostNameComparisonMode.StrongWildcard,
                MaxBufferPoolSize = 524288,
                MaxReceivedMessageSize = 65536,
                ClientBaseAddress = new Uri(baseAddress),
                MessageEncoding = WSMessageEncoding.Text,
                TextEncoding = Encoding.UTF8,
                UseDefaultWebProxy = true,
                ReaderQuotas = new XmlDictionaryReaderQuotas
                {
                    MaxDepth = 32,
                    MaxStringContentLength = 8192,
                    MaxArrayLength = 16384,
                    MaxBytesPerRead = 4096,
                    MaxNameTableCharCount = 16384
                },
                ReliableSession = new ReliableSession
                {
                    Ordered = true,
                    InactivityTimeout = TimeSpan.MaxValue
                },
                Security = new WSDualHttpSecurity { Mode = WSDualHttpSecurityMode.None }
            };
            return binding;
        }

        public static int FreeTcpPort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
