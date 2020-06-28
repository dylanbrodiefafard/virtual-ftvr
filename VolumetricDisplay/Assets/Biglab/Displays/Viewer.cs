using System;
using System.Collections;

using Biglab.Extensions;
using Biglab.Math;
using Biglab.Utility;

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR;

namespace Biglab.Displays
{
    using Eye = Camera.MonoOrStereoscopicEye;

    [SelectionBase]
    public class Viewer : MonoBehaviour
    {
        /// <summary>
        /// Can this viewer render stereo content?
        /// </summary>
        public bool CanRenderStereo
            => _debugStereoRendering || (EnabledStereoRendering && XRSettings.enabled &&
                                         DisplaySystem.Instance.SubsystemRenderMode ==
                                         DisplaySystem.VolumetricRenderMode.SingleViewer);

        /// <summary>
        /// Enables or disables the intent to render with stereo viewpoints.
        /// </summary>
        public bool EnabledStereoRendering
        {
            get { return _enableStereoRendering; }
            set { _enableStereoRendering = value; }
        }

        /// <summary>
        /// Was the latest rendered frame stereo content?
        /// </summary>
        private bool HasStereoContent
            => _projMatrices.Length == 2;

        /// <summary>
        /// The eye left anchor.
        /// </summary>
        public Transform LeftAnchor => _leftAnchor;

        /// <summary>
        /// The eye right anchor.
        /// </summary>
        public Transform RightAnchor => _rightAnchor;

        public Camera RightCamera => _rightAnchor.GetComponent<Camera>();

        public Camera LeftOrMonoCamera => _leftAnchor.GetComponent<Camera>();

        /// <summary>
        /// The viewers render textures.
        /// </summary>
        public RenderTexture[] Textures { get; private set; }

        /// <summary>
        /// The role of this viewer.
        /// </summary>
        public ViewerRole Role
        {
            get { return _role; }

            set
            {
                if (_role != value && _isStarted)
                {
                    UnregisterWithDisplay();
                    _role = value;
                    RegisterWithDisplay();
                }
                else
                {
                    _role = value;
                }
            }
        }

        public delegate void EyePassType(Viewer viewer, Eye eye);

        public event EyePassType PreEyePass;

        public event EyePassType PostEyePass;

        protected void RaisePreEyePass(Viewer viewer, Eye eye)
            => PreEyePass?.Invoke(viewer, eye);

        protected void RaisePostEyePass(Viewer viewer, Eye eye)
            => PostEyePass?.Invoke(viewer, eye);

        /// <summary>
        /// A captured frame was rendered and the data is available on the CPU.
        /// </summary>
        public event Action<Texture2D> RenderedFrame;

        /// <summary>
        /// A callback just before each eye is rendered.
        /// </summary>
        public event Action<Eye> EyeRenderCallback;

        [Header("Render Settings")]
        [SerializeField]
        private bool _enableStereoRendering = true;

        // Uncomment to use
        // DF: I used this for testing. It's very useful to make sure the tracking is working properly.
        [SerializeField]
        [Tooltip("Forces stereo rendering even if platform does not support it.")]
        private bool _debugStereoRendering;

        [Tooltip("Determines which viewpoint to fallback to when rendering in non-stereo. Mono is the average viewpoint of left and right.")]
        public Eye NonStereoFallbackEye = Eye.Mono;

        /// <summary>
        /// Size of the texture ( a single eye in stereo ).
        /// </summary> 
        public Vector2Int TextureSize = new Vector2Int(512, 512);

        [Space]
        public bool EnableFrameCapture;

        public float CaptureFrequency = 30F;

        [Space]
        [Tooltip("Culling mask of this viewer. This mask is combied with the volumetric camera's mask for rendering.")]
        public LayerMask CullingMask = -1;

        [Header("Viewpoints")]
        [SerializeField]
        [Tooltip("The left eye anchor point. Will be automatically created if not specified.")]
        private Transform _leftAnchor;

        [SerializeField]
        [Tooltip("The right eye anchor point. Will be automatically created if not specified.")]
        private Transform _rightAnchor;

        private Matrix4x4[] _projMatrices;
        private Matrix4x4[] _viewMatrices;

        [Header("Frustum")]
        public FrustumFittingMode FrustumMode = FrustumFittingMode.Andrew;

        public float FrustumScalingFactor = 1.05f;

        [Space]
        [SerializeField]
        private ViewerRole _role = ViewerRole.Primary;

        private bool _isStarted;

        private Coroutine _renderCoroutine;

