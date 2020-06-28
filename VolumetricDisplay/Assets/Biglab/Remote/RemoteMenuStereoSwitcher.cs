using System.Collections;
using System.Collections.Generic;

using Biglab.Collections;
using Biglab.Displays;
using Biglab.Extensions;
using Biglab.IO.Networking;

using UnityEngine;

using FallbackEye = UnityEngine.Camera.MonoOrStereoscopicEye;

namespace Biglab.Remote
{
    public class RemoteMenuStereoSwitcher : MonoBehaviour
    {
        private RemoteMenuDropdown _fallbackDropdown;
        private RemoteMenuToggle _stereoToggle;
        private Viewer _viewer => DisplaySystem.Instance.PrimaryViewer;
        private BiDictionary<int, FallbackEye> _indexEyeMapping;

        private void Awake()
            => _indexEyeMapping = EnumExtensions.GetIndexMapping<FallbackEye>();

        private IEnumerator Start()
        {
            yield return DisplaySystem.Instance.GetWaitForPrimaryViewer();

            // Create and setup stereo toggle
            _stereoToggle = gameObject.AddComponent<RemoteMenuToggle>();
            _stereoToggle.Group = "Stereo Rendering";
            _stereoToggle.Order = 0;
            _stereoToggle.ValueChanged += StereoValueChanged;
            _stereoToggle.Label = "Stereo Enabled";

            // Create and setup non-stereo fallback eye
            _fallbackDropdown = gameObject.AddComponent<RemoteMenuDropdown>();
            _fallbackDropdown.Group = "Stereo Rendering";
            _fallbackDropdown.Order = 1;
            _fallbackDropdown.ValueChanged += FallbackValueChanged;

            // Set scene list and select active scene by default
            _fallbackDropdown.Options = new List<string>
            {
                FallbackEye.Left.ToString(),
                FallbackEye.Right.ToString(),
                FallbackEye.Mono.ToString(),
            };

            // Sync and Update remote menu items with the viewer settings
            UpdateRemoteMenuState();
        }

        private void OnEnable()
        {
            DisplaySystem.Instance.RegisteredViewer += OnRegisteredViewer;
            DisplaySystem.Instance.UnregisteredViewer += OnUnregisteredViewer;

            if (_fallbackDropdown)
            {
                _fallbackDropdown.enabled = true;
                _stereoToggle.enabled = true;
            }
        }

        private void OnDisable()
        {
            if (DisplaySystem.Instance == null)
            { return; }

            DisplaySystem.Instance.RegisteredViewer -= OnRegisteredViewer;
            DisplaySystem.Instance.UnregisteredViewer -= OnUnregisteredViewer;

            _fallbackDropdown.enabled = false;
            _stereoToggle.enabled = false;
        }

        private void OnUnregisteredViewer(Viewer viewer, ViewerRole viewerRole)
            => UpdateRemoteMenuState();

        private void OnRegisteredViewer(Viewer viewer, ViewerRole viewerRole)
            => UpdateRemoteMenuState();

        private void SyncStereoToggleWithViewer()
            => _stereoToggle.enabled = _viewer.EnabledStereoRendering;

        private void SyncFallbackDropdownWithViewer()
            => _fallbackDropdown.Selected = _indexEyeMapping[_viewer.NonStereoFallbackEye];

        private void UpdateRemoteMenuState()
        {
            if (_stereoToggle == null || _fallbackDropdown == null)
            { return; }

            _stereoToggle.enabled = DisplaySystem.Instance.HasSingleViewer;
            _fallbackDropdown.enabled = _stereoToggle.enabled && !_stereoToggle.IsSelected;

            SyncStereoToggleWithViewer();
            SyncFallbackDropdownWithViewer();
        }

        private void FallbackValueChanged(int index, INetworkConnection connection)
        {
            if (index < 0)
            {
                Debug.LogWarning("Somehow chose a negative fallback index.");
                return;
            }

            _viewer.NonStereoFallbackEye = _indexEyeMapping[index];
        }

        private void StereoValueChanged(bool value, INetworkConnection connection)
        {
            _viewer.EnabledStereoRendering = value;
            _fallbackDropdown.enabled = !value;
        }
    }
}