using System.IO;

namespace Biglab.Extensions
{
    public static class StreamExtensions
        // Author: Christopher Chamberlain - 2018
    {
        /// <summary>
        /// Reads all available bytes in this stream.
        /// </summary>
        public static byte[] ReadAllBytes(this Stream @this)
        {
            using (var ms = new MemoryStream())
            {
                @this.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}