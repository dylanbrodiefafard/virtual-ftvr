using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Biglab.IO.Networking.TCP
{
    public class TCPNetworkClient : TCPNetworkConnection
    {
        public TCPNetworkClient(string address, int port)
            : base(new TcpClient(), 0)
        {
            IPAddress ip;
            if (NetworkUtility.TryParseAddress(address, out ip))
            {
                // Async try to connect
                Client.ConnectAsync(ip, port)
                    .ContinueWith(task =>
                    {
                        // 
                        if (task.IsFaulted)
                        {
                            Debug.LogError("Unable to establish connection");
                            Disconnect();
                        }
                        else
                        {
                            // Its ok?
                            ExecuteReadTask();
                        }
                    });
            }
            else
            {
                throw new ArgumentException($"Unable to connect to server. Unable to parse IP Address '{address}'.");
            }
        }
    }
}