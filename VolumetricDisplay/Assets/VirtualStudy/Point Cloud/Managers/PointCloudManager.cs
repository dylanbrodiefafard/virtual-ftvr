using System.Diagnostics;

using Biglab.Extensions;
using Biglab.IO.Logging;

using UnityEngine;

using Debug = UnityEngine.Debug;
using MethodLevel = Biglab.Displays.Virtual.OculusPresentationMethodController.PresentationMethod;

public abstract class PointCloudManager : MonoBehaviour
{
    public AudioClip SuccessClip;
    public AudioClip FailureClip;

    public ParticleSystem[] Emitters;
    // 
    public Transform SpawnTransform;

    // Distance
    public GameObject[] TrainingTrials;

    public GameObject[] TaskTrials;

    // state variables
    protected bool IsTraining { get; private set; }
    public bool IsCompleted { get; protected set; }
    private int _trainingIndex;
    private int _taskIndex;
    protected GameObject CurrentTrial;
    private Stopwatch _stopwatch;

    // Logging
    public TableDescription TableDescription;
    protected CsvTableWriter Writer;

    protected GameObject Wireframe { get; private set; }

    protected abstract void InitializeCloud(GameObject cloud);

    private GameObject InstatiateCloud(GameObject cloud, bool isTimed = false)
    {
        // Instatiate it in the environment
        var go = Instantiate(cloud);
        go.transform.position = SpawnTransform.transform.position;

        var wireframe = go.transform.FindDeepChild("Wireframe");
        if (wireframe != null) { Wireframe = wireframe.gameObject; }

        InitializeCloud(go);
        StaticBatchingUtility.Combine(go);

        if (isTimed) { _stopwatch.Restart(); }

        return go;
    }

    #region MonoBehaviour

    protected virtual void Awake()
        => _stopwatch = new Stopwatch();

    #endregion

    public void StartTraining()
    {
        Debug.Log($"{nameof(StartTraining)}");

        if (TrainingTrials != null && TrainingTrials.Length > 0)
        {
            // Start training
            IsTraining = true;
            _trainingIndex = 0;
            CurrentTrial = InstatiateCloud(TrainingTrials[_trainingIndex]);
        }
        else
        {
            Debug.LogWarning("No training was specific.");
            SendMessageUpwards("OnTrainingCompleted", SendMessageOptions.DontRequireReceiver);
        }
    }

    public void StartTasks(MethodLevel level)
    {
        Debug.Log($"{nameof(StartTasks)}");

        if (TaskTrials != null && TaskTrials.Length > 0)
        {
            if (Writer != null) { Destroy(Writer); }

            // Setup writer
            Writer = gameObject.AddComponent<CsvTableWriter>();
            Writer.TableDescription = TableDescription;
            Writer.EnableDatePrefix = false;
            Writer.FilePath = VirtualStudy.Config.GetDataFilepath($"{gameObject.name} - {level}");

            _taskIndex = 0;
            CurrentTrial = InstatiateCloud(TaskTrials[_taskIndex], true);
        }
        else
        {
            Debug.LogWarning("No tasks were specific.");
            SendMessageUpwards("OnTasksCompleted", SendMessageOptions.DontRequireReceiver);
        }
    }

    public void OnTrialCompleted()
    {
        Debug.Log($"{nameof(OnTrialCompleted)}");

        FireEmitters();

        if (IsTraining)
        {
            if (CurrentTrial != null) { Destroy(CurrentTrial); }

            if (_trainingIndex + 1 < TrainingTrials.Length)
            {
                CurrentTrial = InstatiateCloud(TrainingTrials[++_trainingIndex], true);
                return;
            }

            IsTraining = false;
            SendMessageUpwards("OnTrainingCompleted", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            _stopwatch.Stop();
            Writer.SetField("Time Taken (ms)", _stopwatch.ElapsedMilliseconds);
            Writer.SetField("Point Cloud", CurrentTrial.name);
            Writer.Commit(); // Write the line

            if (CurrentTrial != null) { Destroy(CurrentTrial); }

            if (_taskIndex + 1 < TaskTrials.Length)
            {
                CurrentTrial = InstatiateCloud(TaskTrials[++_taskIndex], true);
                return;
            }

            // Tasks are completed!
            Destroy(Writer); // Force a write to the disk

            SendMessageUpwards("OnTasksCompleted", SendMessageOptions.DontRequireReceiver);
        }
    }

    public void FireEmitters()
    {
        foreach (var emitter in Emitters)
        {
            emitter.Play();
        }
    }

}
