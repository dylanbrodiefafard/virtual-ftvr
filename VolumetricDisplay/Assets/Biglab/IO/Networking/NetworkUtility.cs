using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;

namespace Biglab.IO.Networking
{
    public static class NetworkUtility
    {
        /// <summary>
        /// Attempts to parse the given address.
        /// </summary>
        /// <param name="address"> Some input address as a string. </param>
        /// <param name="addr"> out parameter for the IP address. </param>
        /// <returns> The parsed address or null of unable to parse. </returns>
        public static bool TryParseAddress([NotNull] string address, out IPAddress addr)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            // Blank address
            if (string.IsNullOrWhiteSpace(address))
            {
                addr = null;
                return false;
            }

            // Sanitize address
            address = address.Trim().ToLower();

            // use known values
            if (address == "localhost")
            {
                address = "127.0.0.1";
            }

            if (address == "any")
            {
                address = "0.0.0.0";
            }

            if (address == "auto")
            {
                address = GetLocalAddress().ToString();
            }

            // Try to parse the address
            return IPAddress.TryParse(address, out addr);
        }

        /// <summary>
        /// Gets the best guess at the local address.
        /// </summary>
        /// <returns> The best-guessed address or null if unable to be determined. </returns>
        public static IPAddress GetLocalAddress()
        {
            try
            {
                // Try to resolve outgoing address using UDP
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    var endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint?.Address;
                }
            }
            catch (Exception)
            {
                // Attempt to find outgoing address from DNS
                var host = Dns.GetHostEntry(Dns.GetHostName());

                // Find first IPv4 address
                var address = host.AddressList.FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);

                // 
                if (address == null)
                {
                    throw new Exception("No network adapters with an IPv4 address in the system!");
                }

                return address;
            }
        }
    }
}