        private Texture2D _readTexture;

        private bool _takeLeftScreenshot;
        private int _leftCounter = 0;
        private bool _takeRightScreenshot;
        private int _rightCounter = 0;

        #region MonoBehaviour

        private void OnDrawGizmosSelected()
        {
            if (FrustumMode == FrustumFittingMode.Dylan)
            {
                Bizmos.DrawDylanFrustumGizmo(LeftOrMonoCamera.transform, FrustumScalingFactor * 0.5f);
                if (EnabledStereoRendering)
                {
                    Bizmos.DrawDylanFrustumGizmo(RightCamera.transform, FrustumScalingFactor * 0.5f);
                }
            }
        }

        private void OnEnable() => this.WaitOneFrame(() =>
        {
            RegisterWithDisplay();

            // Start render coroutine if not started
            if (_renderCoroutine != null)
            {
                StopCoroutine(_renderCoroutine);
            }

            _renderCoroutine = StartCoroutine(RenderCoroutine());
        });

        private void OnDisable()
        {
            // Stop render coroutine of not stopped
            if (_renderCoroutine != null)
            {
                StopCoroutine(_renderCoroutine);
                _renderCoroutine = null;
            }

            UnregisterWithDisplay();
        }

        private IEnumerator Start()
        {
            // 
            InitializeAnchorCameras();

            // Configure target eyes
            // TODO: remove these? These cameras aren't actually stereo cameras.
            LeftOrMonoCamera.stereoTargetEye = StereoTargetEyeMask.Left;
            RightCamera.stereoTargetEye = StereoTargetEyeMask.Right;

            // Create read texture
            _readTexture = new Texture2D(TextureSize.x, TextureSize.y);

            // 
            CreateOrAdjustStereoSetup(skipAdjustCheck: true);

            _isStarted = true;

            // Wait a frame and disable ( because the cameras didn't seem to update values correctly otherwise )
            // TODO: DF: Is this still needed?
            yield return new WaitForEndOfFrame();

            LeftOrMonoCamera.enabled = false;
            RightCamera.enabled = false;
        }

        private void Reset()
            => InitializeAnchorCameras();

        private void Update() 
            => _takeLeftScreenshot = _takeRightScreenshot = Input.GetKeyDown(KeyCode.K);

        #endregion

        private void ResizeReadTexture(RenderTexture eyeTexture)
        {
            // Ensure that the read texture and eye texture are the same size
            if (_readTexture.width != eyeTexture.width || _readTexture.height != eyeTexture.height)
            {
                _readTexture.Resize(eyeTexture.width, eyeTexture.height);
            }
        }

        private void RegisterWithDisplay()
        {
            // Register self with the display system
            if (Application.isPlaying && DisplaySystem.Instance != null)
            {
                DisplaySystem.Instance.RegisterViewer(this);
            }
        }

        private void UnregisterWithDisplay()
        {
            // Unregister self with the display system
            if (Application.isPlaying && DisplaySystem.Instance != null)
            {
                DisplaySystem.Instance.UnregisterViewer(this);
            }
        }

        #region Create/Size Render Textures

        /// <summary>
        /// Sets the desired resolution and stereo mode.
        /// </summary>
        private void CreateOrAdjustStereoSetup(bool skipAdjustCheck)
        {
            // Is the desired texture size different than actual size
            // Or stereo configuration doesn't match stereo state
            if (skipAdjustCheck
                // Check texture size
                || (TextureSize.x != Textures[0].width)
                || (TextureSize.y != Textures[0].height)
                // Check stereo state
                || (CanRenderStereo && Textures.Length == 1)
                || (!CanRenderStereo && Textures.Length == 2))
            {
                var width = TextureSize.x;
                var height = TextureSize.y;

                // Dispose of previous textures
                if (Textures != null)
                {
                    foreach (var tex in Textures)
                    {
                        tex.Release();
                    }
                }

                Debug.Log($"Setting Viewer '{name}' Textures: {width} by {height} ({(CanRenderStereo ? "Stereo" : "Mono")}).");

                // Create the render texture
                if (CanRenderStereo)
                {
                    // Stereo projection
                    _projMatrices = new Matrix4x4[2];
                    _viewMatrices = new Matrix4x4[2];

                    Textures = new[]
                    {
                        CreateRenderTexture($"{name} L", width, height),
                        CreateRenderTexture($"{name} R", width, height)
                    };

                    // Stereo cameras render to texture
                    LeftOrMonoCamera.targetTexture = GetEyeTexture(Eye.Left);
                    RightCamera.targetTexture = GetEyeTexture(Eye.Right);
                }
                else
                {
                    // Mono projection
                    _projMatrices = new Matrix4x4[1];
                    _viewMatrices = new Matrix4x4[1];

                    Textures = new[]
                    {
                        CreateRenderTexture($"{name} M", width, height)
                    };

                    // Stereo cameras render to texture
                    LeftOrMonoCamera.targetTexture = Textures[0];
                    RightCamera.targetTexture = Textures[0];
                }
            }
        }

