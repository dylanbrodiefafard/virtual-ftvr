using System;
using Biglab.Utility;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityInput = UnityEngine.Input;

namespace Biglab.Displays.Mosaic
{
    /// <summary>
    /// Renders a <see cref="DisplaySystem"/> to a mosiac stereo display.
    /// </summary>
    public class MosaicDisplaySubsystem : DisplaySubsystem
    {
        private Material _quadProjectionMaterial;

        private Permutation<int> _ordering;

        private static Texture2D _blank;

        #region MonoBehaviour

        void Start()
        {
            // TODO: figure out if this blank texture is needed
            _blank = new Texture2D(Display.FlatTexture.width, Display.FlatTexture.height, TextureFormat.ARGB32, false);

            // Create material
            _quadProjectionMaterial = new Material(Shader.Find("Biglab/Spheree/Projector"));

            // Load ordering
            _ordering = new Permutation<int>(Config.MosaicRenderer.ProjectorMapping);

            CreateBlitCamera();
        }

        void Update()
        {
            // Swap projector ordering
            // TODO: Improve this design! Its dependant on an xbox controller
            if (UnityInput.GetKeyDown(KeyCode.F5))
            {
                // Move to next permutation
                _ordering.NextPermutation();

                // Set ordering and save
                Config.MosaicRenderer.ProjectorMapping = _ordering.ToArray();
                Config.Save();
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            RenderEyePass(Camera.current.stereoActiveEye, destination);
        }

        #endregion

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

                case DisplaySystem.VolumetricRenderMode.MultiViewer:
                    RenderMode = DisplaySystem.VolumetricRenderMode.MultiViewer;
                    break;
            }
        }

        #region Rendering

        private static int GetRenderTextureWidth(Texture target)
            => target == null ? UnityEngine.Display.main.renderingWidth : target.width;

        private static int GetRenderTextureHeight(Texture target)
            => target == null ? UnityEngine.Display.main.renderingHeight : target.height;

        protected override void RenderEyePass(Camera.MonoOrStereoscopicEye eye, RenderTexture cameraRenderTexture)
        {
            // Store matrices
            GL.PushMatrix();

            // Get the size of the render target
            var width = GetRenderTextureWidth(cameraRenderTexture);
            var height = GetRenderTextureHeight(cameraRenderTexture);

            // Sets the target
            Graphics.SetRenderTarget(cameraRenderTexture);
            GL.LoadPixelMatrix(0, width, height, 0);

            if (RenderMode.Equals(DisplaySystem.VolumetricRenderMode.Flat))
            {
                DrawFlat(width, height);
            }

            if (RenderMode.Equals(DisplaySystem.VolumetricRenderMode.SingleViewer))
            {
                DrawSingleViewer(Display.SingleViewer, eye, width, height);
            }

            if (RenderMode.Equals(DisplaySystem.VolumetricRenderMode.MultiViewer))
            {
                DrawMultiViewer(eye, width, height);
            }

            // Restores previous matrices
            GL.PopMatrix();
        }

        private void DrawFlat(int width, int height)
        {
            _quadProjectionMaterial.DisableKeyword("VIEWER");
            _quadProjectionMaterial.SetTexture("_FlatTex", Display.FlatTexture);
            _quadProjectionMaterial.SetMatrixArray("_FlatWorldToClips", Display.CubemapCameraRig.WorldToClips);

            DrawProjectors(_blank, width, height);
        }

        private void DrawSingleViewer(Viewer viewer, Camera.MonoOrStereoscopicEye eye, int width, int height)
        {
            // Set display information
            _quadProjectionMaterial.SetMatrix("_VolumeToWorld", Display.VolumeToWorld);
            _quadProjectionMaterial.SetMatrix("_VolumeToWorldNormal", Display.VolumeToWorldNormal);

            // Set viewer information
            _quadProjectionMaterial.EnableKeyword("VIEWER");
            _quadProjectionMaterial.SetVector("_ViewerPosition", viewer.GetEyeTransform(eye).position);
            _quadProjectionMaterial.SetMatrix("_ViewerMatrix", viewer.GetShaderMatrix(eye, false));
            DrawProjectors(viewer.GetEyeTexture(eye), width, height);
        }

