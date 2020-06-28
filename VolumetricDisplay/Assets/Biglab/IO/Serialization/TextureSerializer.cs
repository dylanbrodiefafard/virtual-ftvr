using System.IO;
using UnityEngine;

namespace Biglab.IO.Serialization
{
    public static class TextureSerializer
    {
        /// <summary>
        /// Encodes the given texture as a jpeg image with a small header describing the texture size.
        /// </summary>
        public static byte[] SerializeTexture(this Texture2D texture, int jpegQuality)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(texture.width);
                writer.Write(texture.height);

                var bytes = texture.EncodeToJPG(jpegQuality);
                writer.Write(bytes.Length);
                writer.Write(bytes);

                return stream.ToArray();
            }
        }

        public static void DeserializeTexture(this byte[] data, ref Texture2D texture)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                var width = reader.ReadInt32();
                var height = reader.ReadInt32();

                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);

                // Create texture if not created
                if (texture == null)
                {
                    texture = new Texture2D(width, height);
                }

                // If not the correct size, resize texture
                if (texture.width != width || texture.height != height)
                {
                    texture.Resize(width, height);
                }

                // Load jpg into image
                texture.LoadImage(bytes);
                texture.Apply();
            }
        }
    }
}