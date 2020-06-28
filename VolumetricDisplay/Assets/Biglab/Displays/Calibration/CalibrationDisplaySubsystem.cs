using UnityEngine;
using Biglab.Utility;
using Biglab.Tracking;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Biglab.Displays.Calibration
{
    /// <summary>
    /// Renders a <see cref="DisplaySystem"/> to a virtual display. 
    /// The mesh used is generated from projector calibration data.
    /// </summary>
    public sealed class CalibrationDisplaySubsystem : DisplaySubsystem
    {
        /// <summary>
        /// The material created for the displays shader.
        /// </summary>
        private Material _material;

        // 
        private MaterialPropertyBlock[] _projectorMaterialBlocks;

        public bool ForceAlignViewerToSceneView;

        private bool _wasTrackedObjectEnabled;
        private bool _isTrackedObjectDisabled;

        private Camera _previewCamera;

        #region MonoBehaviour

        private void Start()
        {
            // 
            _material = new Material(Shader.Find("Biglab/Spheree/Virtual"));

            // 
            _projectorMaterialBlocks = new MaterialPropertyBlock[Display.Calibration.Projectors.Count];
            for (var i = 0; i < _projectorMaterialBlocks.Length; i++)
            {
                _projectorMaterialBlocks[i] = new MaterialPropertyBlock();
            }

            // Create observer game object
            var previewCameraGameObject = new GameObject("Preview Camera")
            {
                hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor
            };
            previewCameraGameObject.transform.SetParent(transform);

            // Create observer camera
            _previewCamera = previewCameraGameObject.AddComponent<Camera>();
            _previewCamera.depth = -5;
        }

        private void Update()
        {
            if (!Display.HasSingleViewer)
            {
                return;
            }

            var viewer = Display.SingleViewer;

            // Copy camera
            _previewCamera.transform.position = viewer.LeftOrMonoCamera.transform.position;
            _previewCamera.transform.rotation = viewer.LeftOrMonoCamera.transform.rotation;
            // _previewCamera.projectionMatrix = viewer.LeftCamera.projectionMatrix;

            if (ForceAlignViewerToSceneView)
            {
                // Get tracked object
                var trackedObject = viewer.GetComponent<TrackedObject>();
                if (trackedObject.enabled)
                {
                    _wasTrackedObjectEnabled = trackedObject.enabled;
                    _isTrackedObjectDisabled = true; // Mark that we need to reset when not forceful
                    trackedObject.enabled = false; // Disable tracked object
                }

#if UNITY_EDITOR
                // 
                if (SceneView.lastActiveSceneView == null)
                {
                    return;
                }

                var sceneCamera = SceneView.lastActiveSceneView.camera;

                if (!sceneCamera)
                {
                    return;
                }

                viewer.transform.position = sceneCamera.transform.position;
                viewer.transform.rotation = sceneCamera.transform.rotation;

#endif
            }
            else if (_isTrackedObjectDisabled)
            {
                // Reset tracked object state
                var trackedObject = viewer.GetComponent<TrackedObject>();
                trackedObject.enabled = _wasTrackedObjectEnabled;

                _isTrackedObjectDisabled = false;
            }
        }

        private void OnRenderObject()
        {
            // TODO: This is incorrect to be in OnRenderObject, as it would render multiple times?
            RenderEyePass(Camera.current.stereoActiveEye, null);
        }

        #endregion

        #region DisplaySubsystem

        public override void SetCompatibleRenderMode(DisplaySystem.VolumetricRenderMode expectedMode)
        {
            switch (expectedMode)
            {
                default: // Fallback to Flat
                    RenderMode = DisplaySystem.VolumetricRenderMode.Flat;
                    break;

                case DisplaySystem.VolumetricRenderMode.SingleViewer:
                    RenderMode = DisplaySystem.VolumetricRenderMode.SingleViewer;
                    break;
            }
        }

        protected override void RenderEyePass(Camera.MonoOrStereoscopicEye eye, RenderTexture cameraRenderTexture)
        {
            if (RenderMode.Equals(DisplaySystem.VolumetricRenderMode.Flat))
            {
                DrawFlat();
            }

            if (RenderMode.Equals(DisplaySystem.VolumetricRenderMode.SingleViewer))
            {
                DrawSingleViewer(Display.SingleViewer, eye);
            }
        }

        protected internal override Calibrations.Display.DisplayCalibration LoadDisplayCalibration()
        {
            // Load projector geometry
            var projTextures = new GeometryTexture[Config.MosaicRenderer.Count];
            for (var i = 0; i < projTextures.Length; i++)
            {
                projTextures[i] = Calibrations.Display.DisplayCalibration.LoadProjectorGeometry(i);
            }

            // Load camera geometry
            var camTextures = new[] {Calibrations.Display.DisplayCalibration.LoadCameraGeometry()};

            //
            return new Calibrations.Display.DisplayCalibration(projTextures, camTextures);
        }

        protected internal override Matrix4x4 ComputePhysicalToVolumeMatrix()
        {
            var factor = 0.5F / Config.MosaicRenderer.Radius; // 1v / 0.3048m
            return Matrix4x4.Scale(Vector3.one * factor);
        }

        #endregion

        #region Rendering

        private void DrawFlat()
        {
            _material.DisableKeyword("VIEWER");

            // Set Cubemap uniforms
            _material.SetTexture("_FlatTex", Display.FlatTexture);
            _material.SetMatrixArray("_FlatWorldToClips", Display.CubemapCameraRig.WorldToClips);

            // Render each projector mesh for the pass
            for (var i = 0; i < Display.Calibration.Projectors.Count; i++)
            {
                var projector = Display.Calibration.Projectors[i];
                _projectorMaterialBlocks[i].SetTexture("_ProjectorTex", projector.Texture);
                _projectorMaterialBlocks[i].SetTexture("_NormalMap", projector.NormalMap);
                Graphics.DrawMesh(projector.Mesh, Display.VolumeToWorld, _material, gameObject.layer, null, 0,
                    _projectorMaterialBlocks[i]);
            }
        }

        private void DrawSingleViewer(Viewer viewer, Camera.MonoOrStereoscopicEye eye)
        {
            // Set display information
            _material.SetMatrix("_VolumeToWorld", Display.VolumeToWorld);
            _material.SetMatrix("_VolumeToWorldNormal", Display.VolumeToWorldNormal);

            // Set viewer information
            _material.EnableKeyword("VIEWER");
            _material.SetVector("_ViewerPosition", viewer.GetEyeTransform(eye).position);
            _material.SetMatrix("_ViewerMatrix", viewer.GetShaderMatrix(eye, false));

            // Set the main texture to the eye texture
            _material.SetTexture("_MainTex", viewer.GetEyeTexture(eye));

            // Render each projector mesh for the pass
            for (var i = 0; i < Display.Calibration.Projectors.Count; i++)
            {
                var projector = Display.Calibration.Projectors[i];
                _projectorMaterialBlocks[i].SetTexture("_ProjectorTex", projector.Texture);
                _projectorMaterialBlocks[i].SetTexture("_NormalMap", projector.NormalMap);
                Graphics.DrawMesh(projector.Mesh, Display.VolumeToWorld, _material, gameObject.layer, null, 0,
                    _projectorMaterialBlocks[i]);
            }
        }

        #endregion
    }
}