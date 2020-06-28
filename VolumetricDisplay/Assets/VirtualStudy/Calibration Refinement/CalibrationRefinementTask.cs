using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Biglab.Displays;
using Biglab.Displays.Virtual;
using Biglab.Extensions;
using Biglab.IO.Logging;
using Biglab.Math;
using Biglab.Tracking;
using Biglab.Utility;

using UnityEngine;
using UnityEngine.UI;

using Debug = UnityEngine.Debug;
using MethodLevel = Biglab.Displays.Virtual.OculusPresentationMethodController.PresentationMethod;

public class CalibrationRefinementTask : MonoBehaviour
{
    public List<MethodLevel> Levels;
    public TableDescription CalibrationRefinementTableDescription;
    public int NumberOfTrialsPerLevel = 5;
    public float OffsetErrorMagnitude = 0.16f;
    public Text StatusText;

    // Dependencies
    private VirtualSurface _surface;
    private CsvTableWriter _writer;
    private Stopwatch _stopwatch;
    private OculusPresentationMethodController _methodController;

    // Helpful stuff
    private MethodLevel CurrentLevel => _trials[_trialIndex];

    private Viewer _studyViewer => DisplaySystem.Instance.PrimaryViewer;

    private TrackedObject _viewerTrackedObject => _studyViewer.GetComponent<TrackedObject>();

    // State
    private int _trialIndex;
    private bool _isDone;
    private List<MethodLevel> _trials;
    private Vector2 _offsetAdded;
    private Vector3 _offsetStart;
    private Transform _leftEyeRenderTarget;
    private Transform _rightEyeRenderTarget;

    private void Awake()
    {
        _leftEyeRenderTarget = new GameObject("Left eye render target").transform;
        _rightEyeRenderTarget = new GameObject("Right eye render target").transform;

        if (CalibrationRefinementTableDescription == null)
        {
            throw new ArgumentNullException(nameof(CalibrationRefinementTableDescription));
        }

        _trials = new List<MethodLevel>(NumberOfTrialsPerLevel * Levels.Count);

        foreach (var level in Levels)
        {
            for (var i = 0; i < NumberOfTrialsPerLevel; i++)
            {
                _trials.Add(level);
            }
        }

        _trials.Shuffle();

        _stopwatch = new Stopwatch();

        // Create writer
        _writer = gameObject.AddComponent<CsvTableWriter>();
        _writer.TableDescription = CalibrationRefinementTableDescription;
        _writer.EnableDatePrefix = false;
        _writer.FilePath = VirtualStudy.Config.GetDataFilepath(gameObject.name);
    }

    private IEnumerator Start()
    {
        // Wait for things to get setup
        yield return DisplaySystem.Instance.GetWaitForPrimaryViewer();
        yield return new WaitForEndOfFrame();

        _methodController = FindObjectOfType<OculusPresentationMethodController>();

        if (_methodController == null)
        {
            throw new ArgumentNullException(nameof(_methodController));
        }

        _surface = FindObjectOfType<VirtualSurface>();

        if (_surface == null)
        {
            throw new ArgumentNullException(nameof(_surface));
        }

        if (Levels.Count < 1)
        {
            throw new InvalidOperationException($"{nameof(Levels)} must have at least 1 element.");
        }

        yield return GetWaitSwitchLevels(MethodLevel.BinocularStereo, false);

        // Do some training
        yield return GetWaitTraining();

        yield return GetWaitStartCalibration(CurrentLevel, "Press any button to start");

        // Start capturing input
        StartCoroutine(InputLoop());
    }

    private void AddRandomOffsetToViewer(float magnitude)
    {
        // Record state variables
        var xyOffset = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized * magnitude;
        _offsetAdded = new Vector2
        {
            x = xyOffset.x,
            y = xyOffset.y,
        };

        // Update the viewer manually
        TrackedObject.UpdateAnchors(_viewerTrackedObject.Calibrations, _viewerTrackedObject.TrackingAnchor, _studyViewer.transform, _studyViewer.LeftAnchor, _studyViewer.RightAnchor);

        // Add 5 degrees of error
        var spherical = MathB.CartesianToSpherical(_studyViewer.transform.position);
        spherical.y += xyOffset.x * Mathf.Deg2Rad;
        spherical.z += xyOffset.y * Mathf.Deg2Rad;

        _studyViewer.transform.position = MathB.SphericalToCartesian(spherical);
        _studyViewer.transform.rotation = Quaternion.LookRotation(
            (VolumetricCamera.Instance.transform.position - _studyViewer.transform.position).normalized,
            VolumetricCamera.Instance.transform.up);

        // Record the eye anchors starting orientation
        _leftEyeRenderTarget.position = _studyViewer.LeftAnchor.position;
        _leftEyeRenderTarget.rotation = _studyViewer.LeftAnchor.rotation;
        _rightEyeRenderTarget.position = _studyViewer.RightAnchor.position;
        _rightEyeRenderTarget.rotation = _studyViewer.RightAnchor.rotation;

        Debug.Log($"Added offset: {_offsetAdded.ToString("G4")}");
    }

    private IEnumerator WaitForAnyButton(string statusText)
    {
        StatusText.text = statusText;
        yield return OVRInputWait.Instance.AnyButtonUp;
        StatusText.text = string.Empty;
    }

