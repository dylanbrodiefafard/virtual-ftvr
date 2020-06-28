using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Biglab.Displays;
using Biglab.Displays.Virtual;
using Biglab.Extensions;
using Biglab.IO.Logging;
using Biglab.Utility;

using UnityEngine;
using UnityEngine.UI;

using Debug = UnityEngine.Debug;
using MethodLevel = Biglab.Displays.Virtual.OculusPresentationMethodController.PresentationMethod;

public class SpatialImpressionTask : MonoBehaviour
{
    public List<MethodLevel> Levels;
    public TableDescription SpatialImpressionTableDescription;
    public SelectorHandPairer SelectorPairer;
    public Text StatusText;
    public GameObject LevelButtons;
    public AudioClip AcceptClip;
    public AudioClip SuccessClip;
    public ViewingCondition LeftViewingCondition;
    public ViewingCondition RightViewingCondition;
    public GameObject ViewerViewingCondition;
    public GameObject ViewerViewingConditionContainer;

    private IEnumerable<IEnumerable<MethodLevel>> _levelPairs;

    private IEnumerable<MethodLevel> CurrentPair => _levelPairs.ElementAt(_pairIndex);

    private MethodLevel CurrentLevel => CurrentPair.ElementAt(_levelIndex);

    // Dependencies
    private VirtualSurface _surface;
    private OculusPresentationMethodController _methodController;
    private CsvTableWriter _writer;
    private Stopwatch _stopwatch;
    private Viewer _viewer => DisplaySystem.Instance.PrimaryViewer;

    // State
    private int _levelIndex;
    private int _pairIndex;
    private IEnumerator _inputLoop;
    private bool[] _visibilityConditionMet;
    private bool _isTraining;
    private bool _isTrainingConditionSelected;

    private void Awake()
    {
        if (SpatialImpressionTableDescription == null)
        {
            throw new ArgumentNullException(nameof(SpatialImpressionTableDescription));
        }

        if (StatusText == null)
        {
            throw new ArgumentNullException(nameof(StatusText));
        }

        if (LevelButtons == null)
        {
            throw new ArgumentNullException(nameof(LevelButtons));
        }

        if (ViewerViewingCondition == null)
        {
            throw new ArgumentNullException(nameof(ViewerViewingCondition));
        }

        if (LeftViewingCondition == null)
        {
            throw new ArgumentNullException(nameof(LeftViewingCondition));
        }

        if (RightViewingCondition == null)
        {
            throw new ArgumentNullException(nameof(RightViewingCondition));
        }

        if (AcceptClip == null)
        {
            throw new ArgumentNullException(nameof(AcceptClip));
        }

        _stopwatch = new Stopwatch();

        ResetVisibilityConditions();

        // Get all pairs
        _levelPairs = Levels.ToCombination(2);

        // Create writer
        _writer = gameObject.AddComponent<CsvTableWriter>();
        _writer.TableDescription = SpatialImpressionTableDescription;
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

        if (SelectorPairer == null)
        {
            throw new ArgumentNullException(nameof(SelectorPairer));
        }

        // Setup the selector on the preferred hand
        if (VirtualStudy.Config.PreferredHand == Camera.StereoscopicEye.Left)
        {
            SelectorPairer.PairWithLeftHand();
        }
        else
        {
            SelectorPairer.PairWithRightHand();
        }


        // Do some training
        yield return GetWaitTraining();

        _levelIndex = 0;

        yield return GetWaitToContinue(CurrentLevel, "Press any button to start");

        _stopwatch.Restart();

        _inputLoop = InputLoop();
        StartCoroutine(_inputLoop);
    }

    private void Update()
    {
        if (_viewer == null) { return; }

        var forward = Vector3.ProjectOnPlane(_viewer.transform.position - VolumetricCamera.Instance.transform.position, VolumetricCamera.Instance.transform.up).normalized;

        ViewerViewingConditionContainer.transform.rotation = Quaternion.LookRotation(forward, VolumetricCamera.Instance.transform.up);
    }

