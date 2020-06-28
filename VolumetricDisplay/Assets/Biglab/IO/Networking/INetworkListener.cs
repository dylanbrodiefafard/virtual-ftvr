using System;
using System.Collections.Generic;
using System.Net;

namespace Biglab.IO.Networking
{
    public interface INetworkListener : IDisposable
    {
        event Action<INetworkConnection> Connected;

        event Action<INetworkConnection, DisconnectReason> Disconnected;

        IReadOnlyDictionary<int, INetworkConnection> Connections { get; }

        IPEndPoint GetLocalEndPoint();

        void SendAll(byte type, byte[] message);
    }
}