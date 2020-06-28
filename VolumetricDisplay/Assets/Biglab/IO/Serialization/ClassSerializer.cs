using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Biglab.IO.Serialization
{
    public static class ClassSerializer
        // TODO: CC: Documentation ( Extension Methods? )
    {
        public static byte[] Serialize<T>(T c) where T : class
        {
            var binFormatter = new BinaryFormatter();
            var mStream = new MemoryStream();
            binFormatter.Serialize(mStream, c);
            return mStream.ToArray();
        }

        public static byte[] SerializeArray<T>(T[] cArray) where T : class
        {
            var binFormatter = new BinaryFormatter();
            var mStream = new MemoryStream();
            binFormatter.Serialize(mStream, cArray);
            return mStream.ToArray();
        }

        public static T Deserialize<T>(byte[] bes) where T : class
        {
            var memStream = new MemoryStream();
            var binaryFormat = new BinaryFormatter();
            memStream.Write(bes, 0, bes.Length);
            memStream.Position = 0;
            return binaryFormat.Deserialize(memStream) as T;
        }

        public static T[] DeserializeArray<T>(byte[] bes) where T : class
        {
            var memStream = new MemoryStream();
            var binaryFormat = new BinaryFormatter();
            memStream.Write(bes, 0, bes.Length);
            memStream.Position = 0;
            return binaryFormat.Deserialize(memStream) as T[];
        }
    }
}