using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

public class SelectionManager : PointCloudManager
{
    /// <summary>
    /// Gets the point cloud component of the current trial
    /// </summary>
    public SelectionPointCloud CurrentPointCloud => CurrentTrial.GetComponent<SelectionPointCloud>();

    public RaycastSelector Selector;

    private UnityAction _negativeSelectionListener => () => OnNegativeSelection(Selector.CurrentRay);
    private UnityAction _positiveSelectionListener => () => OnPositiveSelection(Selector.CurrentHit, Selector.CurrentRay);

    // State
    private List<GameObject> _selectablePoints;
    private int _numberOfSuccessfulSelections;
    private int _numberOfFailedSelections;
    private float _failedAngles;

    private void WriteFields(float averageAngularMiss, float errorRate)
    {
        Writer.SetField("Average Angular Miss (degrees)", averageAngularMiss);
        Writer.SetField("Error (%)", errorRate * 100);
    }

    public void OnPositiveSelection(RaycastHit hit, Ray selectionRay)
    {
        Debug.Log($"Positive selection on {hit.transform.name}.");

        if (!_selectablePoints.Contains(hit.transform.gameObject))
        {
            OnNegativeSelection(selectionRay);
        }
    }

    public void OnNegativeSelection(Ray selectionRay)
    {
        Debug.Log($"Negative selection with ray: {selectionRay}.");

        AudioSource.PlayClipAtPoint(FailureClip, selectionRay.origin);

        var closestAngle = float.MaxValue;
        foreach (var point in _selectablePoints)
        {
            var pointDirection = (point.transform.position - selectionRay.origin).normalized;
            var pointAngle = Vector3.Angle(selectionRay.direction, pointDirection);
            closestAngle = Mathf.Min(closestAngle, pointAngle);
        }

        _failedAngles += closestAngle;
        _numberOfFailedSelections++;
    }

    public void OnPointSelected(GameObject point)
    {
        Debug.Log($"Successful selection on selectable {point.name}.");

        _selectablePoints.Remove(point);
        _numberOfSuccessfulSelections++;

        if (_selectablePoints.Count > 0)
        {
            return;
        }

        if (!IsTraining)
        {
            var meanAngularMiss = _numberOfFailedSelections == 0 ? 0 : _failedAngles / _numberOfFailedSelections;
            var errorRate = (float)_numberOfFailedSelections /
                            (_numberOfFailedSelections + _numberOfSuccessfulSelections);

            WriteFields(meanAngularMiss, errorRate);
        }

        Selector.OnNegativeSelection.RemoveListener(_negativeSelectionListener);
        Selector.OnPositiveSelection.RemoveListener(_positiveSelectionListener);

        OnTrialCompleted();
    }

    protected override void InitializeCloud(GameObject cloud)
    {

        Wireframe.SetActive(true);

        if (Selector == null)
        {
            throw new ArgumentNullException(nameof(Selector));
        }

        Selector.OnNegativeSelection.AddListener(_negativeSelectionListener);
        Selector.OnPositiveSelection.AddListener(_positiveSelectionListener);

        var cloudScript = cloud.GetComponent<SelectionPointCloud>();
        if (cloudScript == null) { throw new ArgumentNullException(nameof(cloudScript)); }

        // Setup state
        _selectablePoints = new List<GameObject>(cloudScript.SelectablePoints);
        _numberOfFailedSelections = 0;
        _numberOfSuccessfulSelections = 0;
        _failedAngles = 0;

        // hook up events
        foreach (var point in _selectablePoints)
        {
            var selectable = point.AddComponent<Selectable>();
            selectable.OnSelectionEvent.AddListener(() => OnPointSelected(point));
            selectable.OnSelectionSound = SuccessClip;
        }
    }
}
