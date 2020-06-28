using Biglab.Extensions;
using Biglab.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biglab.Utility.Components
{
    public class CubemapCamera : MonoBehaviour
    {
        public enum UpdateMode
        {
            OnDemand, /* Updates will be handled by calling UpdateCubemap externally. */
            OnUpdate /* Updates will happen on LateUpdate */
        }

        [Flags]
        public enum RenderFace
        {
            None = 0,
            PositiveX = 1 << 0,
            NegativeX = 1 << 1,
            PositiveY = 1 << 2,
            NegativeY = 1 << 3,
            PositiveZ = 1 << 4,
            NegativeZ = 1 << 5,
            All = ~0
        }

        private const int _distinctRenderFaces = 6; // How many distinct faces are there

        private const int _maximumRenderSides = 4; // The maximum number of faces this camera can render into
        private const int _minimumRenderSides = 0; // The fewest number of faces this camera can render into

        private static int CountRenderFaces(RenderFace face)
        {
            if (face.Equals(RenderFace.All))
            {
                return _maximumRenderSides;
            }

            if (face.Equals(RenderFace.None))
            {
                return _minimumRenderSides;
            }

            var count = _minimumRenderSides;
            if (face.HasFlag(RenderFace.PositiveX))
            {
                count++;
            }

            if (face.HasFlag(RenderFace.NegativeX))
            {
                count++;
            }

            if (face.HasFlag(RenderFace.PositiveY)) count++; // TODO: implement rendering of this face (if needed)
            if (face.HasFlag(RenderFace.NegativeY)) count++; // TODO: implement rendering of this face (if needed)
            if (face.HasFlag(RenderFace.PositiveZ))
            {
                count++;
            }

            if (face.HasFlag(RenderFace.NegativeZ))
            {
                count++;
            }

            return count;
        }

        // Camera Fields
        public CameraClearFlags ClearFlags;
        public Color Background;
        public LayerMask CullingMask;

        // Focal point
        public Transform FocalPoint;
        public Bounds FocalBounds;

        [EnumMask] public RenderFace FaceMask;

        [Tooltip("Determines if the cubemap will be updated per frame or externally.")]
        public UpdateMode CubemapUpdateMode;

        [Tooltip("This must be a render texture with the dimension set to Cube.")]
        public RenderTexture TargetTexture;

        private Camera _renderCamera;

        [Tooltip("If enabled, only one side of the cubemap will be rendered per frame.")]
        public bool MultiplexRenderFaces;

        public int FlagCount => CountRenderFaces(FaceMask);

        private static readonly Dictionary<RenderFace, Vector3> _renderFaceAxes = new Dictionary<RenderFace, Vector3>
        {
            {RenderFace.PositiveX, Vector3.left},
            {RenderFace.NegativeX, Vector3.right},
            {RenderFace.PositiveZ, Vector3.back},
            {RenderFace.NegativeZ, Vector3.forward},
            {RenderFace.PositiveY, Vector3.down },
            {RenderFace.NegativeY, Vector3.up }
        };

        private int _renderFaceIndex;

        public Matrix4x4[] WorldToClips => _worldToClipMatrices.OrderBy(x => x.Key).Select(x => x.Value).ToArray(); // TODO: make this safer
        //public Matrix4x4[] ClipToWorlds => _clipToWorldMatrices.OrderBy(x => x.Key).Select(x => x.Value).ToArray(); // TODO: make this safer

        private Dictionary<RenderFace, Matrix4x4> _worldToClipMatrices;
        private RenderTexture _texture;
        private int _slice;

        private void Awake()
        {
            // Create camera
            var go = new GameObject("Render Camera");
            go.transform.parent = transform;
            go.transform.localScale = Vector3.one;
            _renderCamera = go.AddComponent<Camera>();
            //go.hideFlags = HideFlags.HideInHierarchy;
            _renderCamera.enabled = false; // Render manually

            _renderFaceIndex = 0;

            _worldToClipMatrices = new Dictionary<RenderFace, Matrix4x4>();
            //_clipToWorldMatrices = new Dictionary<RenderFace, Matrix4x4>();

            // Prepopulate lookup
            foreach(var key in _renderFaceAxes.Keys)
            {
                _worldToClipMatrices[key] = Matrix4x4.identity;
            }
        }

        private void LateUpdate()
        {
            if (FocalPoint == null)
            {
                return;
            }

            if (CubemapUpdateMode.Equals(UpdateMode.OnUpdate))
            {
                Render(TargetTexture);
            }
        }

        public void Render()
        {
            if (TargetTexture == null)
            {
                throw new InvalidOperationException("No texture is set to render into.");
            }

            Render(TargetTexture);
        }

        /// <summary>
        /// Renders a single face into the cubemap texture.
        /// </summary>
        /// <param name="face">A single face to be rendered into.</param>
        /// <param name="texture">A texture with dimension cubemap.</param>
        public void Render(RenderFace face, RenderTexture texture)
        {
            if (FocalPoint == null)
            {
                return;
            }

            if (face.Equals(RenderFace.None) || face.Equals(RenderFace.All))
            {
                return; // Don't render
            }

            if (CountRenderFaces(face) > 1)
            {
                return; // Only render one side at a time
            }

            if (!texture.dimension.Equals(TextureDimension.Tex2DArray))
            {
                throw new ArgumentException($"{nameof(texture)} must have dimension {nameof(TextureDimension.Tex2DArray)}");
            }

            // Set Camera properties
            var renderAxis = _renderFaceAxes[face];
            _renderCamera.transform.position = transform.position +
                                               transform.lossyScale.Multiply(renderAxis.Multiply(FocalBounds.extents)) +
                                               FocalBounds.center;
            _renderCamera.transform.rotation = Quaternion.LookRotation(FocalPoint.position - _renderCamera.transform.position, face == RenderFace.PositiveY || face == RenderFace.NegativeY ? FocalPoint.forward : FocalPoint.up); // TODO: fix when rendering Y faces
            _renderCamera.cullingMask = CullingMask;
            _renderCamera.clearFlags = ClearFlags;
            _renderCamera.backgroundColor = Background;
            SetOrthographicProperties(face);

            // store the most recent projection matrix for this face
            _worldToClipMatrices[face] = GL.GetGPUProjectionMatrix(_renderCamera.projectionMatrix, false) * _renderCamera.worldToCameraMatrix;
            //_clipToWorldMatrices[face] = _worldToClipMatrices[face].inverse;

            _renderCamera.targetTexture = texture;
            // Render the image
            _texture = texture;

            switch (face)
            {
                case RenderFace.PositiveX:
                    _slice = 0;
                    break;
                case RenderFace.NegativeX:
                    _slice = 1;
                    break;
                case RenderFace.PositiveY:
                    _slice = 2;
                    break;
                case RenderFace.NegativeY:
                    _slice = 3;
                    break;
                case RenderFace.PositiveZ:
                    _slice = 4;
                    break;
                case RenderFace.NegativeZ:
                    _slice = 5;
                    break;
            }
             
            // Grab temp
            var tmp = RenderTexture.GetTemporary(_texture.width, _texture.height);

            // Rendre to temp
            _renderCamera.targetTexture = tmp;
            _renderCamera.Render();
            _renderCamera.targetTexture = null;

            // Copy temp to slice
            Graphics.CopyTexture(tmp, 0, _texture, _slice);

            RenderTexture.ReleaseTemporary(tmp);
        }

        /// <summary>
        /// Renders into the face(s) deteremined by <see cref="FaceMask"/> and <see cref="MultiplexRenderFaces"/>.
        /// </summary>
        /// <param name="texture">The cubemap to render into.</param>
        public void Render(RenderTexture texture)
        {
            if (FaceMask.Equals(RenderFace.None))
            {
                return; // Don't render
            }

            var renderCount = 0;
            var sidesToRender = MultiplexRenderFaces ? 1 : FlagCount;
            do
            {
                // Get the face to render
                var renderFace = (RenderFace)(1 << _renderFaceIndex);
                // increment the face index
                _renderFaceIndex = (_renderFaceIndex + 1) % _distinctRenderFaces;
                // Skip if we don't need to render this face
                if (!FaceMask.HasFlag(renderFace))
                {
                    continue;
                }

                // Render the face and count that we did
                Render(renderFace, texture);
                renderCount++;
            } while (!MultiplexRenderFaces && renderCount < sidesToRender);
        }

        private void SetOrthographicProperties(RenderFace pFace)
        {
            var isAlongXAxis =
                pFace.Equals(RenderFace.PositiveX) || pFace.Equals(RenderFace.NegativeX);

            var isAlongZAxis =
                pFace.Equals(RenderFace.PositiveZ) || pFace.Equals(RenderFace.NegativeZ);

            var localScale = FocalPoint.transform.lossyScale;

            float halfCoindicentSize;
            if (isAlongXAxis) halfCoindicentSize = FocalBounds.extents.x * localScale.x;
            else if (isAlongZAxis) halfCoindicentSize = FocalBounds.extents.z * localScale.z;
            else halfCoindicentSize = FocalBounds.extents.y * localScale.y;

            float halfPerpendicularSize;
            if(isAlongXAxis)
            {
                halfPerpendicularSize = FocalBounds.extents.z * localScale.z;
            }
            else if(isAlongZAxis)
            {
                halfPerpendicularSize = FocalBounds.extents.x * localScale.x;
            }
            else
            {
                halfPerpendicularSize = FocalBounds.extents.x * localScale.x;
            }

            float halfVerticalSize;
            if (isAlongXAxis || isAlongZAxis)
            {
                halfVerticalSize = FocalBounds.extents.y * localScale.y; // Vertically perpendicular direction
            }
            else
            {
                halfVerticalSize = FocalBounds.extents.z * localScale.z;
            }

            _renderCamera.orthographic = true;
            _renderCamera.nearClipPlane = 0;
            _renderCamera.orthographicSize = halfVerticalSize;
            _renderCamera.aspect = halfPerpendicularSize / halfVerticalSize;
            _renderCamera.farClipPlane = 2 * halfCoindicentSize;
        }
    }
}