        /// <summary>
        /// Creates a 2D render texture suitable for cameras.
        /// </summary>
        private static RenderTexture CreateRenderTexture(string name, int width, int height, RenderTextureFormat format = RenderTextureFormat.Default)
        {
            var tex = new RenderTexture(width, height, 24, format)
            {
                name = name,
                autoGenerateMips = false
            };
            return tex;
        }

        #endregion

        #region Getters ( Proj, View and Eye Texture )

        /// <summary>
        /// Return the full matrix ( world to clip ) matrix for the specified eye transformed for use in a shader.
        /// </summary>
        public Matrix4x4 GetShaderMatrix(Eye eye, bool renderIntoTexture)
        {
            var view = GetViewMatrix(eye);
            var proj = GL.GetGPUProjectionMatrix(GetProjectionMatrix(eye), renderIntoTexture);
            return proj * view;
        }

        /// <summary>
        /// Return the projection ( camera to clip ) matrix for the specified eye.
        /// </summary>
        public Matrix4x4 GetProjectionMatrix(Eye eye)
        {
            MakeSafeEye(ref eye);

            return eye == Eye.Mono ? _projMatrices[0] : _projMatrices[(int)eye];
        }

        /// <summary>
        /// Return the view ( world to camera ) matrix for the specified eye.
        /// </summary>
        public Matrix4x4 GetViewMatrix(Eye eye)
        {
            MakeSafeEye(ref eye);

            return eye == Eye.Mono ? _viewMatrices[0] : _viewMatrices[(int)eye];
        }

        /// <summary>
        /// Gets an eyes render texture.
        /// </summary>
        public RenderTexture GetEyeTexture(Eye eye)
        {
            MakeSafeEye(ref eye);

            switch (eye)
            {
                case Eye.Left:
                    return Textures[0];
                case Eye.Right:
                    return Textures[1];
                case Eye.Mono:
                    return Textures[0];
            }

            throw new Exception("Attempting to get texture for eye, but eye enum was invalid");
        }

        /// <summary>
        /// Gets an eyes Transform
        /// </summary>
        /// <param name="eye">The specified eye.</param>
        /// <returns>A Transform for the specified eye.</returns>
        public Transform GetEyeTransform(Eye eye)
        {
            MakeSafeEye(ref eye);

            switch (eye)
            {
                case Eye.Left:
                    return LeftAnchor;
                case Eye.Right:
                    return RightAnchor;
                case Eye.Mono:
                    return LeftAnchor;
                default:
                    throw new ArgumentException($"Invalid eye: {eye} is not a valid enum.");
            }
        }

        public Camera GetEyeCamera(Eye eye)
        {
            MakeSafeEye(ref eye);

            switch(eye)
            {
                case Eye.Left:
                    return LeftOrMonoCamera;
                case Eye.Right:
                    return RightCamera;
                case Eye.Mono:
                    return LeftOrMonoCamera;
                default:
                    throw new ArgumentException($"Invalid eye: {eye} is not a valid enum.");
            }
        }

        private void MakeSafeEye(ref Eye eye)
        {
            if (!HasStereoContent)
            {
                eye = Eye.Mono;
            }
        }

        #endregion

        #region Anchors and Camera

        private void InitializeAnchorCameras()
        {
            // Find or create anchor references
            FindOrCreateAnchor(ref _leftAnchor, "Left");
            FindOrCreateAnchor(ref _rightAnchor, "Right");
        }

        private void FindOrCreateAnchor(ref Transform anchor, string side)
        {
            if (anchor == null)
            {
                // Find left anchor
                var left = transform.FindChild(child =>
                {
                    var childName = child.name.ToLower();
                    return childName.Contains(side.ToLower()) &&
                           (childName.Contains("eye") || childName.Contains("anchor"));
                });

                if (left)
                {
                    anchor = left;
                }
                else
                {
                    // Create anchor
                    var anchorGameObject = new GameObject($"{side} Eye");
                    anchorGameObject.transform.SetParent(transform);

                    // Set anchor
                    anchor = anchorGameObject.transform;

                    // Configure camera
                    anchor.gameObject.GetOrAddComponent<Camera>();
                }
            }
        }

