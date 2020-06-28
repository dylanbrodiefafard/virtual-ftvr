using System;

namespace Biglab.Remote
{
    [Serializable]
    public class RemoteMenuStreamStateMessage
    {
        public float ImageRate;
        public float DeltaRate;

        public RemoteMenuStreamStateMessage(float imageRate, float deltaRate)
        {
            ImageRate = imageRate;
            DeltaRate = deltaRate;
        }
    }
}
