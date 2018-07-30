#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceModel;
using System.Text;
using System.Xml;

#endregion

namespace ZombieUtilities.Client
{
    public class ServiceUtils
    {
        /// <summary>
        /// Creates a new http binding for a Zombie Service client.
        /// </summary>
        /// <param name="port">Port to run the client on.</param>
        /// <returns>New DualHttpBinding.</returns>
        public static WSDualHttpBinding CreateClientBinding(int port)
        {
            var baseAddress = $"http://localhost:{port}/ZombieService/Service.svc";
            var binding = new WSDualHttpBinding
            {
                OpenTimeout = new TimeSpan(0, 1, 0),
                CloseTimeout = new TimeSpan(0, 0, 10),
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

        /// <summary>
        /// Retrieve next available TCP Port starting from a given port number.
        /// </summary>
        /// <param name="startingPort">Port to start looking from.</param>
        /// <returns>Free port number.</returns>
        public static int FreeTcpPort(int startingPort = 8000)
        {
            var portArray = new List<int>();
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            // Ignore active connections
            var connections = properties.GetActiveTcpConnections();
            portArray.AddRange(from n in connections
                where n.LocalEndPoint.Port >= startingPort
                select n.LocalEndPoint.Port);

            // Ignore active tcp listners
            var endPoints = properties.GetActiveTcpListeners();
            portArray.AddRange(from n in endPoints
                where n.Port >= startingPort
                select n.Port);

            // Ignore active udp listeners
            endPoints = properties.GetActiveUdpListeners();
            portArray.AddRange(from n in endPoints
                where n.Port >= startingPort
                select n.Port);

            portArray.Sort();

            for (var i = startingPort; i < ushort.MaxValue; i++)
            {
                if (!portArray.Contains(i)) return i;
            }

            return 0;
        }
    }
}