        private static void ConfigureCamera(Camera camera, Color backgroundColor, LayerMask cullingMask,
            CameraClearFlags clearFlags)
        {
            // camera.hideFlags = HideFlags.NotEditable;
            camera.backgroundColor = backgroundColor;
            camera.cullingMask = cullingMask;
            camera.clearFlags = clearFlags;
            camera.stereoConvergence = 0;
            camera.stereoSeparation = 0;
            camera.useJitteredProjectionMatrixForTransparentRendering = false;
        }

        #endregion

        #region Projection

        /// <summary>
        /// Frustum mode.
        /// </summary>
        public enum FrustumFittingMode
        {
            Dylan,
            Andrew,
            Window,
            Fixed
        }

        private void UpdateCameraView(Camera targetCamera, Transform vCameraT, Eye eye)
        {
            switch (FrustumMode)
            {
                case FrustumFittingMode.Andrew:
                    // Force camera to look at center
                    targetCamera.transform.LookAt(vCameraT, vCameraT.up);
                    break;

                case FrustumFittingMode.Window:

                    // Add the offset from the camera on the tablet to the viewer's eye
                    var physicalToWorldScale = DisplaySystem.Instance.GetTransformAlignedPhysicalToWorldScale(targetCamera.transform);
                    targetCamera.transform.position = targetCamera.transform.TransformPoint(Config.RemoteViewer.ViewInDevice.Multiply(physicalToWorldScale));

                    break;

                case FrustumFittingMode.Dylan:
                case FrustumFittingMode.Fixed:
                default:
                    break;
            }

            // Store matrix for stereo use
            if (CanRenderStereo && eye != Eye.Mono)
            {
                _viewMatrices[(int)eye] = targetCamera.worldToCameraMatrix;
            }
            else
            {
                // Mono is left. We have to store these values because Oculus overrites the Camera's properties
                _viewMatrices[0] = targetCamera.worldToCameraMatrix;
            }

        }

        /// <summary>
        /// Updates the target camera to be suitable for viewing the volumetric camera instance.
        /// </summary>
        private void UpdateCameraProjection(Camera targetCamera, Transform vCameraT, Eye eye)
        {
            var texture = targetCamera.targetTexture;
            var aspect = texture == null ? 1F : texture.width / (float)texture.height;

            // Compute projection matrix
            switch (FrustumMode)
            {
                case FrustumFittingMode.Andrew:
                    targetCamera.projectionMatrix = Projection.ComputeAndrewsFittedProjectionMatrix(targetCamera.transform, vCameraT, aspect, FrustumScalingFactor);
                    break;

                case FrustumFittingMode.Dylan:
                    targetCamera.projectionMatrix = Projection.ComputeDylansFittedProjectionMatrix(targetCamera.transform, vCameraT, aspect, FrustumScalingFactor);
                    break;

                case FrustumFittingMode.Window:
                    var physicalToWorldScale = DisplaySystem.Instance.GetTransformAlignedPhysicalToWorldScale(targetCamera.transform);
                    var windowPosition = targetCamera.transform.position + targetCamera.transform.forward * -Config.RemoteViewer.ViewInDevice.z * physicalToWorldScale.z;
                    var windowSize = Config.RemoteViewer.DeviceScreenSize.Multiply(new Vector2(physicalToWorldScale.x, physicalToWorldScale.y));
                    targetCamera.projectionMatrix = Projection.ComputeWindowProjectionMatrix(targetCamera.transform, windowPosition, windowSize, vCameraT, aspect, targetCamera.farClipPlane);
                    break;

                case FrustumFittingMode.Fixed:
                default:
                    targetCamera.projectionMatrix = Projection.ComputeFixedProjectionMatrix(vCameraT, aspect);
                    targetCamera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, targetCamera.projectionMatrix);
                    targetCamera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, targetCamera.projectionMatrix);
                    break;
            }
            // Store matrix for stereo use
            if (CanRenderStereo && eye != Eye.Mono)
            {
                _projMatrices[(int)eye] = targetCamera.projectionMatrix;
            }
            else
            {
                // Mono is left. We have to store these values because Oculus overrites the Camera's properties
                _projMatrices[0] = targetCamera.projectionMatrix;
            }

