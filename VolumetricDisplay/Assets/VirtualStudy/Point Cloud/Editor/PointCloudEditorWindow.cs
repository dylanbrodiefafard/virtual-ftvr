using System;
using Biglab.Extensions;
using Biglab.Math;
using UnityEditor;
using UnityEngine;

public class PointCloudEditorWindow : EditorWindow
{
    // General settings
    private PointCloudType _pointCloudType;
    private float _pointDensity = 0.5f;
    private GameObject _point;
    private int _gridSize = 10;
    // Distance settings
    private float _distanceRatio = 0.5f;
    private Color _distanceFirstPairColor;
    private Color _distanceSecondPairColor;
    // Selection settings
    private int _selectionNumberOfTargets = 4;
    private Color _selectionSelectableColor;
    private Color _selectionSelectedColor;
    // Cluster settings
    private int _clusterNumberOfClusters = 3;
    private int _clusterPointsPerCluster = 30;
    private float _clusterStdDevInCluster = 2;
    // Cutting settings
    private int _cuttingPointsPerCluster = 10;
    private float _cuttingClusterDistance = 10; // grid units
    private float _cuttingStdDevInCluster = 0.5f;
    private Color _cuttingClusterColor;
    private Color _cuttingClusterTouchingColor;

    public static GameObject Wireframe => Resources.Load<GameObject>("Wireframe");

    // Add menu named "My Window" to the Window menu
    [MenuItem("Biglab/Point Cloud Generator")]
    public static void Init()
    {
        // Get existing open window or if none, make a new one:
        var window = (PointCloudEditorWindow)GetWindow(typeof(PointCloudEditorWindow));
        window.Show();
    }

    public virtual void OnGUI()
    {
        EditorGUILayout.LabelField("Step 1. Type", EditorStyles.boldLabel);

        _pointCloudType = (PointCloudType)EditorGUILayout.EnumPopup(_pointCloudType);


        EditorGUILayout.LabelField("Step 2. Options", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Regular grid size: {_gridSize}");
        _gridSize = (int)EditorGUILayout.Slider(_gridSize, 1, 50);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Point density: {_pointDensity * 100:F2}%");
        _pointDensity = MathB.RoundToNearest(EditorGUILayout.Slider(_pointDensity, 0, 0.5f), 0.0025f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Point prefab");
        _point = (GameObject)EditorGUILayout.ObjectField(_point, typeof(GameObject), true);
        EditorGUILayout.EndHorizontal();

        GUIOptions(_pointCloudType);

        EditorGUILayout.LabelField("Step 3. Generate", EditorStyles.boldLabel);
        if (GUILayout.Button("Create Point Cloud Prefab"))
        {
            CreatePointCloudPrefab(CreateGenerator(_pointCloudType));
        }
    }

    private void GUIOptions(PointCloudType type)
    {
        switch (type)
        {
            case PointCloudType.Distance:
                GUIDistanceOptions();
                return;
            case PointCloudType.Cluster:
                GUIClusterOptions();
                return;
            case PointCloudType.Selection:
                GUISelectionOptions();
                return;
            case PointCloudType.CuttingPlane:
                GUICuttingOptions();
                return;
            default:
                throw new ArgumentException($"Unknown type {type}");
        }
    }

    private void GUIDistanceOptions()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Pair distance ratio: {_distanceRatio:F2}");
        _distanceRatio = MathB.RoundToNearest(EditorGUILayout.Slider(_distanceRatio, 0.1f, 0.9f), 0.01f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("First pair color");
        _distanceFirstPairColor = EditorGUILayout.ColorField(_distanceFirstPairColor);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Second pair color");
        _distanceSecondPairColor = EditorGUILayout.ColorField(_distanceSecondPairColor);
        EditorGUILayout.EndHorizontal();

    }

    private void GUISelectionOptions()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Number of targets: {_selectionNumberOfTargets}");
        _selectionNumberOfTargets = (int)EditorGUILayout.Slider(_selectionNumberOfTargets, 1, 10);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Selectable color");
        _selectionSelectableColor = EditorGUILayout.ColorField(_selectionSelectableColor);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Selected color");
        _selectionSelectedColor = EditorGUILayout.ColorField(_selectionSelectedColor);
        EditorGUILayout.EndHorizontal();

    }

    private void GUIClusterOptions()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Number of clusters: {_clusterNumberOfClusters}");
        _clusterNumberOfClusters = (int)EditorGUILayout.Slider(_clusterNumberOfClusters, 1, 10);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Number of points in cluster: {_clusterPointsPerCluster}");
        _clusterPointsPerCluster = (int)EditorGUILayout.Slider(_clusterPointsPerCluster, 1, 50);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Standard deviation of cluster points: {_clusterStdDevInCluster:F2}");
        _clusterStdDevInCluster = MathB.RoundToNearest(EditorGUILayout.Slider(_clusterStdDevInCluster, 0, 4), 0.1f);
        EditorGUILayout.EndHorizontal();
    }