    private IEnumerator GetWaitTraining()
    {
        _isTraining = true;
        yield return WaitForAnyButton("Press any button to start training");

        do
        {
            _levelIndex = 0;

            yield return GetWaitSwitchLevels(Levels.RandomElement());

            StatusText.text = "This is condition 1. Move your view to turn both red posts green then press button 1.";

            yield return new WaitForEndOfFrame();

            yield return new WaitUntil(() => _visibilityConditionMet.All(element => element) && OVRInput.GetUp(OVRInput.Button.One));

            _levelIndex = 1;

            _isTrainingConditionSelected = false;

            yield return GetWaitSwitchLevels(Levels.RandomElement());

            StatusText.text = "This is condition 2. Move your view to turn both red posts green then select the better condition using the laser pointer.";

            // Wait until a condition is selected
            yield return new WaitUntil(() => _isTrainingConditionSelected);

            StatusText.text = "Press button 1 to continue training. Press button 2 to quit training and start tasks.";

            yield return new WaitForSeconds(0.25f);

            yield return new WaitUntil(() => OVRInput.GetUp(OVRInput.Button.One) || OVRInput.GetUp(OVRInput.Button.Two));

            _isTraining = OVRInput.GetUp(OVRInput.Button.One);

        } while (_isTraining);

    }

    private IEnumerator GetWaitToContinue(MethodLevel level, string statusText)
    {
        yield return WaitForAnyButton(statusText);

        yield return GetWaitSwitchLevels(level);
    }

    private IEnumerator WaitForAnyButton(string statusText)
    {
        yield return new WaitForSeconds(0.25f);

        StatusText.text = statusText;

        yield return OVRInputWait.Instance.AnyButtonUp;

        StatusText.text = string.Empty;
    }

    private IEnumerator GetWaitSwitchLevels(MethodLevel level)
    {
        yield return _surface.GetWaitFadeToBlack(0.5f);

        yield return new WaitForSeconds(0.25f);

        _methodController.ConfigureViewerForPresentationMethod(level);

        ResetVisibilityConditions();

        yield return new WaitForSeconds(0.25f);

        yield return _surface.GetWaitFadeToOriginal(0.5f);
    }

    private IEnumerator InputLoop()
    {
        while (enabled)
        {
            if (OVRInput.GetUp(OVRInput.Button.One))
            {
                if (_levelIndex == 0 && _visibilityConditionMet.All(element => element))
                {
                    _levelIndex++;
                    yield return GetWaitSwitchLevels(CurrentLevel);
                }
            }

            yield return new WaitForEndOfFrame();
        }
    }

    public IEnumerator RecordAndGotoNext(MethodLevel preferredLevel)
    {
        _stopwatch.Stop();

        StopCoroutine(_inputLoop);

        _writer.SetField("Level A", CurrentPair.ElementAt(0));
        _writer.SetField("Level B", CurrentPair.ElementAt(1));
        _writer.SetField("Preferred Level", preferredLevel);
        _writer.SetField("Time Taken (ms)", _stopwatch.ElapsedMilliseconds);

        _writer.Commit(); // Write the line

        if (_pairIndex + 1 == _levelPairs.Count())
        {
            Debug.Log("All level pairs are completed.");
            Destroy(_writer); // Force a write to the disk

            yield return GetWaitToContinue(MethodLevel.BinocularStereo, "Press any button to finish");

            Scheduler.StartCoroutine(VirtualStudy.Quit(5, StatusText));
            LevelButtons.SetActive(false);
            gameObject.SetActive(false);

            yield break;
        }

        _pairIndex++;

        _levelIndex = 0;

        yield return GetWaitToContinue(CurrentLevel, "Press any button to continue");

        StartCoroutine(_inputLoop);

        _stopwatch.Restart();
    }

    private void ResetVisibilityConditions()
    {
        _visibilityConditionMet = new[] { false, false };
        LeftViewingCondition.ResetColor();
        RightViewingCondition.ResetColor();
    }

    public void OnVisibilityConditionMet(int index)
    {
        // Already met, ignore it
        if (_visibilityConditionMet[index]) { return; }

        _visibilityConditionMet[index] = true;

        AudioSource.PlayClipAtPoint(SuccessClip, VolumetricCamera.Instance.transform.position);
        LevelButtons.SetActive(_levelIndex == 1 && _visibilityConditionMet.All(element => element));
    }

    public void OnConditionAccepted(int selected)
    {
        AudioSource.PlayClipAtPoint(AcceptClip, VolumetricCamera.Instance.transform.position);

        LevelButtons.SetActive(false);

        var selectedCondition = CurrentPair.ElementAt(selected);

        Debug.Log($"Accepted: {selectedCondition}");

        if (_isTraining)
        {
            _isTrainingConditionSelected = true;
        }
        else
        {
            StartCoroutine(RecordAndGotoNext(selectedCondition));
        }
    }
}
