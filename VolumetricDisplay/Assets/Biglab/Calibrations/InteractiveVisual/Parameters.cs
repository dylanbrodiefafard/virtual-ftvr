using UnityEngine;

namespace Biglab.Calibrations.InteractiveVisual
{
    [CreateAssetMenu(fileName = "Intrinsics", menuName = "Biglab/Calibrations/InteractiveVisual/Parameters", order = 1)]
    public class Parameters : ScriptableObject
    {
        [Tooltip("Number of calibration positions")]
        public int NumberOfSamples = 4;

        [Tooltip("Distance range from the display when calibrating.")]
        public Vector2 DistanceRange = new Vector2(1, 2);

        [Tooltip("Polar angle range for generated calibration positions.")]
        public Vector2 PolarAngleRange = new Vector2(Mathf.PI / 2.5f, Mathf.PI / 6);

        [Tooltip("The random seed for generating calibration positions and fake data.")]
        public int RandomSeed = 4;
    }
}