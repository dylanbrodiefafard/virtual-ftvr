using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biglab.Utility
{
    /// <summary>
    /// Storage object containing the pixel-to-surface data.
    /// </summary>
    public class GeometryTexture
    {
        /// <summary>
        /// Width of the texture in pixels.
        /// </summary>
        public int Width => Texture?.width ?? 0;

        /// <summary>
        /// Height of the texture in pixels.
        /// </summary>
        public int Height => Texture?.height ?? 0;

        /// <summary>
        /// The backing texture.
        /// </summary>
        public Texture2D Texture { get; }

        /// <summary>
        /// A mesh reconstruction of the surface via the texture.
        /// </summary>
        public Mesh Mesh
        {
            get
            {
                if (_mesh == null)
                {
                    _mesh = CreateMesh(16); // TODO: expose this magic number? Add to config?
                }

                return _mesh;
            }
        }

        private Mesh _mesh;

        public Texture2D NormalMap
        {
            get
            {
                if (_normalMap == null)
                {
                    _normalMap = CreateNormalMap();
                }

                return _normalMap;
            }
            private set { _normalMap = value; }
        }

        private Texture2D _normalMap;

        public GeometryTexture(Texture2D positions, Texture2D normals = null)
        {
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (positions.format != TextureFormat.RGBAFloat)
            {
                throw new ArgumentException($"{nameof(positions)} must be a {nameof(TextureFormat.RGBAFloat)} texture.");
            }

            if (normals != null && normals.format != TextureFormat.RGBAFloat)
            {
                throw new ArgumentException($"{nameof(normals)} must be a {nameof(TextureFormat.RGBAFloat)} texture.");
            }

            // TODO: Maybe allow RGBAHalf as well if we need it
            Texture = positions;
            NormalMap = normals;
        }

        /// <summary>
        /// Samples the projector-to-display texture for position data at a given location.
        /// </summary>
        public Vector3 SamplePosition(float u, float v)
        {
            var c = Texture.GetPixelBilinear(u, v);
            return new Vector3(c.r, c.g, c.b);
        }

        /// <summary>
        /// Samples the projector-to-display texture for alpha masking data at a given location.
        /// </summary>
        public float SampleAlpha(float u, float v)
        {
            var c = Texture.GetPixelBilinear(u, v);
            return c.a;
        }

        private Texture2D CreateNormalMap()
        {
            // TODO: Implement compression if memory is an issue
            var normalMap = new Texture2D(Width, Height, TextureFormat.RGBAFloat, false);

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var u = x / (float)Width;
                    var v = y / (float)Height;

                    var position = SamplePosition(u, v);

                    var normal = position.normalized;
                    var color = new Color(normal.x, normal.y, normal.z);

                    normalMap.SetPixel(x, y, color);
                }
            }

            normalMap.Apply();
            return normalMap;
        }

        /// <summary>
        /// Constructs a mesh from the positional data.
        /// </summary>
        private Mesh CreateMesh(int pixelSkip)
        {
            // Gather Vertices
            var points = new List<Vector3>();
            var uvs = new List<Vector2>();

            for (var y = 0; y <= Height - pixelSkip; y += pixelSkip)
            {
                for (var x = 0; x <= Width - pixelSkip; x += pixelSkip)
                {
                    var u = x / (float)Width;
                    var v = y / (float)Height;
                    points.Add(SamplePosition(u, v));
                    uvs.Add(new Vector2(u, v));
                }
            }

            // Gather Triangles ( Faces )
            var meshWidth = Width / pixelSkip;
            var meshHeight = Height / pixelSkip;

            var triangles = new List<int>();
            for (var y = 0; y < meshHeight - 1; y += 1)
            {
                for (var x = 0; x < meshWidth - 1; x += 1)
                {
                    var t00 = (x + 0) + (y + 0) * meshWidth;
                    var t10 = (x + 1) + (y + 0) * meshWidth;
                    var t11 = (x + 1) + (y + 1) * meshWidth;
                    var t01 = (x + 0) + (y + 1) * meshWidth;

                    triangles.AddRange(new[] { t00, t10, t11 });
                    triangles.AddRange(new[] { t00, t11, t01 });
                }
            }

            // Configure Mesh
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(points);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}