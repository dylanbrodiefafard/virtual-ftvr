using Biglab.Calibrations.Display;
using Biglab.Extensions;
using Biglab.Utility;
using System.Linq;
using UnityEngine;

namespace Biglab.Displays.Virtual
{
    public class PerspectiveProjectorRenderer : VirtualDisplaySubsystem
    {
        #region MonoBehaviour

        private void Awake()
        {
            RenderMaterial = new Material(Shader.Find("Projector/Perspective"));


            // Create Virtual Display
            var display = Instantiate(GetDisplayPrefab(Config.VirtualRenderer.GetDisplayType()), transform, true);

            // Get and disable all of the biglab projectors in the display
            var virtualProjectors = display.transform.GetComponentsInChildren<BiglabProjector>();
            var ignoreLayerMask = 0;
            foreach (var projector in virtualProjectors)
            {
                projector.enabled = false;
                ignoreLayerMask = projector.ProjectorIgnoreMask;
            }

            // Create a perspective projector
            var go = new GameObject("Perspective Projector")
            {
                layer = gameObject.layer
            };
            go.transform.parent = transform;
            go.AddComponentWithInit<PerspectiveProjector>(script =>
            {
                script.ProjectorMaterial = RenderMaterial;
                script.IgnoreLayerMask = ignoreLayerMask;
            });
        }

        #endregion

        #region DisplaySubsystem

        protected internal override DisplayCalibration LoadDisplayCalibration()
        {
            return new DisplayCalibration(Enumerable.Empty<GeometryTexture>(), Enumerable.Empty<GeometryTexture>());
        }

        #endregion

        #region Rendering

        protected override void DrawFlat()
        {
            // Draw a projection of the scene onto the display surface
            RenderMaterial.DisableKeyword("VIEWER");

            // Set Cubemap uniforms
            RenderMaterial.SetTexture("_FlatTex", Display.FlatTexture);
            RenderMaterial.SetMatrixArray("_FlatWorldToClips", Display.CubemapCameraRig.WorldToClips);
        }

        protected override void DrawSingleViewer(Viewer viewer, Camera.MonoOrStereoscopicEye eye)
        { 
            // Set display uniforms
            RenderMaterial.SetMatrix("_PhysicalToWorld", Display.PhysicalToWorld);
            RenderMaterial.SetMatrix("_PhysicalToWorldNormal", Display.PhysicalToWorldNormal);

            // Set viewer uniforms
            RenderMaterial.EnableKeyword("VIEWER");
            RenderMaterial.SetVector("_ViewerPosition", viewer.GetEyeTransform(eye).position);
            RenderMaterial.SetMatrix("_ViewerMatrix", viewer.GetShaderMatrix(eye, false));
            RenderMaterial.SetTexture("_MainTex", viewer.GetEyeTexture(eye));
        }

        #endregion
    }
}