    private void GUICuttingOptions()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Number of points in cluster: {_cuttingPointsPerCluster}");
        _cuttingPointsPerCluster = (int)EditorGUILayout.Slider(_cuttingPointsPerCluster, 1, 50);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Standard deviation of cluster points: {_cuttingStdDevInCluster:F2}");
        _cuttingStdDevInCluster = MathB.RoundToNearest(EditorGUILayout.Slider(_cuttingStdDevInCluster, 0, 4), 0.1f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Distance between clusters: {_cuttingClusterDistance}");
        _cuttingClusterDistance = MathB.RoundToNearest(EditorGUILayout.Slider(_cuttingClusterDistance, 1, _gridSize), 0.25f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cluster color");
        _cuttingClusterColor = EditorGUILayout.ColorField(_cuttingClusterColor);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cluster touching color");
        _cuttingClusterTouchingColor = EditorGUILayout.ColorField(_cuttingClusterTouchingColor);
        EditorGUILayout.EndHorizontal();
    }

    private IPointCloudGenerator CreateGenerator(PointCloudType type)
    {
        var go = new GameObject($"{type} Point Cloud")
        {
            isStatic = true
        };

        var wireframe = Instantiate(Wireframe, go.transform);
        wireframe.name = "Wireframe";

        Action<PointCloud> baseInit = (script) =>
        {
            script.PointPrefab = _point;
            script.RegularGridSize = _gridSize;
            script.RegularPointDensity = _pointDensity;
        };

        switch (type)
        {
            case PointCloudType.Distance:
                return go.AddComponentWithInit<DistancePointCloud>(script =>
                {
                    baseInit(script);
                    script.PairDistanceRatio = _distanceRatio;
                    script.FirstPairColor = _distanceFirstPairColor;
                    script.SecondPairColor = _distanceSecondPairColor;
                });
            case PointCloudType.Cluster:
                return go.AddComponentWithInit<ClusterPointCloud>(script =>
                {
                    baseInit(script);
                    script.NumberOfClusters = _clusterNumberOfClusters;
                    script.NumberOfPointsPerCluster = _clusterPointsPerCluster;
                    script.StdDevInCluster = _clusterStdDevInCluster;
                });
            case PointCloudType.Selection:
                return go.AddComponentWithInit<SelectionPointCloud>(script =>
                {
                    baseInit(script);
                    script.NumberOfTargets = _selectionNumberOfTargets;
                    script.SelectableColor = _selectionSelectableColor;
                    script.SelectedColor = _selectionSelectedColor;
                });
            case PointCloudType.CuttingPlane:
                return go.AddComponentWithInit<CuttingPointCloud>(script =>
                {
                    baseInit(script);
                    script.NumberOfPointsPerCluster = _cuttingPointsPerCluster;
                    script.StdDevInCluster = _cuttingStdDevInCluster;
                    script.ClusterDistance = _cuttingClusterDistance;
                    script.ClusterColor = _cuttingClusterColor;
                    script.ClusterTouchingColor = _cuttingClusterTouchingColor;
                });
            default:
                DestroyImmediate(go);
                throw new ArgumentException($"Unknown type {type}");
        }
    }

    private static void CreatePointCloudPrefab(IPointCloudGenerator generator)
        => generator.GeneratePointCloud();
}
