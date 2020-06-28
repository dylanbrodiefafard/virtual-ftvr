using System.Collections.Generic;
using UnityEngine;

public class ClusterPointCloud : PointCloud {

    public int NumberOfClusters;
    public int NumberOfPointsPerCluster;
    public float StdDevInCluster;

    public override void GeneratePointCloud()
    {
        Points = new List<GameObject>();

        PopulateRegularGrid();

        PopulateClusters();
    }

    private void PopulateClusters()
    {
        for (var i = 0; i < NumberOfClusters; i++)
        {
            var cluster = new GameObject($"Cluster {i + 1}")
            {
                isStatic = true
            };
            cluster.transform.parent = transform;
            cluster.transform.localPosition = GetRandomPositionOnRegularGrid();
            cluster.transform.localScale = PointLocalScale;
            PopulateCluster(cluster.transform);
        }
    }

    private void PopulateCluster(Transform cluster)
    {
        for (var j = 0; j < NumberOfPointsPerCluster; j++)
        {
            var pointInCluster = Instantiate(PointPrefab, cluster);
            pointInCluster.transform.localPosition = GetNormallyDistributedPosition(StdDevInCluster);
        }
    }
}
