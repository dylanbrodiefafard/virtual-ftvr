using System.Linq;
using Biglab.Utility;
using UnityEngine;
using UnityEngine.XR;
using DCalibration = Biglab.Calibrations.Display.DisplayCalibration;

namespace Biglab.Displays.Virtual
{
    public class BiglabProjectorRenderer : VirtualDisplaySubsystem
    {
        private BiglabProjector[] _virtualProjectors;
        private Camera _blit;

        private static Camera CreateBlitCamera(GameObject obj)
        {
            var blit = obj.AddComponent<Camera>();
            // blit.hideFlags |= HideFlags.HideAndDontSave;
            // blit.targetDisplay = 7;
            blit.clearFlags = CameraClearFlags.SolidColor;
            blit.backgroundColor = Color.black;
            blit.stereoTargetEye = XRSettings.enabled ? StereoTargetEyeMask.Both : StereoTargetEyeMask.None;
            blit.orthographic = true;
            blit.orthographicSize = 1;
            blit.cullingMask = 0;
            blit.depth = 100;
            blit.enabled = false;

            return blit;
        }

        #region MonoBehaviour

        private void Awake()
        {
            RenderMaterial = new Material(Shader.Find("Biglab/Spheree/Projector"));

            // Create Virtual Display
            var display = Instantiate(GetDisplayPrefab(Config.VirtualRenderer.GetDisplayType()), transform, true);

            // Get and configure each virtual projector
            _virtualProjectors = display.transform.GetComponentsInChildren<BiglabProjector>();
            foreach (var projector in _virtualProjectors)
            {
                projector.ImageSource = new RenderTexture(projector.ProjectorIntrinsics.PixelWidth,
                    projector.ProjectorIntrinsics.PixelHeight, 0)
                {
                    name = projector.name + " Image Source"
                };
            }

            _blit = CreateBlitCamera(gameObject);
            _blit.stereoTargetEye = StereoTargetEyeMask.Both;
            _blit.backgroundColor = new Color(0, 0, 1);
        }

        #endregion

        #region DisplaySubsystem

        protected internal override DCalibration LoadDisplayCalibration()
        {
            // Generate projector textures
            var projTextures = new GeometryTexture[_virtualProjectors.Length];
            for (var i = 0; i < projTextures.Length; i++)
            {
                projTextures[i] = _virtualProjectors[i].GenerateGeometryTexture(DisplaySpace);
            }

            // No camera geometry
            var camTextures = Enumerable.Empty<GeometryTexture>();

            return new DCalibration(projTextures, camTextures);
        }

        #endregion

        #region Rendering

        protected override void DrawFlat()
        {
            RenderMaterial.DisableKeyword("VIEWER");
            RenderMaterial.SetTexture("_FlatTex", Display.FlatTexture);
            RenderMaterial.SetMatrixArray("_FlatWorldToClips", Display.CubemapCameraRig.WorldToClips);

            // Draw for all projectors 
            for (var i = 0; i < _virtualProjectors.Length; i++)
            {
                var projector = _virtualProjectors[i];
                var destination = projector.ImageSource as RenderTexture;

                if (destination == null)
                {
                    Debug.LogWarning($"Projector {projector} {nameof(BiglabProjector.ImageSource)} is null.");
                    continue;
                }

                var calibration = Display.Calibration.Projectors[i];
                RenderMaterial.SetTexture("_NormalMap", calibration.NormalMap);
                Graphics.Blit(null, destination, RenderMaterial);
            }
        }

        protected override void DrawSingleViewer(Viewer viewer, Camera.MonoOrStereoscopicEye eye)
        {
            // Set display uniforms
            RenderMaterial.SetMatrix("_VolumeToWorld", Display.VolumeToWorld);
            RenderMaterial.SetMatrix("_VolumeToWorldNormal", Display.VolumeToWorldNormal);

            // Set viewer uniforms
            RenderMaterial.EnableKeyword("VIEWER");
            RenderMaterial.SetVector("_ViewerPosition", viewer.GetEyeTransform(eye).position);
            RenderMaterial.SetMatrix("_ViewerMatrix", viewer.GetShaderMatrix(eye, false));

            // Draw for N projectors 
            for (var i = 0; i < _virtualProjectors.Length; i++)
            {
                var projector = _virtualProjectors[i];
                var destination = projector.ImageSource as RenderTexture;

                if (destination == null)
                {
                    Debug.LogWarning($"Projector {projector} {nameof(BiglabProjector.ImageSource)} is null.");
                    continue;
                }

                // Store matrices
                GL.PushMatrix();

                // Sets the target
                Graphics.SetRenderTarget(destination);

                //
                GL.LoadPixelMatrix(0, destination.width, destination.height, 0);

                // Set projector uniforms
                var calibration = Display.Calibration.Projectors[i];
                RenderMaterial.SetTexture("_ProjectorTex", calibration.Texture);
                RenderMaterial.SetTexture("_NormalMap", calibration.NormalMap);

                // Blit the image
                Graphics.Blit(viewer.GetEyeTexture(eye), destination, RenderMaterial);

                // Restores previous matrices
                GL.PopMatrix();
            }
        }

        #endregion
    }
}