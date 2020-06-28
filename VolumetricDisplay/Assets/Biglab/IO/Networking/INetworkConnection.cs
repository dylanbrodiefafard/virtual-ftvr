using System;
using System.Net;

namespace Biglab.IO.Networking
{
    public interface INetworkConnection
    {
        /// <summary>
        /// The connection id.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the state of the connection.
        /// </summary>
        ConnectionState State { get; }

        /// <summary>
        /// Remote end point.
        /// </summary>
        IPEndPoint EndPoint { get; }

        /// <summary>
        /// Event invoked when the connection is lost.
        /// </summary>
        event Action<INetworkConnection, DisconnectReason> Disconnected;

        /// <summary>
        /// Event invoked when a message is received.
        /// </summary>
        event Action<INetworkConnection, Message> MessageReceived;

        /// <summary>
        /// Sends a message to the remote side of this connection.
        /// </summary>
        void Send(byte type, byte[] data);

        /// <summary>
        /// Request this connection to disconnect.
        /// </summary>
        void Disconnect();
    }
}