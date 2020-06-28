using Biglab.Remote;
using UnityEngine;

public class SceneTouchHandler : MonoBehaviour
{
    private enum SceneInteractionState
    {
        ReadyState,
        DragBackground,
        DragCube
    }

    GameObject cube = null;
    float lastX = 0;
    float lastY = 0;

    private SceneInteractionState state = SceneInteractionState.ReadyState;

    private void Awake()
    {
        RemoteInput.Touched += OnTouch;
    }

    /*
    private void Update()
    {
        if (RemoteTouch.GetTouchCount(1) > 0)
        {
            var touches = RemoteTouch.GetTouches(1);

            switch (state)
            {
                case SceneInteractionState.ReadyState:
                    // determine if touch point is in the cube
                    var ray = viewer.Viewer.MainCamera.ViewportPointToRay(touches[0].position);
                    Debug.DrawRay(ray.origin, ray.direction * 10, Color.green, 1);
                    if (Physics.Raycast(ray, out hit) && hit.transform.gameObject.name == "Cube")
                    {
                        cube = hit.transform.gameObject;
                        state = SceneInteractionState.DragCube;
                        lastX = viewer.Viewer.MainCamera.ViewportToScreenPoint(touches[0].position).x;
                        lastY = viewer.Viewer.MainCamera.ViewportToScreenPoint(touches[0].position).y;
                    }
                    else
                    {
                        state = SceneInteractionState.DragBackground;
                    }
                    Debug.Log(touches[0].position);
                    break;
                case SceneInteractionState.DragCube:
                    var point = viewer.Viewer.MainCamera.ViewportToScreenPoint(touches[0].position);
                    Debug.Log(new Vector3(point.x - lastX, point.y - lastY, 0));
                    cube.GetComponent<MeshRenderer>().material.color = new Color(point.x / viewer.Viewer.MainCamera.pixelWidth, point.y / viewer.Viewer.MainCamera.pixelHeight, 0.25f);
                    lastX = point.x;
                    lastY = point.y;
                    if (touches[0].phase == TouchPhase.Ended)
                    {
                        state = SceneInteractionState.ReadyState;
                    }
                    break;
                case SceneInteractionState.DragBackground:
                    if (touches[0].phase == TouchPhase.Ended)
                    {
                        state = SceneInteractionState.ReadyState;
                    }
                    break;
            }
        }
    }*/

    public void OnTouch(int id, RemoteTouch[] touches)
    {
        var viewer = RemoteSystem.Instance.GetViewer(id);

        RaycastHit hit;
        Debug.Log(state);
        switch (state)
        {
            case SceneInteractionState.ReadyState:
                // determine if touch point is in the cube
                var ray = viewer.LeftOrMonoCamera.ViewportPointToRay(touches[0].Position);
                Debug.DrawRay(ray.origin, ray.direction * 10, Color.green, 1);
                if (Physics.Raycast(ray, out hit) && hit.transform.gameObject.name == "Cube")
                {
                    cube = hit.transform.gameObject;
                    state = SceneInteractionState.DragCube;
                    lastX = viewer.LeftOrMonoCamera.ViewportToScreenPoint(touches[0].Position).x;
                    lastY = viewer.LeftOrMonoCamera.ViewportToScreenPoint(touches[0].Position).y;
                }
                else
                {
                    state = SceneInteractionState.DragBackground;
                }

                Debug.Log(touches[0].Position);
                break;

            case SceneInteractionState.DragCube:
                var point = viewer.LeftOrMonoCamera.ViewportToScreenPoint(touches[0].Position);
                Debug.Log(new Vector3(point.x - lastX, point.y - lastY, 0));
                cube.GetComponent<MeshRenderer>().material.color = new Color(
                    point.x / viewer.LeftOrMonoCamera.pixelWidth, point.y / viewer.LeftOrMonoCamera.pixelHeight, 0.25f);
                lastX = point.x;
                lastY = point.y;
                if (touches[0].Phase == TouchPhase.Ended)
                {
                    state = SceneInteractionState.ReadyState;
                }

                break;

            case SceneInteractionState.DragBackground:
                if (touches[0].Phase == TouchPhase.Ended)
                {
                    state = SceneInteractionState.ReadyState;
                }

                break;
        }
    }
}