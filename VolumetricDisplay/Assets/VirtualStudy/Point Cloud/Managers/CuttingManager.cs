using System.Linq;

using Biglab.Displays;
using Biglab.Utility.Controllers;

using UnityEngine;

public class CuttingManager : PointCloudManager
{
    public GameObject CuttingPlaneTool;
    public GameObject CuttingBoard;

    private CuttingPointCloud CurrentPointCloud => CurrentTrial.GetComponent<CuttingPointCloud>();

    private Plane _cuttingPlane
        => new Plane(
            DisplaySystem.Instance.WorldToPhysical.MultiplyVector(CuttingPlaneTool.transform.up),
            DisplaySystem.Instance.WorldToPhysical.MultiplyPoint(CuttingPlaneTool.transform.position));

    private SingleOculusTouchManipulator CuttingController
        => CuttingPlaneTool.GetComponent<SingleOculusTouchManipulator>();

    private void WriteFields(float sumDistanceToClusters)
        => Writer.SetField("Sum of Distances (cm)", sumDistanceToClusters * 100);

    public void OnCuttingPlanePlaced()
    {
        Debug.Log($"{nameof(OnCuttingPlanePlaced)}");

        // record data if doing the task
        if (!IsTraining)
        {
            var sum = CurrentPointCloud.GeometricMeans.Sum(mean 
                => Mathf.Abs(_cuttingPlane.GetDistanceToPoint(DisplaySystem.Instance.WorldToPhysical.MultiplyPoint3x4(mean))));

            WriteFields(sum);
        }

        OnTrialCompleted();
    }

    protected override void InitializeCloud(GameObject cloud)
    {
        Wireframe.SetActive(false);
        StartCoroutine(CuttingController.ResetOrientation(1));

        var cuttingPointCloud = cloud.GetComponent<CuttingPointCloud>();

        // Get all of the colliders in the clusters
        var firstClusterColliders = cuttingPointCloud.FirstCluster.GetComponentsInChildren<Collider>();
        var secondClusterColliders = cuttingPointCloud.SecondCluster.GetComponentsInChildren<Collider>();
        var thirdClusterColliders = cuttingPointCloud.ThirdCluster.GetComponentsInChildren<Collider>();
        var clusterColliders = firstClusterColliders.Concat(secondClusterColliders.Concat(thirdClusterColliders)).ToList();

        // Disable colliders for easier selection
        foreach (var pointCollider in cloud.transform.GetComponentsInChildren<Collider>())
        {
            // Skip disabling the collider if it's part of a cluster
            if (clusterColliders.Contains(pointCollider))
            {
                continue;
            }

            pointCollider.enabled = false;
        }
    }

    #region MonoBehaviour

    private void OnEnable()
    {
        CuttingPlaneTool.SetActive(true);
        CuttingBoard.SetActive(true);
    }

    private void OnDisable()
    {
        if (CuttingPlaneTool != null)
        {
            CuttingPlaneTool.SetActive(false);
        }

        if (CuttingBoard != null)
        {
            CuttingBoard.SetActive(false);
        }
    }

    #endregion

}
