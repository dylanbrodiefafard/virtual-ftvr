using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Biglab.Displays;
using Biglab.IO.Networking;
using Biglab.Math;
using Biglab.Remote;
using Biglab.Tracking;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using HVCalibrations = Biglab.Calibrations.HeadToView.CalibrationGroup;
using Random = UnityEngine.Random;
using UnityInput = UnityEngine.Input;

namespace Biglab.Calibrations.InteractiveVisual
{
    public class Calibrator : MonoBehaviour
    {
        public Parameters Parameters;

        public bool IsStereoCalibration => ObjectKind == TrackedObjectKind.Viewpoint;

        [Space] public TrackedObjectKind ObjectKind;
        public int ObjectIndex;

        [Space] public GameObject Pattern;
        public GameObject IdleAnimation;

        public Viewer Viewer;

        [Header("Command Menu")] public RemoteMenuButton StartButton;
        public RemoteMenuButton NextViewpointButton;
        public RemoteMenuButton FinishButton;
        public RemoteMenuDropdown TrackedObjectDropdown;
        public RemoteMenuLabel CalibrationStateLabel;
        public Text CalibrationStateText;

        // Intended Calibration Points
        private List<Vector3> _calibrationPositions;
        private int _calibrationPositionsIndex;
        private Camera.MonoOrStereoscopicEye _currentEye;

        // Recorded Calibration Information
        private List<Vector3> _leftOrMonoAnchorPositions;
        private List<Vector3> _rightAnchorPositions;
        private List<Quaternion> _leftOrMonoAnchorRotations;
        private List<Quaternion> _rightAnchorRotations;

        private readonly Vector3 _defaultViewerPosition = Vector3.up;

        private bool _isCalibrating;
        private bool _isAllDataCollected;

        public bool IsSolverRunning { get; private set; }

        public Vector3 CurrentCalibrationPosition => _isCalibrating
            ? _calibrationPositions[_calibrationPositionsIndex]
            : Vector3.up;

        public Camera.MonoOrStereoscopicEye NextEye => IsStereoCalibration
            ? (_currentEye.Equals(Camera.MonoOrStereoscopicEye.Left)
                ? Camera.MonoOrStereoscopicEye.Right
                : Camera.MonoOrStereoscopicEye.Left)
            : Camera.MonoOrStereoscopicEye.Mono;

        public List<Vector3> CurrentAnchorPositions => _currentEye.Equals(Camera.MonoOrStereoscopicEye.Right)
            ? _rightAnchorPositions
            : _leftOrMonoAnchorPositions;

        public List<Quaternion> CurrentAnchorRotations => _currentEye.Equals(Camera.MonoOrStereoscopicEye.Right)
            ? _rightAnchorRotations
            : _leftOrMonoAnchorRotations;

        #region MonoBehaviour

        void OnEnable()
        {
            // Ensure references are set
            Assert.IsNotNull(Pattern, $"{nameof(Pattern)} cannot be null. Please specify in the Editor.");

            // 
            Assert.IsNotNull(StartButton, $"{nameof(StartButton)} cannot be null. Please specify in the Editor.");
            Assert.IsNotNull(NextViewpointButton, $"{nameof(NextViewpointButton)} cannot be null. Please specify in the Editor.");
            Assert.IsNotNull(TrackedObjectDropdown, $"{nameof(TrackedObjectDropdown)} cannot be null. Please specify in the Editor.");
        }

        IEnumerator Start()
        {
            yield return null; // Wait a frame

            CalibrationStateLabel.enabled = false;
            SetupTrackedObjectDropdown();

            // Wait for the tracking system.
            yield return TrackingSystem.Instance.GetWaitForSubsystem();

            Viewer.enabled = false;

            // Bind connection events
            RemoteSystem.Instance.Connected += ViewerConnected;

            yield return null; // Wait a frame

            // Disable frame capure for all known remote viewers
            foreach (var connection in RemoteSystem.Instance.Connections)
            {
                var viewer = RemoteSystem.Instance.GetViewer(connection.Id);
                viewer.EnableFrameCapture = false;
            }
        }

