using LZ4;

namespace Biglab.IO
{
    /// <summary>
    /// Utility functions for compressing and decompressing byte arrays.
    /// </summary>
    public static class Compression
    {
        /// <summary>
        /// Compress the given byte array using LZ4.
        /// </summary>
        public static byte[] Compress(this byte[] data)
        {
            return LZ4Codec.Wrap(data);
        }

        /// <summary>
        /// Decompress the given byte array using LZ4.
        /// </summary>
        public static byte[] Decompress(this byte[] data)
        {
            return LZ4Codec.Unwrap(data);
        }
    }
}