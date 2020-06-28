using System.Collections;
using System.ComponentModel;
using Biglab.Extensions;
using UnityEngine;

namespace Biglab.Displays.Virtual
{
    public class OculusPresentationMethodController : MonoBehaviour
    {
        /// <summary>
        /// Binocular - Images will be rendered from two viewpoints. The images may be the same (NonStereo) or different (Stereo).
        /// Stereo - Only compatible with binocular viewing. Two images are rendered from separate viewpoints.
        /// NonStereo - Image(s) is/are rendered from one viewpoint.
        /// Dominant - Image will be rendered to the dominant viewpoint.
        /// NonDominant - Image will be rendered to the nondominant viewpoint.
        /// Mean - Image will be rendered to the average of the left and right viewpoints.
        /// </summary>
        public enum PresentationMethod
        {
            MonocularNonStereoDominant = 0,
            MonocularNonStereoNonDominant,
            BinocularNonStereoMean,
            BinocularNonStereoDominant,
            BinocularNonStereoNonDominant,
            BinocularStereo
        }

        private static Viewer PrimaryViewer => DisplaySystem.Instance.PrimaryViewer;

        public OVRCameraRig OculusCamera;

        public Camera.StereoscopicEye PreferredEye = Camera.StereoscopicEye.Right;

        private Camera PreferredEyeCamera => PreferredEye.Equals(Camera.StereoscopicEye.Right)
            ? OculusCamera.rightEyeCamera
            : OculusCamera.leftEyeCamera;
        
        private Camera NonPreferredEyeCamera => PreferredEye.Equals(Camera.StereoscopicEye.Right)
            ? OculusCamera.leftEyeCamera
            : OculusCamera.rightEyeCamera;

        private Camera.MonoOrStereoscopicEye DominantEye => PreferredEye == Camera.StereoscopicEye.Left
            ? Camera.MonoOrStereoscopicEye.Left
            : Camera.MonoOrStereoscopicEye.Right;

        private Camera.MonoOrStereoscopicEye NonDominantEye => PreferredEye == Camera.StereoscopicEye.Left
            ? Camera.MonoOrStereoscopicEye.Right
            : Camera.MonoOrStereoscopicEye.Left;

        private bool _isReady;
        private int _originalCullingMask;
        private const int _blankCullingMask = 0;

        #region MonoBehaviour

        private IEnumerator Start()
        {
            yield return DisplaySystem.Instance.GetWaitForPrimaryViewer();

            if (OculusCamera == null)
            {
                this.FindComponentReference(ref OculusCamera);
            }

            _originalCullingMask = OculusCamera.leftEyeCamera.cullingMask; // Save the culling mask for later

            // Start in the expected mode
            ConfigureViewerForPresentationMethod(PresentationMethod.BinocularStereo);

            _isReady = true;
        }

        private void Update()
        {
            if (!_isReady)
            {
                return;
            }

            if (Input.GetKey(KeyCode.F7))
            {
                ConfigureViewerForPresentationMethod(PresentationMethod.MonocularNonStereoNonDominant);
            }
            else if (Input.GetKey(KeyCode.F8))
            {
                ConfigureViewerForPresentationMethod(PresentationMethod.MonocularNonStereoDominant);
            }
            else if (Input.GetKey(KeyCode.F9))
            {
                ConfigureViewerForPresentationMethod(PresentationMethod.BinocularNonStereoNonDominant);
            }
            else if (Input.GetKey(KeyCode.F10))
            {
                ConfigureViewerForPresentationMethod(PresentationMethod.BinocularNonStereoDominant);
            }
            else if (Input.GetKey(KeyCode.F11))
            {
                ConfigureViewerForPresentationMethod(PresentationMethod.BinocularNonStereoMean);
            }
            else if (Input.GetKey(KeyCode.F12))
            {
                ConfigureViewerForPresentationMethod(PresentationMethod.BinocularStereo);
            }
        }

        #endregion

        /// <summary>
        /// Called when [remote presentation method changed].
        /// </summary>
        /// <param name="id">The identifier from the remote dropdown. Assumes that this corresponds to the enum.</param>
        public void OnRemotePresentationMethodChanged(int id) =>
            ConfigureViewerForPresentationMethod((PresentationMethod)id);

        #region Presentation Method Factors

        private static void EnableStereoRendering(Viewer viewer, bool isEnabled, Camera.MonoOrStereoscopicEye fallbackEye = Camera.MonoOrStereoscopicEye.Mono)
        {
            viewer.EnabledStereoRendering = isEnabled;
            if (!isEnabled)
            {
                viewer.NonStereoFallbackEye = fallbackEye;
            }
        }

        private static void EnableBinocular(Camera preferred, Camera nonPreferred, int cameraCullingMask,
            bool isEnabled)
        {
            EnableCamera(preferred, true, cameraCullingMask);
            EnableCamera(nonPreferred, isEnabled, cameraCullingMask);
        }

        private static void EnableCamera(Camera oculusCamera, bool isEnabled, int cameraCullingMask)
        {
            if (isEnabled)
            {
                oculusCamera.cullingMask = cameraCullingMask;
                oculusCamera.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                oculusCamera.cullingMask = _blankCullingMask;
                oculusCamera.clearFlags = CameraClearFlags.Color;
                oculusCamera.backgroundColor = Color.black;
            }
        }

        #endregion

        public void ConfigureViewerForPresentationMethod(PresentationMethod method)
        {
            switch (method)
            {
                case PresentationMethod.BinocularNonStereoMean:
                    EnableStereoRendering(PrimaryViewer, false);
                    EnableBinocular(PreferredEyeCamera, NonPreferredEyeCamera, _originalCullingMask, true);
                    return;

                case PresentationMethod.BinocularNonStereoDominant:
                    EnableStereoRendering(PrimaryViewer, false, DominantEye);
                    EnableBinocular(PreferredEyeCamera, NonPreferredEyeCamera, _originalCullingMask, true);
                    return;

                case PresentationMethod.BinocularNonStereoNonDominant:
                    EnableStereoRendering(PrimaryViewer, false, NonDominantEye);
                    EnableBinocular(NonPreferredEyeCamera, PreferredEyeCamera, _originalCullingMask, true);
                    return;

                case PresentationMethod.MonocularNonStereoDominant:
                    EnableStereoRendering(PrimaryViewer, true);
                    EnableBinocular(PreferredEyeCamera, NonPreferredEyeCamera, _originalCullingMask, false);
                    return;

                case PresentationMethod.MonocularNonStereoNonDominant:
                    EnableStereoRendering(PrimaryViewer, true);
                    EnableBinocular(NonPreferredEyeCamera, PreferredEyeCamera, _originalCullingMask, false);
                    return;

                case PresentationMethod.BinocularStereo:
                    EnableStereoRendering(PrimaryViewer, true);
                    EnableBinocular(PreferredEyeCamera, NonPreferredEyeCamera, _originalCullingMask, true);
                    return;

                default:
                    throw new InvalidEnumArgumentException($"Unknown {typeof(PresentationMethod)}: {method}.");
            }
        }
    }
}