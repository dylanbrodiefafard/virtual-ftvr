using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Biglab.Utility;

using UnityEngine;

namespace Biglab.IO.Networking.TCP
{
    public class TCPNetworkListener : INetworkListener
    {
        private readonly TcpListener _listener;

        public event Action<INetworkConnection> Connected;

        public event Action<INetworkConnection, DisconnectReason> Disconnected;

        public IReadOnlyDictionary<int, INetworkConnection> Connections => _connections;

        private readonly Dictionary<int, INetworkConnection> _connections;

        private readonly Queue<int> _recycledIds;
        private int _idCounter;

        public TCPNetworkListener(string address, int port)
        {
            _recycledIds = new Queue<int>();
            _connections = new Dictionary<int, INetworkConnection>();

            IPAddress ip;
            if (NetworkUtility.TryParseAddress(address, out ip))
            {
                var endpoint = new IPEndPoint(ip, port);
                _listener = new TcpListener(endpoint);

                address = ip.MapToIPv4().ToString();

                Debug.Log($"Trying to listen on: {address}:{port}");

                _listener.Start();

                Debug.Log($"Listening on: {address}:{port}");

                var acceptTask = _listener.AcceptTcpClientAsync();
                acceptTask.ContinueWith(AcceptConnection);
            }
            else
            {
                throw new ArgumentException($"Unable to start server. Unable to parse IP Address '{address}'.");
            }
        }

        private void AcceptConnection(Task<TcpClient> task)
        {
            // Get the TCP Client Object
            var client = task.Result;
            var id = GetAvailableConnectionId();

            // 
            var connection = new TCPNetworkServerClient(client, id);
            connection.Disconnected += (c, r) =>
            {
                _recycledIds.Enqueue(c.Id);
                _connections.Remove(c.Id);
                Disconnected?.Invoke(c, r);
            };

            // Store and invoke connection event
            _connections[id] = connection;

            // Invoke connect event on server
            Scheduler.DeferNextFrame(() => Connected?.Invoke(connection));

            // Async wait for next connection
            var acceptTask = _listener.AcceptTcpClientAsync();
            acceptTask.ContinueWith(AcceptConnection);
        }

        private int GetAvailableConnectionId()
        {
            if (_recycledIds.Count > 0)
            {
                return _recycledIds.Dequeue();
            }
            else
            {
                return _idCounter++;
            }
        }

        public IPEndPoint GetLocalEndPoint()
        {
            return _listener.Server.LocalEndPoint as IPEndPoint;
        }

        public void SendAll(byte type, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            foreach (var connection in _connections.Values)
            {
                connection.Send(type, data);
            }
        }

        private class TCPNetworkServerClient : TCPNetworkConnection
        {
            public TCPNetworkServerClient(TcpClient client, int id)
                : base(client, id)
            {
                ExecuteReadTask();
            }
        }

        public void Dispose()
        {
            _listener.Stop();
            _listener.Server.Shutdown(SocketShutdown.Both);
        }
    }
}