        private void OnDestroy()
        {
            if (!RemoteSystem.ApplicationIsQuitting)
            {
                // Unind connection events
                RemoteSystem.Instance.Connected -= ViewerConnected;
            }
        }

        private void UpdateStatusText()
        {
            var trackingSystem = TrackingSystem.Instance;
            var trackingAnchor = trackingSystem.GetTrackingAnchor(ObjectKind, ObjectIndex);

            var position = trackingAnchor != null ? trackingSystem.GetTrackingPosition(trackingAnchor.position).ToString("F2") : string.Empty;
            var rotation = trackingAnchor != null ? trackingSystem.GetTrackingRotation(trackingAnchor.rotation).eulerAngles.ToString("F2") : string.Empty;

            CalibrationStateText.text = $"Position: {position}, Rotation: {rotation}. {CalibrationStateLabel.Text}";
        }

        void Update()
        {
            Pattern.SetActive(_isCalibrating); // Hide the pattern while not calibrating
            IdleAnimation.SetActive(IsSolverRunning); // Show the idle animation while the solver is running.

            // TODO: Replace input-specific code with an input abstraction layer
            if (_isCalibrating && UnityInput.GetButtonUp("Fire1")) // Xbox A
            {
                RecordCurrentAndGoToNext();
                if (_isAllDataCollected)
                {
                    FinishCalibration();
                }
            }
            else if (!_isCalibrating && UnityInput.GetKeyUp(KeyCode.Space))
            {
                StartCalibration();
            }

            // orient viewer and pattern
            var targetPatternPosition = VolumetricCamera.Instance.transform.position;
            var targetPatternRotation = Pattern.transform.rotation;

            if (Viewer != null)
            {
                Viewer.transform.position = _isCalibrating ? CurrentCalibrationPosition : _defaultViewerPosition;
                Viewer.transform.LookAt(VolumetricCamera.Instance.transform);

                if (_isCalibrating)
                {
                    // Orient pattern to look the calibration point
                    targetPatternPosition = VolumetricCamera.Instance.transform.position;
                    var direction = (Viewer.transform.position - Pattern.transform.position).normalized;
                    targetPatternRotation = Quaternion.LookRotation(direction, VolumetricCamera.Instance.transform.up);

                    UpdateStatusText();
                }
            }

            Pattern.transform.position =
                Vector3.Lerp(Pattern.transform.position, targetPatternPosition, Time.deltaTime * 20);
            Pattern.transform.rotation =
                Quaternion.Slerp(Pattern.transform.rotation, targetPatternRotation, Time.deltaTime * 20);
        }

        void OnDrawGizmos()
        {
            if (Application.isEditor && Application.isPlaying)
            {
                Gizmos.color = Color.green;
                var currentCalibrationPositionWorld =
                    DisplaySystem.Instance.PhysicalToWorld.MultiplyPoint3x4(CurrentCalibrationPosition);

                Gizmos.DrawLine(currentCalibrationPositionWorld - Vector3.up,
                    currentCalibrationPositionWorld + Vector3.up);
                Gizmos.DrawLine(currentCalibrationPositionWorld - Vector3.left,
                    currentCalibrationPositionWorld + Vector3.left);
                Gizmos.DrawLine(currentCalibrationPositionWorld - Vector3.forward,
                    currentCalibrationPositionWorld + Vector3.forward);
                Gizmos.DrawWireSphere(currentCalibrationPositionWorld, 0.1F);
            }
        }

        #endregion

        private void ViewerConnected(Viewer viewer, INetworkConnection connection)
        {
            // Disable image streaming for viewers in this scene
            Debug.Log($"Viewer: {connection.Id} connected.");
            viewer.EnableFrameCapture = false;
        }

        #region Object Kind Dropdown

        private void SetupTrackedObjectDropdown()
        {
            //
            var objectTypes = new List<string>();
            for (var i = 0; i < TrackedObject.MaximumTrackedObjects; i++)
            {
                var index = TrackedObject.GetTrackedObjectIndex(i);
                var kind = TrackedObject.GetTrackedObjectKind(i);
                objectTypes.Add($"{kind} {index}");
            }

            // Populate options
            TrackedObjectDropdown.Options = objectTypes;
        }