    private IEnumerator GetWaitTraining()
    {
        yield return WaitForAnyButton("Press any button to start training");

        do
        {
            yield return GetWaitSwitchLevels(Levels.RandomElement());

            _viewerTrackedObject.enabled = false;

            StatusText.text = "Press button 1 to record your answer. Press button 2 to quit training";

            yield return new WaitForEndOfFrame();

            yield return new WaitUntil(() => OVRInput.GetUp(OVRInput.Button.One) || OVRInput.GetUp(OVRInput.Button.Two));

            if (OVRInput.GetUp(OVRInput.Button.Two)) { break; }

            _viewerTrackedObject.enabled = true;

            yield return WaitForAnyButton("Press any button to continue");

        } while (enabled);
    }

    private IEnumerator GetWaitStartCalibration(MethodLevel level, string waitMessage)
    {
        _viewerTrackedObject.enabled = true;

        StatusText.text = waitMessage;

        yield return new WaitForEndOfFrame();

        yield return new WaitUntil(() => OVRInput.GetDown(OVRInput.Button.One));

        _viewerTrackedObject.enabled = false;

        StatusText.text = "Press button 1 to record your answer";

        yield return GetWaitSwitchLevels(level);

        _stopwatch.Start();
    }

    private IEnumerator GetWaitSwitchLevels(MethodLevel level, bool addOffset = true)
    {
        yield return _surface.GetWaitFadeToBlack(0.5f);
        yield return new WaitForSeconds(0.25f);
        _methodController.ConfigureViewerForPresentationMethod(level);
        if (addOffset) { AddRandomOffsetToViewer(OffsetErrorMagnitude); }
        yield return new WaitForSeconds(0.25f);
        yield return _surface.GetWaitFadeToOriginal(0.5f);
    }

    private IEnumerator RecordAndGotoNext()
    {
        _stopwatch.Stop();

        // Update the viewer to record the data
        _studyViewer.EnabledStereoRendering = true;
        TrackedObject.UpdateAnchors(_viewerTrackedObject.Calibrations, _viewerTrackedObject.TrackingAnchor, _studyViewer.transform, _studyViewer.LeftAnchor, _studyViewer.RightAnchor);

        // Compute the metrics
        var leftEyeDisplacement = _leftEyeRenderTarget.InverseTransformPoint(_studyViewer.LeftAnchor.position).Multiply(DisplaySystem.Instance.WorldToPhysical.ToScale());
        var leftEyeImagePlaneMagnitude = new Vector2(leftEyeDisplacement.x, leftEyeDisplacement.y).magnitude;
        var leftEyeDepthMagnitude = Mathf.Abs(leftEyeDisplacement.z);
        var rightEyeDisplacement = _rightEyeRenderTarget.InverseTransformPoint(_studyViewer.RightAnchor.position).Multiply(DisplaySystem.Instance.WorldToPhysical.ToScale());
        var rightEyeImagePlaneMagnitude = new Vector2(rightEyeDisplacement.x, rightEyeDisplacement.y).magnitude;
        var rightEyeDepthMagnitude = Mathf.Abs(rightEyeDisplacement.z);

        Debug.Log($"{nameof(DisplaySystem.Instance.WorldToPhysical)}.ToScale(): {DisplaySystem.Instance.WorldToPhysical.ToScale().ToString("G4")}");
        Debug.Log($"Left eye start: {_leftEyeRenderTarget.position.ToString("G4")}, end: {_studyViewer.LeftAnchor.position.ToString("G4")}, ipm: {leftEyeImagePlaneMagnitude:G4}, dm: {leftEyeDepthMagnitude:G4}");
        Debug.Log($"Right eye start: {_rightEyeRenderTarget.position.ToString("G4")}, end: {_studyViewer.RightAnchor.position.ToString("G4")}, ipm: {rightEyeImagePlaneMagnitude:G4}, dm: {rightEyeDepthMagnitude:G4}");

        // Collect and record data

        const string vectorFormat = "G4";

        _writer.SetField("Offset Added", _offsetAdded.ToString(vectorFormat));
        _writer.SetField("L Start Position", _leftEyeRenderTarget.position.ToString(vectorFormat));
        _writer.SetField("L End Position", _studyViewer.LeftAnchor.position.ToString(vectorFormat));
        _writer.SetField("L Displacement", leftEyeDisplacement.ToString(vectorFormat));
        _writer.SetField("L IPM", leftEyeImagePlaneMagnitude);
        _writer.SetField("L DM", leftEyeDepthMagnitude);
        _writer.SetField("R Start Position", _rightEyeRenderTarget.position.ToString(vectorFormat));
        _writer.SetField("R End Position", _studyViewer.RightAnchor.position.ToString(vectorFormat));
        _writer.SetField("R Displacement", rightEyeDisplacement.ToString(vectorFormat));
        _writer.SetField("R IPM", rightEyeImagePlaneMagnitude);
        _writer.SetField("R DM", rightEyeDepthMagnitude);
        _writer.SetField("Level", CurrentLevel);
        _writer.SetField("Time Taken (ms)", _stopwatch.ElapsedMilliseconds);

        _stopwatch.Reset();

        _writer.Commit(); // Write the line

        if (_trialIndex + 1 == _trials.Count)
        {
            Debug.Log("All levels calibrated.");
            _isDone = true;
            yield return _surface.GetWaitFadeToBlack(0.5f);
            _viewerTrackedObject.enabled = true;
            _methodController.ConfigureViewerForPresentationMethod(MethodLevel.BinocularStereo);
            yield return _surface.GetWaitFadeToOriginal(0.5f);

            Scheduler.StartCoroutine(VirtualStudy.Quit(5, StatusText));
            yield break;
        }

        _trialIndex++;

        yield return GetWaitStartCalibration(CurrentLevel, "Press any button to start");
    }

    private IEnumerator InputLoop()
    {
        while (enabled && !_isDone)
        {
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                yield return RecordAndGotoNext();
            }

            yield return new WaitForEndOfFrame();
        }
    }
}
