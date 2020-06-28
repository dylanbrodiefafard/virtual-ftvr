using Biglab.Displays;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeColorSwitcher : MonoBehaviour
{
    public Renderer Renderer;

    public Color MonoColor = Color.green;

    public Color LeftColor = Color.red;

    public Color RightColor = Color.blue;

    IEnumerator Start()
    {
        yield return null;
        var primary = DisplaySystem.Instance.PrimaryViewer;
        primary.EyeRenderCallback += EyeRenderCallback;
    }

    private void EyeRenderCallback(Camera.MonoOrStereoscopicEye eye)
    {
        switch (eye)
        {
            case Camera.MonoOrStereoscopicEye.Mono:
                Renderer.material.color = MonoColor;
                break;

            case Camera.MonoOrStereoscopicEye.Left:
                Renderer.material.color = LeftColor;
                break;

            case Camera.MonoOrStereoscopicEye.Right:
                Renderer.material.color = RightColor;
                break;
        }
    }
}
