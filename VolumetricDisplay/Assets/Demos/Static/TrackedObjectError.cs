using System.Collections;
using System.Collections.Generic;

using Biglab.Calibrations.HeadToView;
using Biglab.Displays;
using Biglab.Math;
using Biglab.Remote;
using Biglab.Tracking;

using UnityEngine;

public class TrackedObjectError : MonoBehaviour
{
    public TrackedObject TargetObject;

    [Tooltip("Small error magnitude. x - translation error magnitude. y - rotation error degrees")]
    public Vector2 SmallError = new Vector2(0.01f, 1f);
    [Tooltip("Medium error magnitude. x - translation error magnitude. y - rotation error degrees")]
    public Vector2 MediumError = new Vector2(0.05f, 5f);
    [Tooltip("Large error magnitude. x - translation error magnitude. y - rotation error degrees")]
    public Vector2 LargeError = new Vector2(0.10f, 10f);

    private RemoteMenuDropdown _magnitudeDropdown;

    private CalibrationGroup[] _calibrations;

    private IEnumerator Start()
    {
        // Wait for the ground truth calibration to be computed
        // These waits are probably excessive, but I'm tired right now
        yield return TrackingSystem.Instance.GetWaitForSubsystem();

        yield return DisplaySystem.Instance.GetWaitForPrimaryViewer();

        yield return new WaitForSeconds(0.25f);

        var originalCalibration = TargetObject.Calibrations;

        var smallCalibration = originalCalibration.Clone();
        AddError(smallCalibration, SmallError.x, SmallError.y);

        var mediumCalibration = originalCalibration.Clone();
        AddError(mediumCalibration, MediumError.x, MediumError.y);

        var largeCalibration = originalCalibration.Clone();
        AddError(largeCalibration, LargeError.x, LargeError.y);

        _calibrations = new[]
        {
            TargetObject.Calibrations,
            smallCalibration,
            mediumCalibration,
            largeCalibration
        };

        _magnitudeDropdown = gameObject.AddComponent<RemoteMenuDropdown>();
        _magnitudeDropdown.Group = "Calibration Error";
        _magnitudeDropdown.Order = 0;
        _magnitudeDropdown.Options = new List<string>
        {
            "None",
            "Small",
            "Medium",
            "Large"
        };
        _magnitudeDropdown.Selected = 0;
        _magnitudeDropdown.ValueChanged += MagnitudeDropdown_ValueChanged;
    }

    private void AddError(CalibrationGroup group, float offsetMagnitude, float rotationMagnitude)
    {
        foreach (var calibration in group.Calibrations)
        {
            calibration.OffsetInView += Random.rotation * Vector3.up * offsetMagnitude;
            calibration.ViewToHeadRotation *= RandomUtilities.RandomClampedRotation(rotationMagnitude);
        }
    }

    private void MagnitudeDropdown_ValueChanged(int index, Biglab.IO.Networking.INetworkConnection connection)
    {
        if (index >= 0 && index < _calibrations.Length)
        {
            Debug.Log($"Index: {index}.");
            
            TrackingSystem.Instance.SetCalibrations(
                TargetObject.ObjectKind, 
                TargetObject.ObjectIndex,
                _calibrations[index]);
        }
    }
}
