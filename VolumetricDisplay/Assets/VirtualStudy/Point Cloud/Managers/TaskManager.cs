using System;
using System.Collections;

using Biglab.Displays;
using Biglab.Displays.Virtual;
using Biglab.Utility;

using UnityEngine;
using UnityEngine.UI;

using MethodLevel = Biglab.Displays.Virtual.OculusPresentationMethodController.PresentationMethod;

public class TaskManager : MonoBehaviour
{
    // Presentation Levels
    public MethodLevel[] Levels;

    // Managers
    public PointCloudManager[] Managers;

    // Refs
    public SelectorHandPairer SelectorPairer;
    public Text StatusText;

    // Objects to control
    private OculusPresentationMethodController _methodController;
    private VirtualSurface _surface;

    // State
    private int _managerIndex;
    private int _levelIndex;
    private PointCloudManager CurrentManager => Managers[_managerIndex];
    private MethodLevel CurrentLevel => Levels[_levelIndex];

    #region MonoBehaviour

    private IEnumerator Start()
    {
        yield return DisplaySystem.Instance.GetWaitForPrimaryViewer();
        yield return new WaitForEndOfFrame();

        _methodController = FindObjectOfType<OculusPresentationMethodController>();

        if (_methodController == null) { throw new ArgumentNullException(nameof(_methodController)); }

        _surface = FindObjectOfType<VirtualSurface>();

        if (_surface == null) { throw new ArgumentNullException(nameof(_surface)); }

        if (Managers.Length < 1) { throw new InvalidOperationException($"{nameof(Managers)} must have at least 1 element."); }

        if (Levels.Length < 1) { throw new InvalidOperationException($"{nameof(Levels)} must have at least 1 element."); }

        if (SelectorPairer == null) { throw new ArgumentNullException(nameof(SelectorPairer)); }

        if (StatusText == null) { throw new ArgumentNullException(nameof(StatusText)); }

        StatusText.text = string.Empty;

        // Setup the selector on the preferred hand
        if (VirtualStudy.Config.PreferredHand.Equals(StereoTargetEyeMask.Left)) { SelectorPairer.PairWithLeftHand(); }
        else { SelectorPairer.PairWithRightHand(); }

        yield return GetWaitSwitchManagers("Press any button to start training");

        // Start in training mode
        CurrentManager.StartTraining();
    }

    public void OnTrainingCompleted()
    {
        Debug.Log($"{nameof(OnTrainingCompleted)}");

        StartCoroutine(WaitOnTrainingCompleted());
    }

    public IEnumerator WaitOnTrainingCompleted()
    {
        yield return GetWaitSwitchManagers("Press any button to start tasks");

        // When done with training, start tasks for real.
        CurrentManager.StartTasks(CurrentLevel);
    }

    public IEnumerator OnTasksCompleted()
    {
        Debug.Log($"{nameof(OnTasksCompleted)}");

        var areManagersDone = _managerIndex + 1 == Managers.Length;
        var areLevelsDone = _levelIndex + 1 == Levels.Length;

        if (areManagersDone && areLevelsDone)
        {
            Debug.Log("All Managers and Levels are done.");
            gameObject.SetActive(false);
            Scheduler.StartCoroutine(VirtualStudy.Quit(5, StatusText));
            yield break;
        }

        if (areLevelsDone)
        {
            // Go to next manager and reset conditions
            _levelIndex = 0;
            _managerIndex++;
        }
        else
        {
            _levelIndex++;
        }

        yield return GetWaitSwitchManagers("Press any button to start training");

        CurrentManager.StartTraining();
    }

    private IEnumerator GetWaitSwitchManagers(string text)
    {
        DisableManagers();

        yield return new WaitForSeconds(0.25f);

        yield return WaitForAnyButton(text);

        yield return GetWaitSwitchLevels();

        SetManagersActive();
    }

    private IEnumerator GetWaitSwitchLevels()
    {
        yield return _surface.GetWaitFadeToBlack(0.5f);

        yield return new WaitForSeconds(0.25f);

        _methodController.ConfigureViewerForPresentationMethod(CurrentLevel);

        yield return new WaitForSeconds(0.25f);

        yield return _surface.GetWaitFadeToOriginal(0.5f);
    }

    private IEnumerator WaitForAnyButton(string statusText)
    {
        StatusText.text = statusText;
        yield return OVRInputWait.Instance.AnyButtonUp;
        StatusText.text = string.Empty;
    }

    private void OnDisable()
    {
        if (DisplaySystem.Instance == null || !DisplaySystem.Instance.HasPrimaryViewer || _methodController == null) { return; }

        _methodController?.ConfigureViewerForPresentationMethod(MethodLevel.BinocularStereo);
    }

    #endregion

    private void DisableManagers()
    {
        foreach (var manager in Managers) { manager.gameObject.SetActive(false); }
    }

    private void SetManagersActive()
    {
        foreach (var manager in Managers) { manager.gameObject.SetActive(manager == CurrentManager); }
    }
}
