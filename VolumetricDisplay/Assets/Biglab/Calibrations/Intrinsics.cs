using UnityEngine;

namespace Biglab.Calibrations
{
    /// <summary>
    /// See https://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html?highlight=calib
    /// Parameters to encode the projection model of a camera or projector
    /// </summary>
    [CreateAssetMenu(fileName = "Intrinsics", menuName = "Biglab/Calibrations/Intrinsics", order = 1)]
    public class Intrinsics : ScriptableObject
    {
        [Header("Intrinsic Parameters")] [Tooltip("Number of pixels horizontally")]
        public int PixelWidth;

        [Tooltip("Number of pixels vertically")]
        public int PixelHeight;

        [Tooltip("Focal lengths (F) in pixel units.")]
        public Vector2 FocalLengths;

        [Tooltip("Skew coefficient alpha, which is non-zero if the image axes are not perpendicular.")]
        public float SkewCoefficientAlpha;

        [Tooltip("Optical center (the principle point), in pixels.")]
        public Vector2 OpticalCenter;

        [Header("Distortion Parameters")] [Tooltip("Radial distortion coefficients of the lens (k1, k2, k3).")]
        public Vector3 RadialDistortionCoefficients;

        [Tooltip("Tangential distortion coefficients of the lens (p1, p2).")]
        public Vector2 TangentialDistortionCoefficients;
    }
}