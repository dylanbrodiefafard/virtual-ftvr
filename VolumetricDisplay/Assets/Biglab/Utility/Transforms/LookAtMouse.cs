using UnityEngine;

public class LookAtMouse : MonoBehaviour
{
    public Camera viewCamera;

    void Update()
    {
        Vector3 mouse = Input.mousePosition;
        Vector3 mouseWorld = viewCamera.ScreenToWorldPoint(new Vector3(
                                                            mouse.x,
                                                            mouse.y,
                                                            transform.position.y));
        Vector3 forward = mouseWorld - transform.position;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}
