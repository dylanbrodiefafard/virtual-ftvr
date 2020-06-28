using System;
using Biglab.Extensions;
using System.Collections.Generic;
using UnityEngine;

public class SelectionPointCloud : PointCloud
{
    public Color SelectableColor;
    public Color SelectedColor;
    public int NumberOfTargets;

    public List<GameObject> SelectablePoints => _selectablePoints;

    [SerializeField]
    private List<GameObject> _selectablePoints;

    public override void GeneratePointCloud()
    {
        Points = new List<GameObject>();

        PopulateRegularGrid();

        _selectablePoints = new List<GameObject>();
        PickPoints();
        MakePointsSelectable();
    }

    public void MakePointsSelectable()
    {
        foreach (var point in _selectablePoints)
        {
            point.AddComponentWithInit<SelectablePoint>(script =>
            {
                script.OriginalColor = SelectableColor;
                script.HighlightColor = SelectedColor;
            });
        }
    }

    public void PickPoints()
    {
        if (Points.Count < NumberOfTargets)
        {
            throw new InvalidOperationException($"Number of targets ({NumberOfTargets}) is greater than number of points ({Points.Count}).");
        }

        var targetContainer = new GameObject("Targets");
        targetContainer.transform.parent = transform;
        for (var i = 0; i < NumberOfTargets; i++)
        {
            var numberOfAttempts = 0;
            const int maximumNumberOfAttempts = 10000;
            GameObject target;
            do
            {
                target = Points.RandomElement();
            } while (numberOfAttempts++ < maximumNumberOfAttempts && _selectablePoints.Contains(target));

            if (numberOfAttempts > maximumNumberOfAttempts)
            {
                throw new InvalidOperationException($"Unable to find a target after ({numberOfAttempts}) attempts.");
            }

            target.transform.parent = targetContainer.transform;
            _selectablePoints.Add(target);
        }
    }

}
