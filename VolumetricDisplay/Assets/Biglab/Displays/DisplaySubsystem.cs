using UnityEngine;

namespace Biglab.Displays
{
    public abstract class DisplaySubsystem : MonoBehaviour
    {
        public abstract void SetCompatibleRenderMode(DisplaySystem.VolumetricRenderMode expectedMode);

        public DisplaySystem.VolumetricRenderMode RenderMode { get; protected set; }

        protected DisplaySystem Display => DisplaySystem.Instance;

        protected internal abstract Calibrations.Display.DisplayCalibration LoadDisplayCalibration();

        protected internal abstract Matrix4x4 ComputePhysicalToVolumeMatrix();

        protected abstract void RenderEyePass(Camera.MonoOrStereoscopicEye eye, RenderTexture cameraRenderTexture);
    }
}