            // Extract camera properties from projection matrix to update for editor rendering
            float nearClipPlane, farClipPlane, fieldOfView;
            Projection.GetCameraProjectionProperties(targetCamera.projectionMatrix, out nearClipPlane, out farClipPlane, out fieldOfView);
            targetCamera.nearClipPlane = nearClipPlane;
            targetCamera.farClipPlane = farClipPlane;
            if (!XRSettings.enabled)
            {
                targetCamera.fieldOfView = fieldOfView; // To get rid of warning.
            }
        }

        #endregion

        #region Render

        private void RenderEyeCamera(Camera eyeCamera, Eye eye, VolumetricCamera vCamera)
        {
            var cullingMask = CullingMask & vCamera.CullingMask;
            var backgroundColor = vCamera.ClearColor;
            var clearFlags = vCamera.ClearFlags;

            // Save the original view matrix
            var originalPosition = eyeCamera.transform.position;
            var originalRotation = eyeCamera.transform.rotation;

            // Sets the camera's properties for rendering
            ConfigureCamera(eyeCamera, backgroundColor, cullingMask, clearFlags);

            // Setup the render view matrix
            UpdateCameraView(eyeCamera, vCamera.transform, eye);

            // Update the camera's projection matrix using the current frustum mode
            UpdateCameraProjection(eyeCamera, vCamera.transform, eye);

            // Render Callback
            if (eye == Eye.Left && Role == ViewerRole.Secondary) EyeRenderCallback?.Invoke(Eye.Right);
            else EyeRenderCallback?.Invoke(eye);
            
            // Render
            PreEyePass?.Invoke(this, eye);
            RenderWithCamera(eyeCamera, vCamera.ReplacementShader);
            PostEyePass?.Invoke(this, eye);

            if((eye == Eye.Left || eye == Eye.Mono) && _takeLeftScreenshot)
            {
                ScreenRecorder.SaveScreenshot(eyeCamera.targetTexture.ExtractTexture2D(), eyeCamera.name + _leftCounter++, ScreenRecorder.Format.PNG);
                _takeLeftScreenshot = false;
            }

            if(eye == Eye.Right && _takeRightScreenshot)
            {
                ScreenRecorder.SaveScreenshot(eyeCamera.targetTexture.ExtractTexture2D(), eyeCamera.name + _rightCounter++, ScreenRecorder.Format.PNG);
                _takeRightScreenshot = false;
            }

            // Restore the original view matrix now that rendering is complete
            eyeCamera.transform.position = originalPosition;
            eyeCamera.transform.rotation = originalRotation;
        }

        private void Render()
        { 
            var vCamera = VolumetricCamera.Instance;

            // 
            CreateOrAdjustStereoSetup(skipAdjustCheck: false);

            // Render the left or mono camera
            RenderEyeCamera(LeftOrMonoCamera, Eye.Left, vCamera);

            // Stereo. Render the right camera too
            if (CanRenderStereo) { RenderEyeCamera(RightCamera, Eye.Right, vCamera); }
        }

        /// <summary>
        /// Renders with the given camera with the optional replacement shader.
        /// </summary>
        private static void RenderWithCamera(Camera camera, Shader shader = null)
        {
            if (shader)
            {
                camera.RenderWithShader(shader, "RenderType");
            }
            else
            {
                camera.Render();
            }
        }

        private IEnumerator RenderCoroutine()
        {
            var rateLimiter = new RateLimiter(1F / 10F);
            //var pixels = new Color32[0];

            while (enabled)
            {
                // 
                if (EnableFrameCapture)
                {
                    // Set capture framerate
                    if (CaptureFrequency <= 1) { CaptureFrequency = 1F; }
                    rateLimiter.Duration = 1F / CaptureFrequency;

                    // 
                    if (rateLimiter.CheckElapsedTime())
                    {
                        yield return null;

                        Render();

                        // Get eye render texture
                        var eyeTexture = GetEyeTexture(Eye.Mono);
                        ResizeReadTexture(eyeTexture);

                        // Request
                        var frameRequest = AsyncGPUReadback.Request(eyeTexture);

                        // Wait for frame to read from the GPU
                        yield return new WaitUntil(() => frameRequest.done);

                        // Extract data
                        var buffer = frameRequest.GetData<Color32>();
                        _readTexture.SetPixels32(buffer.ToArray(), 0);

                        // Notify that we have captured a frame
                        RenderedFrame?.Invoke(_readTexture);
                    }
                }
                else
                {
                    Render();
                }

                yield return null;
            }
        }

        #endregion
    }

    public enum ViewerRole
    {
        Primary,
        Secondary,
        Remote
    }
}