        public void OnObjectKindChange(int index, INetworkConnection conn)
        {
            if (index < 0)
            {
                Debug.LogWarning("Somehow chose a negative tracking object number.");
                return;
            }

            // Set our object type
            ObjectKind = TrackedObject.GetTrackedObjectKind(index);
            ObjectIndex = TrackedObject.GetTrackedObjectIndex(index);
        }

        public void UpdateStateLabelForIndex()
        {
            CalibrationStateLabel.Text = IsStereoCalibration
                ? $"Viewpoint {_calibrationPositionsIndex + 1} of {_calibrationPositions.Count} ( {_currentEye} )"
                : $"Viewpoint {_calibrationPositionsIndex + 1} of {_calibrationPositions.Count}";
        }

        #endregion

        /// <summary>
        /// Starts the calibration process.
        /// </summary>
        public void StartCalibration()
        {
            CalibrationStateLabel.enabled = true;
            Viewer.enabled = true;

            // Get it to ensure the system has a reference to it
            TrackingSystem.Instance.GetTrackingAnchor(ObjectKind, ObjectIndex);

            // Initialize data
            _leftOrMonoAnchorPositions = new List<Vector3>(Parameters.NumberOfSamples);
            _leftOrMonoAnchorRotations = new List<Quaternion>(Parameters.NumberOfSamples);
            _rightAnchorPositions = new List<Vector3>(Parameters.NumberOfSamples);
            _rightAnchorRotations = new List<Quaternion>(Parameters.NumberOfSamples);

            // Generate new positions
            _calibrationPositions = GenerateCalibrationPositions(Parameters.NumberOfSamples, Parameters.RandomSeed);

            // Setup the current eye
            _currentEye = IsStereoCalibration ? Camera.MonoOrStereoscopicEye.Left : Camera.MonoOrStereoscopicEye.Mono;

            // Setup the current calibration index
            _calibrationPositionsIndex = 0;

            // Setup calibration state
            _isAllDataCollected = false;
            _isCalibrating = true;
            IsSolverRunning = false;

            // 
            StartButton.enabled = false;
            TrackedObjectDropdown.enabled = false;
            NextViewpointButton.enabled = true;

            // Update label for progress (ie "0/8")
            UpdateStateLabelForIndex();
        }

        /// <summary>
        /// Records the position and rotation of the currently calibrating eye at the current calibration position
        /// </summary>
        public void RecordCurrentAndGoToNext()
        {
            // Get tracking system and desired anchor
            var trackingSystem = TrackingSystem.Instance;
            var trackingAnchor = trackingSystem.GetTrackingAnchor(ObjectKind, ObjectIndex);

            CurrentAnchorPositions.Add(trackingSystem.GetTrackingPosition(trackingAnchor.position));
            CurrentAnchorRotations.Add(trackingSystem.GetTrackingRotation(trackingAnchor.rotation));

            // Update current position index and eye if necessary
            if (IsStereoCalibration)
            {
                // Update index
                if (_currentEye.Equals(Camera.MonoOrStereoscopicEye.Right))
                {
                    _calibrationPositionsIndex++;
                }

                // Update eye
                _currentEye = NextEye;
            }
            else
            {
                // Update index
                _calibrationPositionsIndex++;
            }

            // Update label for progress (ie "2/8")
            UpdateStateLabelForIndex();

            // Still more data to collect
            if (_calibrationPositionsIndex < _calibrationPositions.Count)
            {
                return;
            }

            CalibrationStateLabel.Text = IsStereoCalibration
                ? $"Viewpoint {_calibrationPositionsIndex} ( {_currentEye} )"
                : $"Viewpoint {_calibrationPositionsIndex}";

            NextViewpointButton.enabled = false;
            FinishButton.enabled = true;

            // All data has been collected
            _calibrationPositionsIndex =
                _calibrationPositions.Count - 1; // so that no array out of bounds errors can happen
            _isAllDataCollected = true;
        }

