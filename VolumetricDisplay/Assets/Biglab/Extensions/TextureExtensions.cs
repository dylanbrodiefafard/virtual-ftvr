using System;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace Biglab.Extensions
{
    public static class TextureUtils
    {
        /// <summary>
        /// Reads the pixels available to a render texture and returns a Texture2D copy of it. 
        /// This is a slow operation and probably shouldn't be used every frame.
        /// </summary>
        public static Texture2D ExtractTexture2D(this RenderTexture @this, TextureFormat format = TextureFormat.ARGB32,
            Texture2D texture = null)
        {
            // Capture pixe;s
            var prev = RenderTexture.active;
            RenderTexture.active = @this;

            var w = @this?.width ?? Display.main.renderingWidth;
            var h = @this?.height ?? Display.main.renderingHeight;

            // Allocate and read pixels into a new Texture2D
            if (texture == null)
            {
                texture = new Texture2D(w, h, format, false);
            }

            texture.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            texture.Apply();
            RenderTexture.active = prev;

            return texture;
        }

        /// <summary>
        /// Writes the texture as a PNG to the disk.
        /// </summary>
        public static void WriteFile(this Texture2D @this, [NotNull] string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            // Ensure .png extension
            path = Path.ChangeExtension(path, "png");
            File.WriteAllBytes(path, @this.EncodeToPNG());
        }

        /// <summary>
        /// Writes the render texture as a PNG to the disk.
        /// </summary>
        public static void WriteFile(this RenderTexture @this, string path)
        {
            var texture = ExtractTexture2D(@this);
            texture.WriteFile(path);
        }
    }
}