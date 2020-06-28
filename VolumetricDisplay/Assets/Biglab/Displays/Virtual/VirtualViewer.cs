using System;
using System.Linq;
using Biglab.Extensions;
using UnityEngine;

public class VirtualViewer : MonoBehaviour
{
    public string[] LeftEyeNames = { "LeftEyeAnchor" };
    public Camera LeftEye;

    public string[] RightEyeNames = { "RightEyeAnchor" };
    public Camera RightEye;

    private void Awake()
    {
        var parent = transform.root;

        // If the eyes are null, then try and find them automatically.
        if (LeftEye == null)
        {
            LeftEye = FindEyeCamera(parent, LeftEyeNames);
        }

        if (RightEye == null)
        {
            RightEye = FindEyeCamera(parent, RightEyeNames);
        }


        // If they eyes are still null, notify the user with a warning
        if (LeftEye == null)
        {
            throw new ArgumentNullException(nameof(LeftEye));
        }

        if (RightEye == null)
        {
            throw new ArgumentNullException(nameof(RightEye));
        }
    }

    private static Camera FindEyeCamera(Transform root, string[] names)
        => (root == null || names == null)
            ? null
            : names.Select(root.FindDeepChild)
            .Where(child => child != null)
            .Select(child => child.GetComponent<Camera>()).FirstOrDefault(camera => camera != null);
}
