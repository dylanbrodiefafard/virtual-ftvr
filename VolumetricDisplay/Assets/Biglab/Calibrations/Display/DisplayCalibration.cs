using Biglab.Utility;
using Biglab.IO.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Biglab.Calibrations.Display
{
    public class DisplayCalibration
    {
        /// <summary>
        /// Projector calibration information.
        /// </summary>
        public IReadOnlyList<GeometryTexture> Projectors { get; }

        /// <summary>
        /// Camera calibration information.
        /// </summary>
        public IReadOnlyList<GeometryTexture> Cameras { get; }

        public DisplayCalibration(IEnumerable<GeometryTexture> projectors, IEnumerable<GeometryTexture> cameras)
        {
            Projectors = projectors.ToArray();
            Cameras = cameras.ToArray();
        }

        /// <summary>
        /// Loads a single set projector information from disk ( ie, pro1pixel_.bin and friends )
        /// </summary>
        public static GeometryTexture LoadCameraGeometry()
        {
            // 
            var width = 1280; // Config.Camera.Width;
            var height = 1024; // Config.Camera.Height;

            // Read all bytes
            var rgbBytes = File.ReadAllBytes(Path.Combine(Config.CalibrationPath, "campixel_.bin"));

            // Deserialize
            var positions = rgbBytes.DeserializeBytesAsArray<Vector3>()
                .Select(p => new Color(p.x, p.z, p.y, 1F) * 0.5F).ToArray();

            // Create texture out of byte array
            var texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            texture.SetPixels(positions);
            texture.Apply();

            return new GeometryTexture(texture);
        }

        /// <summary>
        /// Loads a single set projector information from disk ( ie, pro1pixel_.bin and friends )
        /// </summary>
        public static GeometryTexture LoadProjectorGeometry(int index)
        {
            // 
            var width = Config.MosaicRenderer.Width;
            var height = Config.MosaicRenderer.Height;

            var name = $"pro{index + 1}pixel";

            // Read all bytes
            var rgbBytes = File.ReadAllBytes(Path.Combine(Config.CalibrationPath, $"{name}_.bin"));
            var aBytes = File.ReadAllBytes(Path.Combine(Config.CalibrationPath, $"{name}a_.bin"));

            // Deserialize
            var alphas = aBytes.DeserializeBytesAsArray<float>();
            var positions = rgbBytes.DeserializeBytesAsArray<Vector3>()
                .Select(position => position * 0.5F).ToArray();

            var normals = rgbBytes.DeserializeBytesAsArray<Vector3>()
                .Select(position => position.normalized).ToArray();

            // Merge position data with alpha data
            var posPixels = new Color[width * height];
            var norPixels = new Color[width * height];
            for (var iy = 0; iy < height; iy++)
            {
                for (var ix = 0; ix < width; ix++)
                {
                    var q = iy * width + ix;
                    var f = (height - iy - 1) * width + ix; // Flips Y

                    var x = positions[f].x;
                    var y = positions[f].y;
                    var z = positions[f].z;
                    var a = alphas[f];

                    posPixels[q] = new Color(x, z, y, a);

                    var normal = normals[f];
                    norPixels[q] = new Color(normal.x, normal.z, normal.y, 0);
                }
            }

            // Create position texture out of byte array
            var positionTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            positionTexture.SetPixels(posPixels);
            positionTexture.Apply();

            // Create normal texture out of byte array
            var normalTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            normalTexture.SetPixels(norPixels);
            normalTexture.Apply();

            return new GeometryTexture(positionTexture, normalTexture);
        }
    }
}