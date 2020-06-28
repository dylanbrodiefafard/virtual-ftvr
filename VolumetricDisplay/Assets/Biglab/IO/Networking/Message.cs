namespace Biglab.IO.Networking
{
    public struct Message
    {
        public readonly byte Type;

        public readonly byte[] Data;

        public Message(byte type, byte[] data)
        {
            Type = type;
            Data = data;
        }
    }
}