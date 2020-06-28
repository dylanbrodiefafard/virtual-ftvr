using Biglab.IO.Networking;

using UnityEngine;

namespace Biglab.Remote
{
    public class RemoteMenuStreamSwitcher : MonoBehaviour
    {
        [SerializeField, ReadOnly]
        private RemoteMenuToggle _streamToggle;

        private void Start()
        {
            _streamToggle = gameObject.AddComponent<RemoteMenuToggle>();
            _streamToggle.ValueChanged += StreamToggle_ValueChanged;
            _streamToggle.Label = "Enable Image Streaming";
            _streamToggle.IsSelected = false;
            _streamToggle.Group = null;

            // 
            _streamToggle.Group = null;
            _streamToggle.Order = 1;
        }

        private void StreamToggle_ValueChanged(bool value, INetworkConnection conn)
        {
            if (RemoteSystem.Instance.HasClientInfo(conn.Id))
            {
                var info = RemoteSystem.Instance.GetClientInfo(conn.Id);
                info.Viewer.EnableFrameCapture = value;
            }
        }
    }
}