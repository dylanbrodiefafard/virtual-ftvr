using System.Collections.Generic;
using Biglab.Extensions;
using UnityEngine;
using HVModel = Biglab.Calibrations.HeadToView.Model;
using TDModel = Biglab.Calibrations.TrackingToDisplay.Model;

namespace Biglab.Calibrations.InteractiveVisual
{
    public class Model : MonoBehaviour
    {
        public TDModel TrackingToDisplayModel;
        public HVModel HeadToViewpointsModel;
        public Parameters CalibrationParameters;

        [Header("Calibration")]
        [Tooltip("The variance of noise in each dimension (viewer-aligned) for fake calibration.")]
        public Vector3 NoiseVariance = Vector3.zero;

        #region monobehaviour

        private void Awake()
        {
            if (CalibrationParameters.IsNull())
            {
                CalibrationParameters = Resources.Load<Parameters>("Defaults");
            }
        }

        #endregion monobehaviour

        public static void GenerateSyntheticTrackedData(
            Quaternion trackingToDisplayRotation,
            Vector3 trackingToDisplayTranslation,
            Vector3 trackingToDisplayScale,
            Vector3 leftOrMonoOffsetInView,
            Vector3 rightOffsetInView,
            Quaternion leftOrMonoHeadToView,
            Quaternion rightHeadToView,
            IEnumerable<Vector3> calibrationPositions,
            Vector3 noiseVariance,
            bool isStereo,
            out List<Vector3> leftOrMonoSyntheticPositions,
            out List<Quaternion> leftOrMonoSyntheticRotations,
            out List<Vector3> rightSyntheticPositions,
            out List<Quaternion> rightSyntheticRotations)
        {
            // Create the collections
            leftOrMonoSyntheticPositions = new List<Vector3>();
            rightSyntheticPositions = new List<Vector3>();
            leftOrMonoSyntheticRotations = new List<Quaternion>();
            rightSyntheticRotations = new List<Quaternion>();

            var trackingToDisplay = Matrix4x4.TRS(trackingToDisplayTranslation, trackingToDisplayRotation,
                trackingToDisplayScale);

            var displayToTracker = trackingToDisplay.inverse;

            //var displayInTracking = displayToTracker.MultiplyPoint3x4(Vector3.zero);
            //Debug.Log("Rotation: " + trackingToDisplay.rotation.eulerAngles.ToString("G4"));
            //Debug.Log("Translation: " + trackingToDisplayTranslation.ToString("G4"));

            /* Setup gameobjects to do the math for us */
            var syntheticLeftOrMono = new GameObject("s_eye_left");
            var syntheticRight = new GameObject("s_eye_right");
            var syntheticTrackedpoint = new GameObject("s_head");
            var leftOrMonoEye = syntheticLeftOrMono.transform;
            var rightEye = syntheticRight.transform;
            var anchor = syntheticTrackedpoint.transform;

            leftOrMonoEye.position = Vector3.zero;
            rightEye.position = Vector3.zero;
            leftOrMonoEye.rotation = Quaternion.identity;
            rightEye.rotation = Quaternion.identity;

            foreach (var position in calibrationPositions)
            {
                anchor.parent = leftOrMonoEye;
                anchor.localPosition = leftOrMonoOffsetInView;
                anchor.localRotation = leftOrMonoHeadToView;
                // Put the eye in the exact position with the correct rotation
                leftOrMonoEye.position = position;
                leftOrMonoEye.rotation = Quaternion.LookRotation(-position.normalized, Vector3.up);

                // Generate the noise vector
                var nV = Vector3.zero;
                if (noiseVariance.x > 0)
                {
                    nV.x = Random.Range(-noiseVariance.x, noiseVariance.x);
                }

                if (noiseVariance.y > 0)
                {
                    nV.y = Random.Range(-noiseVariance.y, noiseVariance.y);
                }

                if (noiseVariance.z > 0)
                {
                    nV.z = Random.Range(-noiseVariance.z, noiseVariance.z);
                }

                // Add noise to the recording
                leftOrMonoEye.position += leftOrMonoEye.TransformPoint(nV);

                // Record the position and rotation
                leftOrMonoSyntheticPositions.Add(displayToTracker.MultiplyPoint3x4(anchor.position));
                leftOrMonoSyntheticRotations.Add(displayToTracker.rotation * anchor.rotation);

                //var calibrationToDisplay = Matrix4x4.LookAt(leftOrMonoEye.position, Vector3.zero, Vector3.up);
                //var displayToCalibration = calibrationToDisplay.inverse;
                //var o = displayToCalibration.MultiplyPoint3x4(trackingToDisplay.rotation * leftOrMonoSyntheticPositions.Last() +
                //                                              trackingToDisplayTranslation);
                //Debug.Log("Head: " + leftOrMonoSyntheticPositions.Last().ToString("G4"));
                //Debug.Log("Offset: " + o.ToString("G4"));
                if (!isStereo)
                {
                    continue;
                }

                anchor.parent = rightEye;
                anchor.localPosition = rightOffsetInView;
                anchor.localRotation = rightHeadToView;
                // Put the eye in the exact position with the correct rotation
                rightEye.position = position;
                rightEye.rotation = Quaternion.LookRotation(-position.normalized, Vector3.up);

                // Generate the noise vector
                nV = Vector3.zero;
                if (noiseVariance.x > 0)
                {
                    nV.x = Random.Range(-noiseVariance.x, noiseVariance.x);
                }

                if (noiseVariance.y > 0)
                {
                    nV.y = Random.Range(-noiseVariance.y, noiseVariance.y);
                }

                if (noiseVariance.z > 0)
                {
                    nV.z = Random.Range(-noiseVariance.z, noiseVariance.z);
                }

                // Add noise to the recording
                rightEye.position += rightEye.TransformPoint(nV);

                // Record the position and rotation
                rightSyntheticPositions.Add(displayToTracker.MultiplyPoint3x4(anchor.position));
                rightSyntheticRotations.Add(displayToTracker.rotation * anchor.rotation);
            }

            DestroyImmediate(syntheticLeftOrMono);
            DestroyImmediate(syntheticRight);
            DestroyImmediate(syntheticTrackedpoint);
        }
    }
}