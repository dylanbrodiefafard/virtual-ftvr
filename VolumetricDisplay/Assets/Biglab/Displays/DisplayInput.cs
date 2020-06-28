using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Biglab.Displays
{
    public static class DisplayInput
        // TODO: Visit this, make sure it works better
    {
        public const string SphereeTouchLib = "TouchDLL";

        #region DLL Imports

        [DllImport(SphereeTouchLib)]
        private static extern IntPtr generateDetector();

        [DllImport(SphereeTouchLib)]
        private static extern void generateRandomTouchEvent(IntPtr t, bool debug);

        [DllImport(SphereeTouchLib)]
        private static extern bool isInitialized(IntPtr t);

        [DllImport(SphereeTouchLib)]
        private static extern bool isTouched(IntPtr t);

        [DllImport(SphereeTouchLib)]
        private static extern float getTouchX(IntPtr t);

        [DllImport(SphereeTouchLib)]
        private static extern float getTouchY(IntPtr t);

        #endregion

        private static IntPtr DetectorInstance { get; }

        private static DisplaySystem Display { get; }

        public static bool IsTouchCapable => Display != null && Display.Calibration.Cameras.Count > 0 &&
                                             DetectorInstance != IntPtr.Zero;

        static DisplayInput()
        {
            // Generate detector ( initialize )
            DetectorInstance = generateDetector();

            // Check if the detector was truly initialized.
            // CC: I assume this is to double check if the camera is plugged in and such
            if (!isInitialized(DetectorInstance))
            {
                Debug.LogWarning("Unable to initialize spheree touch library.");
                DetectorInstance = IntPtr.Zero;
            }

            // 
            Display = UnityEngine.Object.FindObjectOfType<DisplaySystem>();
            if (Display == null)
            {
                // Problem?
            }
        }

        /// <summary>
        /// Is the display being touched?
        /// </summary>
        public static bool IsTouched
            => IsTouchCapable && isTouched(DetectorInstance);

        /// <summary>
        /// Get the touched position in volume space.
        /// </summary>
        public static Vector3 GetTouchPosition()
        {
            if (IsTouchCapable)
            {
                var camera = Display.Calibration.Cameras[0];
                var u = getTouchX(DetectorInstance) / camera.Width;
                var v = getTouchY(DetectorInstance) / camera.Height;

                // World space position of the touch point
                return camera.SamplePosition(u, v);
            }
            else
            {
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Generates a dummy touch event.
        /// </summary>
        public static void GenerateRandomTouchEvent(bool debug = false)
        {
            if (!IsTouchCapable)
            {
                Debug.LogWarning("Unable to generate false touch event, spheree touch library not initialized.");
            }
            else
            {
                generateRandomTouchEvent(DetectorInstance, debug);
            }
        }
    }
}