using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Biglab.Utility;
using UnityEngine;

namespace Biglab.IO.Networking.TCP
{
    public abstract class TCPNetworkConnection : INetworkConnection
    {
        public IPEndPoint EndPoint => Client?.Client.RemoteEndPoint as IPEndPoint;

        public int Id { get; }

        public ConnectionState State
            => Client?.Connected ?? false ? ConnectionState.Connected : ConnectionState.Disconnected;

        public event Action<INetworkConnection, DisconnectReason> Disconnected;

        public event Action<INetworkConnection, Message> MessageReceived;

        protected TcpClient Client { get; }
        protected BinaryWriter Writer;

        private readonly RateLimiter _checkRate = new RateLimiter(1F);
        private int readBytesCount = 0;
        private int writeBytesCount = 0;

        public bool IsDisconnected { get; private set; }

        internal TCPNetworkConnection(TcpClient client, int id)
        {
            Id = id;

            // 
            Client = client;
            Client.NoDelay = true;
        }

        // Read messages thread
        protected Task ExecuteReadTask() => Task.Run(() =>
        {
            var netStream = Client.GetStream();

            // Get a binary reader over the stream
            using (var reader = new BinaryReader(netStream))
            {
                // While we are connected
                while (Client.Connected && !IsDisconnected)
                {
                    Thread.Yield(); // Yield CPU

                    // Occasionally check for disconnection ( 1/10th a second )
                    if (_checkRate.CheckElapsedTime())
                    {
                        // 
                        var readRate = readBytesCount / _checkRate.Duration;
                        var writeRate = writeBytesCount / _checkRate.Duration;

                        // 
                        writeBytesCount = 0;
                        readBytesCount = 0;

                        Debug.Log($"(Connection) [{Id}] W: {writeRate.ToString("0.0")} R: {readRate.ToString("0.0")}");

                        if (!CheckIsAliveOrDisconnect())
                        {
                            // Check for disconnect, if so, break loop.
                            break;
                        }
                    }

                    try
                    {
                        // No data, wait 1 ms
                        if (!netStream.DataAvailable)
                        {
                            Thread.Sleep(1);
                        }

                        // While we have data ( ...? )
                        while (netStream.DataAvailable)
                        {
                            // Read a message frame
                            var type = reader.ReadByte();
                            var length = reader.ReadInt32();
                            var data = reader.ReadBytes(length);

                            // Debug.Log($"(Connection) Read: {(MessageType)type}.");
                            readBytesCount += data.Length;

                            // Check consistency
                            if (data.Length != length)
                            {
                                Debug.LogError($"Inconsistent message frame size, expected {length} but read {data}. Has the stream terminated?");
                                Disconnect(DisconnectReason.Unexpected);

                                throw new IOException("Unable to read a complete message frame.");
                            }

                            // Decompress data
                            data = data.Decompress();

                            // Dispatch message
                            Scheduler.DeferNextFrame(() =>
                            {
                                var message = new Message(type, data);
                                MessageReceived?.Invoke(this, message);
                            });
                        }
                    }
                    catch (Exception)
                    {
                        // 
                    }
                }
            }

            if (!IsDisconnected)
            {
                // "Normally" disconnected ( without exceptions )
                Disconnect(DisconnectReason.Timeout);
            }
        }).ContinueWith(task =>
        {
            // Has something shit the bed?
            if (!task.IsFaulted)
            {
                return;
            }

            Debug.LogError(task.Exception);
            Disconnect(DisconnectReason.Unexpected);
        });

        #region Still Alive Checks

        private bool IsAlive()
        {
            try
            {
                if (Client == null)
                {
                    return false;
                }

                if (!Client.Client.Poll(0, SelectMode.SelectRead))
                {
                    return Client.Connected;
                }

                var temp = new byte[1];
                return Client.Client.Receive(temp, SocketFlags.Peek) > 0; // Any number of bytes read means connection is up.

                // Poll didn't succeed
            }
            catch (ObjectDisposedException) { return false; }
            catch (SocketException) { return false; }
        }

        /// <summary>
        /// Checks if alive (returning true), if determined to be not alive, calls disconnect (returning false).
        /// </summary>
        private bool CheckIsAliveOrDisconnect()
        {
            if (IsAlive())
            {
                return true;
            }

            Disconnect(DisconnectReason.Unexpected);
            return false;
        }

        #endregion

        public void Send(byte type, byte[] data)
        {
            try
            {
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(data));
                }

                if (!CheckIsAliveOrDisconnect())
                {
                    return;
                }

                // No writer created
                if (Writer == null)
                {
                    Writer = new BinaryWriter(Client.GetStream());
                }

                data = data.Compress();

                // Debug.Log($"(Connection) Write: {(MessageType)type}.");
                writeBytesCount += data.Length;

                // Write a message frame
                Writer.Write(type);
                Writer.Write(data.Length);
                Writer.Write(data, 0, data.Length);
            }
            catch (SocketException)
            {
                Disconnect(DisconnectReason.Unexpected);
            }
        }

        public void Disconnect()
        {
            if (CheckIsAliveOrDisconnect())
            {
                Disconnect(DisconnectReason.Request);
            }
        }

        private void Disconnect(DisconnectReason reason)
        {
            if (IsDisconnected)
            {
                return;
            }

            Scheduler.DeferNextFrame(() =>
            {
                Disconnected?.Invoke(this, reason);
                Client?.Dispose();
                Writer = null;
            });

            IsDisconnected = true;
        }
    }
}