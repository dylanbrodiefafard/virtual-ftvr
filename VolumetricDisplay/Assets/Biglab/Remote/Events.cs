using System;

using Biglab.IO.Networking;

using UnityEngine.Events;

namespace Biglab.Remote
{
    [Serializable]
    public class IntMenuEvent : UnityEvent<int, INetworkConnection>
    { }

    [Serializable]
    public class FloatMenuEvent : UnityEvent<float, INetworkConnection>
    { }

    [Serializable]
    public class BoolMenuEvent : UnityEvent<bool, INetworkConnection>
    { }

    [Serializable]
    public class StringMenuEvent : UnityEvent<string, INetworkConnection>
    { }
}
