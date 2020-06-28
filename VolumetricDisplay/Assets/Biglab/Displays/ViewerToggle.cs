using Biglab.Remote;

using System.Collections.Generic;

using UnityEngine;

namespace Biglab.Displays
{
    public class ViewerToggle : MonoBehaviour
    {
        private static Dictionary<ViewerRole, bool> _viewerStateMemory;

        [SerializeField]
        private Viewer _viewer;

        [SerializeField, ReadOnly]
        private RemoteMenuToggle _viewerToggle;

        #region MonoBehaviour

        private void Start()
        {
            // 
            if (_viewer == null)
            {
                Debug.Log($"Unable to start {nameof(ViewerToggle)}, has not reference to viewer.");
                enabled = false;
                return;
            }

            // Recover viewer enabled state ( persistent state )
            if (HasViewerState)
            {
                _viewer.enabled = GetViewerState();
            }

            // Store what the viewer state is
            SetViewerState(_viewer.enabled);

            // Create toggle
            _viewerToggle = gameObject.AddComponent<RemoteMenuToggle>();
            _viewerToggle.IsSelected = _viewer.enabled;
            _viewerToggle.Label = $"Enable {_viewer.Role} Viewer";
            _viewerToggle.Group = "Viewers"; // Set to viewers group
            _viewerToggle.ValueChanged += (state, conn) =>
            {
                _viewer.enabled = state;
                SetViewerState(state);
            };
        }

        private void OnDestroy()
        {
            Destroy(_viewerToggle);
        }

        private void OnApplicationQuit()
        {
            // Clear static memory ( in hopes to prevent editor from persisting )
            _viewerStateMemory = null;
        }

        #endregion

        #region Viewer State

        private bool GetViewerState()
        {
            var role = _viewer.Role;
            if (_viewerStateMemory?.ContainsKey(role) ?? false)
            {
                return _viewerStateMemory[_viewer.Role];
            }
            else
            {
                return _viewer.enabled;
            }
        }

        private void SetViewerState(bool state)
        {
            // 
            if (_viewerStateMemory == null)
            {
                _viewerStateMemory = new Dictionary<ViewerRole, bool>();
            }

            // Store state
            _viewerStateMemory[_viewer.Role] = state;
        }

        private bool HasViewerState
        {
            get
            {
                var role = _viewer.Role;
                if (_viewerStateMemory?.ContainsKey(role) ?? false)
                {
                    return true;
                }

                return false;
            }
        }

        #endregion
    }
}