        /// <summary>
        /// Finishes the calibration 
        /// </summary>
        public async void FinishCalibration()
        {
            CalibrationStateLabel.Text = "Computing";
            Viewer.enabled = false;

            // Solve the problem
            if (_isAllDataCollected)
            {
                IsSolverRunning = true;

                var leftOrMonoOptimizedCalibrations = await System.Threading.Tasks.Task.Run(() => Optimization.Optimize(_calibrationPositions, _leftOrMonoAnchorPositions,
                    _leftOrMonoAnchorRotations, Config.GeneralTracker.ScalingFactor));

                SetAndSaveCalibrations(ObjectKind, ObjectIndex, leftOrMonoOptimizedCalibrations.TrackingToDisplay, leftOrMonoOptimizedCalibrations.HeadToView);

                Debug.Log($"LeftOrMono optimization report: {leftOrMonoOptimizedCalibrations.Report}");

                if (IsStereoCalibration)
                {
                    // Call the stereo version of the solution
                    //MATLAB.RunCalibrationSolution(_calibrationPositions,
                    //    _leftOrMonoAnchorPositions,
                    //    _leftOrMonoAnchorRotations,
                    //    _rightAnchorPositions,
                    //    _rightAnchorRotations,
                    //    Config.GeneralTracker.ScalingFactor,
                    //    Config.GeneralTracker.Is6DoF,
                    //    MATLABSolverCompleted);

                    var rightOptimizedCalibrations = await System.Threading.Tasks.Task.Run(() => Optimization.Optimize(_calibrationPositions, _rightAnchorPositions,
                        _rightAnchorRotations, Config.GeneralTracker.ScalingFactor));

                    SetAndSaveCalibrations(ObjectKind, ObjectIndex, rightOptimizedCalibrations.TrackingToDisplay, leftOrMonoOptimizedCalibrations.HeadToView, rightOptimizedCalibrations.HeadToView);

                    Debug.Log($"Right optimization report: {rightOptimizedCalibrations.Report}");
                }
                else
                {
                    // Call the mono version of the solution
                    //MATLAB.RunCalibrationSolution(_calibrationPositions,
                    //    _leftOrMonoAnchorPositions,
                    //    _leftOrMonoAnchorRotations,
                    //    Config.GeneralTracker.ScalingFactor,
                    //    Config.GeneralTracker.Is6DoF,
                    //    SolverCompleted);
                }

                IsSolverRunning = false;

                // Update state label and set button states
                var calibrations = TrackingSystem.Instance.GetCalibrations(ObjectKind, ObjectIndex);
                CalibrationStateLabel.Text = $"Calibration Complete ( avg error: {calibrations.GetAverageCalibration().Error} )";
                NextViewpointButton.enabled = false;
                FinishButton.enabled = false;
                StartButton.enabled = true;
            }

            _isCalibrating = false;
        }

        /// <summary>
        /// Generates calibrations positions in physical display space.
        /// </summary>
        /// <param name="numberOfPositions">The number of positions to generate.</param>
        /// <param name="seed">The random state intialization seed. Used to generate the same positions given the same seed. (Default = 4)</param>
        /// <returns>A list of calibration positions in physical display space (local).</returns>
        public List<Vector3> GenerateCalibrationPositions(int numberOfPositions, int seed = 4)
        {
            var distanceRange = new Vector2
            {
                x = Parameters.DistanceRange.x,
                y = Parameters.DistanceRange.y
            };

            var polarAngleRange = new Vector2
            {
                x = Parameters.PolarAngleRange.x,
                y = Parameters.PolarAngleRange.y
            };

            return GenerateCalibrationPositions(numberOfPositions, distanceRange, polarAngleRange, seed);
        }

        public static List<Vector3> GenerateCalibrationPositions(Parameters parameters)
            => GenerateCalibrationPositions(parameters.NumberOfSamples, parameters.DistanceRange, parameters.PolarAngleRange, parameters.RandomSeed);

