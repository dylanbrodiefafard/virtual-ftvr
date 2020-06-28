using UnityEngine;

namespace Biglab.Remote
{
    public class RemoteTouchVisualizer : MonoBehaviour
    {
        private void Awake()
            => RemoteInput.Touched += OnTouch;

        private static void OnTouch(int id, RemoteTouch[] touches)
        {
            var viewer = RemoteSystem.Instance.GetViewer(id);
            var cameraGameObject = viewer.LeftOrMonoCamera;

            foreach (var touch in touches)
            {
                var ray = cameraGameObject.ViewportPointToRay(touch.Position);
                Debug.DrawRay(ray.origin, ray.direction * 5, Color.green, 0.1F);
            }
        }
    }
}