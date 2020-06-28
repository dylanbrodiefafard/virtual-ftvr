using Biglab.Math;
using System.Collections.Generic;
using System.Linq;
using Biglab.Extensions;
using UnityEngine;

public class CuttingPointCloud : PointCloud
{
    public int NumberOfPointsPerCluster;
    public float StdDevInCluster;
    public float ClusterDistance;
    public Color ClusterColor;
    public Color ClusterTouchingColor;

    public Vector3[] GeometricMeans => new[]
    {
        GeometricMeanOfCluster(_firstCluster),
        GeometricMeanOfCluster(_secondCluster),
        GeometricMeanOfCluster(_thirdCluster)
    };

    public GameObject FirstCluster => _firstCluster;
    public GameObject SecondCluster => _secondCluster;
    public GameObject ThirdCluster => _thirdCluster;

    [SerializeField]
    private GameObject _firstCluster;
    [SerializeField]
    private GameObject _secondCluster;
    [SerializeField]
    private GameObject _thirdCluster;

    public override void GeneratePointCloud()
    {
        Points = new List<GameObject>();

        PopulateRegularGrid();

        PopulateClusters();
    }

    private static Vector3 GeometricMeanOfCluster(GameObject cluster)
    {
        var children = cluster.GetComponentsInChildren<Transform>();
        var positions = children.Select(child => child.position);

        return children.Length == 0 ? cluster.transform.position : new Vector3
        {
            x = positions.Average(position => position.x),
            y = positions.Average(position => position.y),
            z = positions.Average(position => position.z)
        };
    }

    private void PopulateClusters()
    {
        // Places the clusters at the corner points of an equilateral triangle arbitrarily rotated and translated

        // Place the first cluster in a random position
        _firstCluster = new GameObject("Cluster 1");
        _firstCluster.transform.parent = transform;
        _firstCluster.transform.localScale = PointLocalScale;
        _firstCluster.transform.localPosition = GetRandomPositionOnRegularGrid();

        // Place the second and third clusters exactly localDistance away from the first cluster (in a random direction)
        // This follows the equations of an equilateral triangle
        var localDistance = ClusterDistance / RegularGridSize;
        var equilaterialAltitude = Mathf.Sqrt(3) / 2 * localDistance;
        var medianOfSecondAndThird = GetRandomLocalPositionDistanceAway(_firstCluster.transform.localPosition, equilaterialAltitude);
        var directionToMedian = (medianOfSecondAndThird - _firstCluster.transform.localPosition).normalized;
        var directionPerpendicularToMedian = MathB.GetArbitraryOrthogonalDirection(directionToMedian);

        _secondCluster = new GameObject("Cluster 2");
        _secondCluster.transform.parent = transform;
        _secondCluster.transform.localScale = PointLocalScale;
        _secondCluster.transform.localPosition = medianOfSecondAndThird + directionPerpendicularToMedian * localDistance / 2;

        _thirdCluster = new GameObject("Cluster 3");
        _thirdCluster.transform.parent = transform;
        _thirdCluster.transform.localScale = PointLocalScale;
        _thirdCluster.transform.localPosition = medianOfSecondAndThird - directionPerpendicularToMedian * localDistance / 2;

        PopulateCluster(_firstCluster.transform);
        PopulateCluster(_secondCluster.transform);
        PopulateCluster(_thirdCluster.transform);
    }

    private void PopulateCluster(Transform cluster)
    {
        for (var j = 0; j < NumberOfPointsPerCluster; j++)
        {
            var pointInCluster = Instantiate(PointPrefab, cluster);

            pointInCluster.transform.localPosition = GetNormallyDistributedPosition(StdDevInCluster);

            pointInCluster.AddComponentWithInit<CollidablePoint>(script =>
            {
                script.OriginalColor = ClusterColor;
                script.HighlightColor = ClusterTouchingColor;
            });
        }
    }
}
