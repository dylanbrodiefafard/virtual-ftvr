using UnityEngine;

public class ClusterManager : PointCloudManager
{
    public GameObject ClusterBoard;
    public ClusterPointCloud CurrentPointCloud => CurrentTrial.GetComponent<ClusterPointCloud>();

    private void WriteFields(int estimatedNumberOfClusters)
    {

        Writer.SetField("Estimated Clusters", estimatedNumberOfClusters);
        Writer.SetField("Actual Clusters", CurrentPointCloud.NumberOfClusters);
    }

    public void OnClustersEstimated(int numberOfClusters)
    {
        ClusterBoard.SetActive(false);
        Debug.Log($"{nameof(OnClustersEstimated)}({numberOfClusters})");
        // record data if doing the task
        if (!IsTraining)
        {
            WriteFields(numberOfClusters);
        }
        OnTrialCompleted();
    }

    protected override void InitializeCloud(GameObject cloud)
    {
        Wireframe.SetActive(true);

        foreach (var pointCollider in cloud.transform.GetComponentsInChildren<Collider>())
        {
            pointCollider.enabled = false;
        }
    }

    #region MonoBehaviour

    private void OnEnable() 
        => ClusterBoard.SetActive(true);

    private void OnDisable()
    {
        if (ClusterBoard != null)
        {
            ClusterBoard.SetActive(false);
        }
    }

    #endregion
}
