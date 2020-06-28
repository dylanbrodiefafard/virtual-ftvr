using System;
using System.Collections;
using System.Collections.Generic;

using Biglab.Extensions;
using Biglab.Utility.Components;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Biglab.Displays
{
    public sealed class DisplaySystem : ImmortalMonobehaviour<DisplaySystem>
    {
        /// <summary>
        /// Converts world space to phyiscal space
        /// </summary>
        public Matrix4x4 WorldToPhysical
            => VolumeToPhysical * WorldToVolume;

        /// <summary>
        /// Converts physical space to world space.
        /// </summary>
        public Matrix4x4 PhysicalToWorld
            => VolumeToWorld * PhysicalToVolume;

        /// <summary>
        /// Converts physical space to world space for normals.
        /// </summary>
        public Matrix4x4 PhysicalToWorldNormal
            => PhysicalToWorld.NormalMatrix();

        /// <summary>
        /// Gets Volume to World Inverse Transpose Matrix with no translation
        /// </summary>
        /// <value>
        /// The transformation matrix.
        /// </value>
        public Matrix4x4 VolumeToWorldNormal
            => VolumeToWorld.NormalMatrix();

        /// <summary>
        /// Converts world space to camera volume space.
        /// </summary>
        public Matrix4x4 WorldToVolume
            => VolumetricCamera.Instance.transform.worldToLocalMatrix;

        /// <summary>
        /// Converts camera volume space to world space.
        /// </summary>
        public Matrix4x4 VolumeToWorld
            => VolumetricCamera.Instance.transform.localToWorldMatrix;

        /// <summary>
        /// Transforms from volume space to physical space.
        /// </summary>
        public Matrix4x4 VolumeToPhysical
            => PhysicalToVolume.inverse;

        /// <summary>
        /// Transforms from a physical space ( centered on sphere ) to volume space ( aligned to vcam )
        /// </summary>
        public Matrix4x4 PhysicalToVolume { get; private set; }

        /// <summary>
        /// The display calibration.
        /// </summary>
        public Calibrations.Display.DisplayCalibration Calibration { get; private set; }

        public RenderTexture FlatTexture
        {
            get
            {
                if (_flatCamera == null)
                {
                    _flatCamera = CreateCubemapCamera();
                }

                return _flatCamera.TargetTexture;
            }
        }

        public CubemapCamera CubemapCameraRig => _flatCamera;

        /// <summary>
        /// The expected mode to render based on the viewer configuration.
        /// </summary>
        public VolumetricRenderMode ExpectedRenderMode
        {
            get { return _expectedRenderMode; }

            private set
            {
                _expectedRenderMode = value;

                if (_subsystem != null)
                {
                    _subsystem.SetCompatibleRenderMode(ExpectedRenderMode);
                }
            }
        }

        public VolumetricRenderMode SubsystemRenderMode =>
            _subsystem == null ? _expectedRenderMode : _subsystem.RenderMode;

        private VolumetricRenderMode _expectedRenderMode;

        /// <summary>
        /// The list of non-remote viewers.
        /// </summary>
        public IReadOnlyList<Viewer> Viewers { get; private set; }

        /// <summary>
        /// Gets the viewer set to the primary role.
        /// </summary>
        public Viewer PrimaryViewer { get; private set; }

        /// <summary>
        /// Gets whether or not the display has a primary viewer
        /// </summary>
        public bool HasPrimaryViewer => PrimaryViewer != null;

        /// <summary>
        /// Gets the viewer set to the secondary role.
        /// </summary>
        public Viewer SecondaryViewer { get; private set; }

        /// <summary>
        /// Gets whether or not the display has a secondary viewer
        /// </summary>
        public bool HasSecondaryViewer => SecondaryViewer != null;

        /// <summary>
        /// Gets the best guess at the 'primary' viewer by trying to get the primary role, then secondary role.
        /// </summary>
        public Viewer SingleViewer => HasPrimaryViewer ? PrimaryViewer : SecondaryViewer;

        /// <summary>
        /// Gets whether or not the display has a primary viewer
        /// </summary>
        public bool HasSingleViewer => HasPrimaryViewer || HasSecondaryViewer;

        public event Action<Viewer, ViewerRole> RegisteredViewer;

        public event Action<Viewer, ViewerRole> UnregisteredViewer;

        private DisplaySubsystem _subsystem;

        private CubemapCamera _flatCamera;

        #region MonoBehaviour

        private void Start()
        {
            // Add display subsystem
            _subsystem = AddSubsystemComponent();


            // Construct viewer list
            UpdateViewerList();

            // Set render mode
            _subsystem.SetCompatibleRenderMode(ExpectedRenderMode);
        }

        private void LateUpdate()
        {
            if (!_subsystem.RenderMode.Equals(VolumetricRenderMode.Flat))
            {
                return;
            }

            if (_flatCamera != null)
            {
                _flatCamera.Render();
            }
        }

        #endregion

        public IEnumerator GetWaitForPrimaryViewer()
        {
            yield return new WaitUntil(() => HasPrimaryViewer);
        }

        public IEnumerator GetWaitForSubsystem()
        {
            yield return new WaitUntil(() => _subsystem != null);
        }

        private static CubemapCamera CreateCubemapCamera()
        {
            // Make the Cubemap Camera and put it on the Volumetric Camera
            var cubemapCamera = VolumetricCamera.Instance.gameObject.AddComponentWithInit<CubemapCamera>(script =>
            {
                script.CubemapUpdateMode = CubemapCamera.UpdateMode.OnDemand;
                script.FaceMask = CubemapCamera.RenderFace.All;
                script.FocalBounds = new Bounds(Vector3.zero, Vector3.one);
                script.FocalPoint = VolumetricCamera.Instance.transform;
                script.MultiplexRenderFaces = false;
                script.TargetTexture = new RenderTexture(1024, 1024, 16)
                {
                    autoGenerateMips = false,
                    useMipMap = false,
                    wrapMode = TextureWrapMode.Clamp,
                    dimension = TextureDimension.Tex2DArray,
                    volumeDepth = 6
                };
                script.TargetTexture.Create();
                script.Background = VolumetricCamera.Instance.ClearColor;
                script.CullingMask = VolumetricCamera.Instance.CullingMask;
                script.ClearFlags = VolumetricCamera.Instance.ClearFlags;
                script.hideFlags = HideFlags.HideInInspector;
            });

            // Make sure the settings are synced
            VolumetricCamera.Instance.PropertiesChanged += () =>
            {
                cubemapCamera.Background = VolumetricCamera.Instance.ClearColor;
                cubemapCamera.CullingMask = VolumetricCamera.Instance.CullingMask;
                cubemapCamera.ClearFlags = VolumetricCamera.Instance.ClearFlags;
            };

            return cubemapCamera;
        }

        /// <summary>
        /// Returns the display subsystem.
        /// </summary>
        public DisplaySubsystem GetSubsystem()
            => _subsystem;

        private DisplaySubsystem AddSubsystemComponent()
        {
            // Attempt to instantiate renderer
            var displayType = Config.GeneralDisplay.GetDisplayType();

            DisplaySubsystem subsystem;

            // Instantiate display by type
            if (TryCreateRenderer(displayType, out subsystem))
            {
                return subsystem;
            }

            Debug.LogError($"Unable to load intended display type: {displayType}");

            // Fallback to instantiating the completely virtual type
            if (!TryCreateRenderer(DisplayType.Virtual, out subsystem))
            {
                throw new Exception("Unable to load any display type. This is a serious error.");
            }

            return subsystem;
        }

        public Vector3 GetTransformAlignedPhysicalToWorldScale(Transform worldTransform)
        {
            var rightScalingFactor = Instance.WorldToPhysical.MultiplyVector(worldTransform.right).magnitude;
            var upScalingFactor = Instance.WorldToPhysical.MultiplyVector(worldTransform.up).magnitude;
            var forwardScalingFactor = Instance.WorldToPhysical.MultiplyVector(worldTransform.forward).magnitude;
            return new Vector3(rightScalingFactor, upScalingFactor, forwardScalingFactor).Inverse();
        }

        #region Viewers

        internal void RegisterViewer(Viewer viewer)
        {
            if (viewer.Role != ViewerRole.Remote)
            {
                if (viewer.Role == ViewerRole.Primary)
                {
                    if (HasPrimaryViewer && PrimaryViewer != viewer)
                    {
                        Debug.LogWarning(
                            $"The primary viewer was already set. Replacing '{PrimaryViewer.name}' with the newly registered viewer '{viewer.name}'.");
                    }

                    PrimaryViewer = viewer;
                    RegisteredViewer?.Invoke(viewer, viewer.Role);
                }
                else
                {
                    if (HasSecondaryViewer && SecondaryViewer != viewer)
                    {
                        Debug.LogWarning(
                            $"The secondary viewer was already set. Replacing '{SecondaryViewer.name}' with the newly registered viewer '{viewer.name}'.");
                    }

                    SecondaryViewer = viewer;
                    RegisteredViewer?.Invoke(viewer, viewer.Role);
                }
            }

            // 
            UpdateViewerList();
        }

        internal void UnregisterViewer(Viewer viewer)
        {
            if (viewer.Role != ViewerRole.Remote)
            {
                if (viewer.Role == ViewerRole.Primary)
                {
                    if (PrimaryViewer == viewer)
                    {
                        Debug.Log($"Unregistering '{viewer.name}' as the primary viewer.");

                        // Invoke unregister callback
                        UnregisteredViewer?.Invoke(viewer, viewer.Role);
                        PrimaryViewer = null;
                    }
                    else if (HasPrimaryViewer)
                    {
                        Debug.LogWarning(
                            $"Attempting to unregister '{viewer.name}' as the primary viewer, but wasn't the assigned reference.");
                    }
                }
                else
                {
                    if (SecondaryViewer == viewer)
                    {
                        Debug.Log($"Unregistering '{viewer.name}' as the secondary viewer.");

                        // Invoke unregister callback
                        UnregisteredViewer?.Invoke(viewer, viewer.Role);
                        SecondaryViewer = null;
                    }
                    else if (HasSecondaryViewer)
                    {
                        Debug.LogWarning(
                            $"Attempting to unregister '{viewer.name}' as the secondary viewer, but wasn't the assigned reference.");
                    }
                }
            }

            // 
            UpdateViewerList();
        }

        /// <summary>
        /// <para>Assigns viewers to the display.</para>
        /// </summary>
        private void UpdateViewerList()
        {
            var viewers = new List<Viewer>();

            // 
            if (PrimaryViewer)
            {
                viewers.Add(PrimaryViewer);
            }

            // 
            if (SecondaryViewer)
            {
                viewers.Add(SecondaryViewer);
            }

            // 
            Viewers = viewers;

            // 
            var mode = GetExpectedRenderMode(viewers);
            if (ExpectedRenderMode != mode)
            {
                Debug.Log($"Display expects to render <b>{mode}</b>");
                ExpectedRenderMode = mode;
            }
        }

        private static VolumetricRenderMode GetExpectedRenderMode(IReadOnlyCollection<Viewer> viewers)
        {
            // Determines rendering capabilities
            switch (viewers.Count)
            {
                case 0:
                    return VolumetricRenderMode.Flat;

                case 1:
                    return VolumetricRenderMode.SingleViewer;

                case 2:
                    return VolumetricRenderMode.MultiViewer;

                default:
                    throw new InvalidOperationException(
                        $"Unable to assign viewers. Expected 0, 1 or 2 viewer objects but somehow found {viewers.Count}.");
            }
        }

        #endregion

        #region Creating Display Subsystem

        private bool TryCreateRenderer(DisplayType displayType, out DisplaySubsystem subsystem)
        {
            Debug.Log($"Creating Rendering Subsystem: <b>{displayType}</b>");

            // Get the display prefab by type
            var prefab = GetDisplayTypePrefab(displayType);

            // Create the display
            var subsystemGameObject = Instantiate(prefab);

            // Configure renderer
            subsystem = subsystemGameObject.GetComponentInChildren<DisplaySubsystem>();

            // Set name and set parent
            subsystemGameObject.name = $"{subsystem.GetType().Name}";
            subsystemGameObject.transform.SetParent(transform);

            try
            {
                // Compute and load necessary values
                PhysicalToVolume = subsystem.ComputePhysicalToVolumeMatrix();
                Calibration = subsystem.LoadDisplayCalibration();

                Assert.IsNotNull(Calibration,
                    $"Call to {nameof(subsystem.LoadDisplayCalibration)} must not return null.");

                // Good, loaded and configured
                return true;
            }
            catch (Exception ex)
            {
                // Bad, unable to load configuration
                Debug.LogWarning(ex);

                // Destroy attempted game object
                Destroy(subsystemGameObject);
                subsystem = null;

                return false;
            }
        }

        private GameObject GetDisplayTypePrefab(DisplayType type)
        {
            switch (type)
            {
                default:
                    throw new NotImplementedException($"Display Type {type} has no known implementation.");

                // Projection into mosiac screen
                case DisplayType.Mosaic:
                    return Resources.Load("Mosaic Display Subsystem") as GameObject;

                // Projection into scene with virtual projectors ( for VR )
                case DisplayType.Virtual:
                    return Resources.Load("Virtual Display Subsystem") as GameObject;

                // Projection into scene view with calibration data
                case DisplayType.VirtualByCalibration:
                    return Resources.Load("Calibration Display Subsystem") as GameObject;
            }
        }

        #endregion

        public enum VolumetricRenderMode
        {
            Flat,
            SingleViewer,
            MultiViewer
        }
    }
}