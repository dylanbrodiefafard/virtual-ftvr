using UnityEngine;

public class DistanceManager : PointCloudManager
{
    public GameObject DistanceBoard;

    /// <summary>
    /// Gets the point cloud component of the current trial
    /// </summary>
    public DistancePointCloud CurrentPointCloud => CurrentTrial.GetComponent<DistancePointCloud>();

    private void WriteFields(string selectedPair)
    {
        Writer.SetField("Selected Pair", selectedPair);
        Writer.SetField("First Pair Distance", CurrentPointCloud.FirstPairDistance);
        Writer.SetField("Second Pair Distance", CurrentPointCloud.SecondPairDistance);
        Writer.SetField("Correct Pair", CurrentPointCloud.CorrectPair);
    }

    /// <summary>
    /// Event handler for when the first pair is chosen as the closest pair.
    /// </summary>
    public void OnPair1Selected()
    {
        Debug.Log($"{nameof(OnPair1Selected)}");

        // record data if doing the task
        if (!IsTraining)
        {
            WriteFields("First Pair");
        }
        OnTrialCompleted();
    }

    /// <summary>
    /// Event handler for when the second pair is chosen as the closest pair.
    /// </summary>
    public void OnPair2Selected()
    {
        Debug.Log($"{nameof(OnPair2Selected)}");

        // record data if doing the task
        if (!IsTraining)
        {
            WriteFields("Second Pair");
        }
        OnTrialCompleted();
    }

    /// <inheritdoc />
    protected override void InitializeCloud(GameObject cloud)
    {
        Wireframe.SetActive(false);

        foreach (var pointCollider in cloud.transform.GetComponentsInChildren<Collider>())
        {
            pointCollider.enabled = false;
        }
    }

    #region MonoBehaviour

    private void OnEnable() 
        => DistanceBoard.SetActive(true);

    private void OnDisable()
    {
        if (DistanceBoard != null)
        {
            DistanceBoard.SetActive(false);
        }
    }

    #endregion
}
