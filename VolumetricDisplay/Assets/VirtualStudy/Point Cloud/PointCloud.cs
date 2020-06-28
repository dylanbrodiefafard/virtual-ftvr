using System;
using System.Collections.Generic;
using Biglab.Extensions;
using UnityEngine;
using Biglab.Math;
using Random = UnityEngine.Random;

public abstract class PointCloud : MonoBehaviour, IPointCloudGenerator
{
    public const float PointSize = 0.5f;
    public int RegularGridSize;
    public float RegularPointDensity;
    public GameObject PointPrefab;

    protected Vector3 PointLocalScale => Vector3.one * (PointSize / (RegularGridSize));
    protected List<GameObject> Points;

    public abstract void GeneratePointCloud();

    protected Vector3 RegularGridIndexToLocalPosition(int x, int y, int z)
    {
        // Transforms from 0 - (RegularGridSize - 1) to - 0.5 to 0.5
        var offset = (0.5f / RegularGridSize);
        const float fromMin = 0;
        var fromMax = RegularGridSize - 1;
        var toMin = -0.5f + offset;
        var toMax = 0.5f - offset;
        var position = new Vector3(x, y, z);
        position.x = position.x.Rescale(fromMin, fromMax, toMin, toMax);
        position.y = position.y.Rescale(fromMin, fromMax, toMin, toMax);
        position.z = position.z.Rescale(fromMin, fromMax, toMin, toMax);
        return position;
    }

    protected Vector3 GetRandomLocalPositionDistanceAway(Vector3 position, float distance)
    {
        var numberOfAttempts = 0;
        const int maximumNumberOfAttemps = 10000;
        do
        {
            var direction = Random.onUnitSphere;
            var candidatePosition = position + direction * distance;
            if (candidatePosition.x > 0.5f || candidatePosition.x < -0.5f)
            {
                continue;
            }

            if (candidatePosition.y > 0.5f || candidatePosition.y < -0.5f)
            {
                continue;
            }

            if (candidatePosition.z > 0.5f || candidatePosition.z < -0.5f)
            {
                continue;
            }

            return candidatePosition;
        } while (numberOfAttempts++ < maximumNumberOfAttemps);

        throw new ArgumentException($"Failed to find a position {distance} away from {position} after {numberOfAttempts} attempts.");
    }

    protected Vector3 GetRandomPositionOnRegularGrid()
        => RegularGridIndexToLocalPosition(
            Random.Range(0, RegularGridSize),
            Random.Range(0, RegularGridSize),
            Random.Range(0, RegularGridSize));

    protected Vector3 GetNormallyDistributedPosition(float stdev)
        => new Vector3
        {
            x = RandomUtilities.RandomNormalDistribution(0, stdev),
            y = RandomUtilities.RandomNormalDistribution(0, stdev),
            z = RandomUtilities.RandomNormalDistribution(0, stdev)
        };

    protected void PopulateRegularGrid()
    {
        var regularGrid = new GameObject("Regular Grid Points")
        {
            isStatic = true
        };
        regularGrid.transform.parent = transform;
        // Generate the random positions
        for (var x = 0; x < RegularGridSize; x++)
        {
            for (var y = 0; y < RegularGridSize; y++)
            {
                for (var z = 0; z < RegularGridSize; z++)
                {
                    if (Random.value > RegularPointDensity)
                    {
                        continue; // Fail
                    }
                    // Success
                    var localPosition = RegularGridIndexToLocalPosition(x, y, z);
                    var go = Instantiate(PointPrefab, regularGrid.transform);
                    go.isStatic = true;
                    go.transform.localPosition = localPosition;
                    go.transform.localScale = PointLocalScale;
                    Points.Add(go);
                }
            }
        }
    }

}
