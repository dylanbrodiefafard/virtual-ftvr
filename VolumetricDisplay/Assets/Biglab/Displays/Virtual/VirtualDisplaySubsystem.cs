using System;
using Biglab.Utility;
using Biglab.Utility.Controllers;
using UnityEngine;
using UnityEngine.XR;

namespace Biglab.Displays.Virtual
{
    public abstract class VirtualDisplaySubsystem : DisplaySubsystem
    {
        public enum DisplayType
        {
            Sphere,
        };

        protected Material RenderMaterial;

        // This is for setting up eye callbacks and placing the viewer properly
        private VirtualViewer _virtualViewer;

        // This is used for calibrations (tracking and geometry)
        public Transform DisplaySpace => _virtualDisplay?.transform;

        // This is used for TrackingToPhysical calibrations
        private VirtualPhysicalDisplay _virtualDisplay;

        protected GameObject GetDisplayPrefab(DisplayType type)
        {
            switch (type)
            {
                case DisplayType.Sphere:
                    return Resources.Load<GameObject>("Virtual Sphere Display");

                default:
                    throw new NotImplementedException($"Unknown display {type}");
            }
        }

        public void RegisterVirtualPhysicalDisplay(VirtualPhysicalDisplay virtualDisplay)
        {
            _virtualDisplay = virtualDisplay;
            InstantiateVirtualViewer();
        }

        private void InstantiateVirtualViewer()
        {
            Debug.Log($"Active XR Device Family: <b>{XRDevices.ActiveFamily}</b>");

            // Check if Oculus is running
            var isOculusActiveEnabled = XRDevices.ActiveFamily.Equals(XRDevices.Family.Oculus) && XRSettings.enabled;
            var isDesktopStereoEnabled = XRDevices.ActiveFamily == XRDevices.Family.DesktopStereo;

            // Select either the desktop or oculus viewer
            var virtualViewerPrefab = isOculusActiveEnabled
                ? Resources.Load<GameObject>("OVRCameraRig")
                : Resources.Load<GameObject>("OrbitCameraRig");

            var go = Instantiate(virtualViewerPrefab, transform);

            // Virtual viewer prefabs require a VirtualViewer component
            _virtualViewer = go.GetComponent<VirtualViewer>();

            // Setup stereo/mono eye callbacks
            AddEyeCallback(_virtualViewer.LeftEye.gameObject, this, Camera.MonoOrStereoscopicEye.Left);
            AddEyeCallback(_virtualViewer.RightEye.gameObject, this, Camera.MonoOrStereoscopicEye.Right);

            if (isOculusActiveEnabled)
            {
                return;
            }

            if (!isDesktopStereoEnabled)
            {
                _virtualViewer.RightEye.enabled = false;
            }

            // Start the desktop viewer here
            go.transform.position = DisplaySpace.TransformPoint(Vector3.back);

            // Assume the desktop viewer is using OrbitFocusWithLook and set the focus target
            var orbiter = go.GetComponentInChildren<OrbitFocusWithLook>();
            var displayOrigin = new GameObject("Origin");
            displayOrigin.transform.parent = DisplaySpace;
            displayOrigin.transform.position = _virtualDisplay.PhysicalToVolumeTransformation.inverse.MultiplyPoint(Vector3.zero);
            orbiter.Focus = displayOrigin.transform;
        }

        private static void AddEyeCallback(GameObject go, VirtualDisplaySubsystem renderer,
            Camera.MonoOrStereoscopicEye eye)
        {
            // If no desktop stereo, then set the eye callback to mono
            if (XRDevices.ActiveFamily.Equals(XRDevices.Family.Other))
            {
                eye = Camera.MonoOrStereoscopicEye.Mono;
            }

            var component = go.AddComponent<EyeCallback>();
            component.RenderingEye += activeEye => renderer.RenderEyePass(activeEye, null);
            component.Eye = eye;
        }

        protected abstract void DrawFlat();

        protected abstract void DrawSingleViewer(Viewer viewer, Camera.MonoOrStereoscopicEye eye);

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

        protected internal override Matrix4x4 ComputePhysicalToVolumeMatrix()
            => _virtualDisplay?.PhysicalToVolumeTransformation ?? Matrix4x4.identity;
    }
}