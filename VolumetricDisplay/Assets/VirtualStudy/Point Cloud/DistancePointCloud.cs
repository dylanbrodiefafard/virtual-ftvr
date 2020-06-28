using Biglab.Extensions;
using System;
using System.Collections.Generic;
using Biglab.Math;
using UnityEngine;

public class DistancePointCloud : PointCloud
{
    public float PairDistanceRatio;
    public Color FirstPairColor;
    public Color SecondPairColor;

    [SerializeField]
    private PointPair _firstPair;
    [SerializeField]
    private PointPair _secondPair;

    public float FirstPairDistance => _firstPair.Distance;
    public GameObject FirstPairPointA => _firstPair.PointA;
    public GameObject FirstPairPointB => _firstPair.PointB;

    public float SecondPairDistance => _secondPair.Distance;
    public GameObject SecondPairPointA => _secondPair.PointA;
    public GameObject SecondPairPointB => _secondPair.PointB;

    public string CorrectPair => _firstPair.Distance < _secondPair.Distance ? "First Pair" : "Second Pair";

    [Serializable]
    private struct PointPair
    {
        public GameObject PointA;
        public GameObject PointB;
        public float Distance;
    }

    #region MonoBehaviour

    private void Start()
    {
        SetPairColors(_firstPair, FirstPairColor);
        SetPairColors(_secondPair, SecondPairColor);
    }

    #endregion

    public override void GeneratePointCloud()
    {
        Points = new List<GameObject>();

        PopulateRegularGrid();

        // Pick two pairs of points such that the distance between second pair is 20% that of the first pair
        const float minmumLocalDistance = 0.3f;
        PickPairs(minmumLocalDistance, PairDistanceRatio);
        SetPairColors(_firstPair, FirstPairColor);
        SetPairColors(_secondPair, SecondPairColor);
        PutPairsInContainer(new GameObject("Pair A Points"), _firstPair);
        PutPairsInContainer(new GameObject("Pair B Points"), _secondPair);
    }

    private void PutPairsInContainer(GameObject container, PointPair pair)
    {
        container.transform.parent = transform;
        pair.PointA.transform.parent = container.transform;
        pair.PointB.transform.parent = container.transform;
    }

    private static void SetPairColors(PointPair pair, Color color)
    {
        pair.PointA.GetOrAddComponent<MaterialPropertyBlockSetter>().Block.SetColor(MaterialPropertyBlockSetter.MainColor, color);
        pair.PointB.GetOrAddComponent<MaterialPropertyBlockSetter>().Block.SetColor(MaterialPropertyBlockSetter.MainColor, color);
    }

    public void PickPairs(float minimumDistance, float secondPairDistanceTarget)
    {
        if (Points.IsNull() || Points.Count < 4)
        {
            throw new InvalidOperationException("There aren\'t enough points generated to pick point pairs.");
        }

        var numberOfAttempts = 0;
        const int maximumNumberOfAttempts = 10000;

        // Pick first pair
        while (numberOfAttempts++ < maximumNumberOfAttempts)
        {
            var a = Points.RandomElement();
            var b = Points.RandomElement();

            var distance = Vector3.Distance(a.transform.localPosition, b.transform.localPosition);

            if (distance < minimumDistance)
            {
                continue;
            }

            _firstPair = new PointPair
            {
                PointA = a,
                PointB = b,
                Distance = distance
            };

            break;
        }

        const float distanceTolerance = 0.005f;

        // Pick second pair
        while (numberOfAttempts++ < maximumNumberOfAttempts)
        {
            var a = Points.RandomElement();
            var b = Points.RandomElement();

            if (a.Equals(_firstPair.PointA) || a.Equals(_firstPair.PointB))
            {
                continue;
            }

            if (b.Equals(_firstPair.PointA) || b.Equals(_firstPair.PointB))
            {
                continue;
            }

            var distance = Vector3.Distance(a.transform.localPosition, b.transform.localPosition);

            if (Mathf.Abs(distance / _firstPair.Distance - secondPairDistanceTarget) > distanceTolerance)
            {
                continue;
            }

            _secondPair = new PointPair
            {
                PointA = a,
                PointB = b,
                Distance = distance
            };

            break;
        }

        // Swap the pairs on a coin flip, so that the first pair isn't always the longest
        if (RandomUtilities.IsTails)
        {
            var tmp = _firstPair;
            _firstPair = _secondPair;
            _secondPair = tmp;
        }

        if (numberOfAttempts > maximumNumberOfAttempts)
        {
            throw new InvalidOperationException($"Unable to find valid pairs after {numberOfAttempts} attempts.");
        }
    }
}