        /// <summary>
        /// Generates calibrations positions in physical display space.
        /// </summary>
        /// <param name="numberOfPositions">The number of positions to generate.</param>
        /// <param name="distanceRange">The distance range to generate the positions in as Vector2(min, max)</param>
        /// <param name="polarAngleRange">The polar angle range to generate the positions in as Vector2(min, max). These are radians measured from the vertical axis.</param>
        /// <param name="seed">The random state intialization seed. Used to generate the same positions given the same seed. (Default = 4)</param>
        /// <returns>A list of calibration positions in physical display space (local).</returns>
        public static List<Vector3> GenerateCalibrationPositions(int numberOfPositions, Vector2 distanceRange,
            Vector2 polarAngleRange, int seed = 4)
        {
            Random.InitState(seed);

            var positionsSpherical = new List<Vector3>(numberOfPositions);
            var samplesPerQuadrant = numberOfPositions / 4;
            for (var i = 0; i < numberOfPositions; i++)
            {
                // Generate restricted polar angles
                var phi = Random.Range(polarAngleRange.x, polarAngleRange.y);

                // Generate restricted distances
                var radius = Random.Range(distanceRange.x, distanceRange.y);

                // Generate azimuthal angles clustered around the corners
                // TODO: DF: should we remove quadrant clustering? That was designed for the cubee specifically
                // Maybe we should implement display specific calibration position generators?
                var quadrant = i / samplesPerQuadrant;
                var theta = (((i % samplesPerQuadrant) + 1.0f) / samplesPerQuadrant) * 0.4f + 0.4f +
                            (quadrant * Mathf.PI / 2);

                positionsSpherical.Add(new Vector3(radius, theta, phi));
            }

            // Sort the positions by the azimuthal coordinate so that the positions are in a clockwise order
            positionsSpherical.Sort((v1, v2) => v1.y.CompareTo(v2.y));

            // Convert each spherical to cartesian
            return positionsSpherical.Select(MathB.SphericalToCartesian).ToList();
        }

        private static void SetAndSaveCalibrations(TrackedObjectKind kind, int index, TrackingToDisplay.Calibration trackingToDisplay, HeadToView.Calibration leftOrMono, HeadToView.Calibration right = null)
        {
            Debug.Log("Solver Complete");

            // TODO: CC: Existing when using identity seems to be 0 even though its set to int.Max
            // if (TrackingSystem.Instance.TrackingToPhysical.Error > trackingToDisplay.Error)
            {
                // Debug.Log($"New tracking to physical calibration has less error ({trackingToDisplay.Error:G4} < {TrackingSystem.Instance.TrackingToPhysical.Error:G4}). Updating calibration files with new calibration.");
                // Set and save the tracking system calibration
                TrackingSystem.Instance.SetCalibration(trackingToDisplay);
                TrackingSystem.SaveCalibration(trackingToDisplay);
            }

            // Set and save the viewpoint calibrations
            var calibrations = right != null
                ? new HVCalibrations(leftOrMono, right)
                : new HVCalibrations(leftOrMono);

            TrackingSystem.Instance.SetCalibrations(kind, index, calibrations);
            TrackingSystem.SaveCalibrations(kind, index, calibrations);
        }

        ///// <summary>
        ///// Callback for when the solver has completed.
        ///// </summary>
        ///// <param name="sender">The solver object.</param>
        ///// <param name="e">The <see cref="MATLAB.CalibrationEventArgs"/> instance containing the event data.</param>
        //private void MATLABSolverCompleted(object sender, MATLAB.CalibrationEventArgs e)
        //{
        //    Debug.Log("MATLAB Solver Complete");

        //    SetAndSaveCalibrations(ObjectKind, ObjectIndex, e.TrackingToDisplay, e.LeftOrMonoHeadToView, IsStereoCalibration ? e.RightHeadToView : null);

        // Update state label and set button states
        //     CalibrationStateLabel.Text = $"Calibration Complete ( avg error: {calibrations.GetAverageCalibration().Error} )";
        //     NextViewpointButton.enabled = false;
        //     FinishButton.enabled = false;
        //     StartButton.enabled = true;
        // }
    }
}