        private void DrawMultiViewer(Camera.MonoOrStereoscopicEye eye, int width, int height)
        {
            Viewer viewer;

            switch (eye)
            {
                case Camera.MonoOrStereoscopicEye.Mono:
                    // TODO: Detect this in SetCompatibleRenderMode() and fallback to single or flat render mode.
                    Debug.LogWarning("Warning! Rendering <b>Mono</b> in the <b>Multi Viewer</b> render mode.");
                    viewer = Display.PrimaryViewer;
                    break;

                case Camera.MonoOrStereoscopicEye.Left:
                    viewer = Display.PrimaryViewer;
                    break;

                case Camera.MonoOrStereoscopicEye.Right:
                    viewer = Display.SecondaryViewer;
                    break;
                default:
                    throw new ArgumentException($"Invalid eye: {eye} is not a valid enum.");
            }

            eye = Camera.MonoOrStereoscopicEye.Mono;

            // Draw the given viewer ( from the left eye )
            // TODO: Properly determine the mono matrix is correctly working with stereo calibration
            _quadProjectionMaterial.EnableKeyword("VIEWER");
            // Set display information
            _quadProjectionMaterial.SetMatrix("_VolumeToWorld", Display.VolumeToWorld);

            // Set viewer information
            _quadProjectionMaterial.SetVector("_ViewerPosition", viewer.GetEyeTransform(eye).position);
            _quadProjectionMaterial.SetMatrix("_ViewerMatrix", viewer.GetShaderMatrix(eye, false));

            DrawProjectors(viewer.GetEyeTexture(eye), width, height);
        }

        private void DrawProjectors(Texture texture, int projectorPixelWidth, int projectorPixelHeight)
        {
            // Width of 1 projectors influence
            var w = projectorPixelWidth / (float)Display.Calibration.Projectors.Count;

            // Draw for N projectors
            for (var i = 0; i < Display.Calibration.Projectors.Count; i++)
            {
                var projector = Display.Calibration.Projectors[_ordering[i]];
                _quadProjectionMaterial.SetTexture("_ProjectorTex", projector.Texture);
                _quadProjectionMaterial.SetTexture("_NormalMap", projector.NormalMap);
                Graphics.DrawTexture(new Rect(w * i, 0, w, projectorPixelHeight), texture, _quadProjectionMaterial);
            }
        }

        #endregion

        #region Display Setup ( Calibration and Physical Matrix )

        protected internal override Calibrations.Display.DisplayCalibration LoadDisplayCalibration()
        {
            // Load projector geometry
            var projTextures = new GeometryTexture[Config.MosaicRenderer.Count];
            for (var i = 0; i < projTextures.Length; i++)
            {
                projTextures[i] = Calibrations.Display.DisplayCalibration.LoadProjectorGeometry(i);
            }

            // Load camera geometry
            var camTextures = new[] { Calibrations.Display.DisplayCalibration.LoadCameraGeometry() };

            return new Calibrations.Display.DisplayCalibration(projTextures, camTextures);
        }

        protected internal override Matrix4x4 ComputePhysicalToVolumeMatrix()
        {
            var factor = 0.5F / Config.MosaicRenderer.Radius; // 1v / 0.3048m
            return Matrix4x4.Scale(Vector3.one * factor);
        }

        #endregion

        private void CreateBlitCamera()
        {
            var blit = gameObject.AddComponent<Camera>();
            // blit.hideFlags |= HideFlags.HideAndDontSave;
            blit.clearFlags = CameraClearFlags.Skybox;
            blit.stereoTargetEye = XRSettings.enabled ? StereoTargetEyeMask.Both : StereoTargetEyeMask.None;
            blit.orthographic = true;
            blit.orthographicSize = 1;
            blit.cullingMask = 0;
            blit.depth = 10;
        }
    }
}