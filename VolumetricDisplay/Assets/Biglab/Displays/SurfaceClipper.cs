using System.Collections;
using Biglab.Displays;
using UnityEngine;

public class SurfaceClipper : MonoBehaviour {
    public Color NearIntersectionColor = Color.white;
    [Range(0, 0.1f)]
    public float NearIntersectionDistance = 0.01f;
    [Range(0, 1000)]
    public int NearIntersectionAnimationLines = 20;
    [Range(0, 1000)]
    public float NearIntersectionAnimationSpeed = 50;

    private RenderTexture _depthTexture;
    private Shader _depthShader;

    private IEnumerator Start()
    {
        _depthShader = Shader.Find("Render Depth");
        _depthTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth)
        {
            name = "Outer Surface Depth (Mono)"
        };

        Shader.SetGlobalTexture("_FrontDepthTex", _depthTexture);
        Shader.SetGlobalColor("_NearIntersectionColor", NearIntersectionColor);
        Shader.SetGlobalFloat("_NearIntersectionDistance", NearIntersectionDistance);
        Shader.SetGlobalInt("_NearIntersectionAnimationLines", NearIntersectionAnimationLines);
        Shader.SetGlobalFloat("_NearIntersectionAnimationSpeed", NearIntersectionAnimationSpeed);

        yield return DisplaySystem.Instance.GetWaitForPrimaryViewer();

        DisplaySystem.Instance.PrimaryViewer.PreEyePass += Viewer_PreEyePass;
    }

    private void Viewer_PreEyePass(Viewer viewer, Camera.MonoOrStereoscopicEye eye)
    {
        Debug.Log("Here");
        var eyeCamera = viewer.GetEyeCamera(eye);
        // Render into the depth texture using the viewer camera
        var originalTarget = eyeCamera.targetTexture;
        var originalRenderTexture = RenderTexture.active;
        var originalCullingMask = eyeCamera.cullingMask;
        var layer = LayerMask.NameToLayer("Surface Clipping");

        Graphics.DrawMesh(VolumetricCamera.Instance.BoundaryMesh, 
            VolumetricCamera.Instance.transform.localToWorldMatrix,
            VolumetricCamera.Instance.BoundaryMaterial,
            layer,
            eyeCamera);

        Shader.SetGlobalMatrix("_CaptureWorldToClipMono", viewer.GetShaderMatrix(eye, false));
        eyeCamera.cullingMask = 1 << layer;
        eyeCamera.targetTexture = _depthTexture;
        eyeCamera.RenderWithShader(_depthShader, "RenderType");

        eyeCamera.cullingMask = originalCullingMask;
        eyeCamera.targetTexture = originalTarget;
        RenderTexture.active = originalRenderTexture;
    }

    // Only runs in editor
    private void OnValidate()
    {
        Shader.SetGlobalColor("_NearIntersectionColor", NearIntersectionColor);
        Shader.SetGlobalFloat("_NearIntersectionDistance", NearIntersectionDistance);
        Shader.SetGlobalInt("_NearIntersectionAnimationLines", NearIntersectionAnimationLines);
        Shader.SetGlobalFloat("_NearIntersectionAnimationSpeed", NearIntersectionAnimationSpeed);
    }
}
