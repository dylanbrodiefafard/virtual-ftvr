using System.Collections.Generic;
using System.Linq;

using Biglab.Displays;
using Biglab.Extensions;
using Biglab.IO.Networking;
using Biglab.Remote;
using Biglab.Tracking;
using Biglab.Utility.Transforms;

using UnityEngine;
using UnityEngine.SceneManagement;

public class RemoteViewpointMenu : MonoBehaviour
{
    [SerializeField, ReadOnly]
    private RemoteMenuDropdown _remoteViewpointsDropdown;

    private List<Transform> _remoteViewpointAnchors;

    private void Awake()
    {
        RemoteSystem.Instance.Connected += ViewerConnectionChanged;
        RemoteSystem.Instance.Disconnected += ViewerConnectionChanged;
        SceneManager.activeSceneChanged += ActiveSceneChanged;

        _remoteViewpointsDropdown = gameObject.AddComponent<RemoteMenuDropdown>();
        _remoteViewpointsDropdown.ValueChanged += RemoteViewpointsDropdownOnValueChanged;

        // 
        _remoteViewpointsDropdown.Group = null;
        _remoteViewpointsDropdown.Order = 2;

        _remoteViewpointAnchors = new List<Transform>();
    }

    private void Start()
    {
        DiscoverRemoteViewpoints();
    }

    private void ActiveSceneChanged(Scene prev, Scene current)
    {
        DiscoverRemoteViewpoints();
    }

    private void ViewerConnectionChanged(Viewer viewer, INetworkConnection conn)
    {
        OnRemoteViewpointsChanged(FindObjectsOfType<RemoteViewpoint>());
    }

    /// <summary>
    /// Finds tracked objects without a <see cref="RemoteViewpoint"/> and add it to them.
    /// </summary>
    private void DiscoverRemoteViewpoints()
    {
        foreach (var trackedObject in FindObjectsOfType<TrackedObject>())
        {
            var remoteViewpoint = trackedObject.GetComponent<RemoteViewpoint>();

            // If the tracked object already has one, then there is no need to add one
            if (remoteViewpoint != null) { continue; }

            // Only add this to viewer tracked objects
            var viewer = trackedObject.GetComponent<Viewer>();
            if (viewer == null) { continue; }

            var baseName = $"Viewer {viewer.Role} {trackedObject.ObjectIndex}";

            var leftRemoteViewpoint = trackedObject.gameObject.AddComponent<RemoteViewpoint>();
            leftRemoteViewpoint.Alias = $"{baseName} Left Eye";
            leftRemoteViewpoint.Anchor = viewer.LeftAnchor;

            if (!viewer.EnabledStereoRendering) { continue; }

            var rightRemoteViewpoint = trackedObject.gameObject.AddComponent<RemoteViewpoint>();
            rightRemoteViewpoint.Alias = $"{baseName} Right Eye";
            rightRemoteViewpoint.Anchor = viewer.RightAnchor;
        }

        // Viewpoints have changed
        OnRemoteViewpointsChanged(FindObjectsOfType<RemoteViewpoint>());
    }

    private void RemoteViewpointsDropdownOnValueChanged(int i, INetworkConnection conn)
    {
        var remoteViewer = RemoteSystem.Instance.GetViewer(conn.Id);
        var transformLinker = remoteViewer.gameObject.GetOrAddComponent<TransformLinker>();

        if (i == 0)
        {
            remoteViewer.FrustumMode = Viewer.FrustumFittingMode.Window;

            transformLinker.enabled = false;

            return;
        }

        var anchorIndex = i - 1;

        // Don't do anything if the index is out of range
        if (anchorIndex >= _remoteViewpointAnchors.Count) { return; }

        // Change it to camera mode
        remoteViewer.FrustumMode = Viewer.FrustumFittingMode.Fixed;

        transformLinker.enabled = true;
        transformLinker.IsSourceThisTransform = false;
        transformLinker.IsTargetThisTransform = true;
        transformLinker.UpdateSource(_remoteViewpointAnchors[anchorIndex]);
    }

    private void OnRemoteViewpointsChanged(IEnumerable<RemoteViewpoint> remoteViewpoints)
    {
        _remoteViewpointAnchors.Clear();

        var options = new List<string>
        {
            "Remote Viewpoints"
        };

        foreach (var remoteViewpoint in remoteViewpoints.OrderBy(rv => rv.Alias))
        {
            options.Add(remoteViewpoint.Alias);
            _remoteViewpointAnchors.Add(remoteViewpoint.Anchor);
        }

        _remoteViewpointsDropdown.Options = options;

        // Select the placeholder value
        _remoteViewpointsDropdown.Selected = 0;
    }
}
