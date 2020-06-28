using System;
using Biglab.Calibrations;
using Biglab.Extensions;
using Biglab.Math;
using Biglab.Utility;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class BiglabProjector : MonoBehaviour
{
    [Header("Projector Parameters")]
    [Tooltip("Luminosity (brightness) of projector in lumens")]
    public float Luminosity;

    [Tooltip("The distance to the near plane of the projector.")]
    public float NearPlane;

    [Tooltip("The distance to the far plane of the projector.")]
    public float FarPlane;

    [Tooltip("The Projector's Intrinsic Parameters")]
    public Intrinsics ProjectorIntrinsics;

    [Header("Shader Behaviour")]
    [Tooltip("Should the projector be occluded by objects?")]
    public bool UseOcclusion;

    public Texture ImageSource;
    public Texture2D LuminosityMask;

    public LayerMask ProjectorIgnoreMask;
    private Material _projectorMaterial;
    private Projector _projector;
    private Camera _occlusionCamera;
    private Matrix4x4 _projection;

    protected Material ProjectorMaterial
    {
        get
        {
            InitializeProjectorMaterialIfNull();
            return _projectorMaterial;
        }
    }

    protected Projector Projector
    {
        get
        {
            InitializeProjectorIfNull();
            return _projector;
        }
    }

    protected Camera OcclusionCamera
    {
        get
        {
            InitializeOcclusionCameraIfNull();
            return _occlusionCamera;
        }
    }

    public Matrix4x4 WorldToClipMatrix => _projection * transform.worldToLocalMatrix;

    public Matrix4x4 GetWorldToClipShaderMatrix(bool isRenderToTexture = false)
        => GL.GetGPUProjectionMatrix(_projection, isRenderToTexture) * OcclusionCamera.worldToCameraMatrix;

    public Matrix4x4 GetClipToWorldShaderMatrix(bool isRenderToTexture = false)
        => OcclusionCamera.cameraToWorldMatrix * GL.GetGPUProjectionMatrix(_projection, isRenderToTexture).inverse;

    protected void InitializeProjectorMaterialIfNull()
    {
        if (_projectorMaterial != null)
        {
            return;
        }

        var shader = Shader.Find("Projector/Biglab");
        if (shader == null)
        {
            Debug.LogError("Could not find Biglab Projector material.");
            return;
        }

        _projectorMaterial = new Material(shader);
    }

    protected void InitializeProjectorIfNull()
    {
        if (_projector != null)
        {
            return;
        }

        var projectorComponent = gameObject.GetComponent<Projector>();
        if (projectorComponent == null)
        {
            projectorComponent = gameObject.AddComponent<Projector>();
        }

        projectorComponent.hideFlags = HideFlags.HideInInspector;

        projectorComponent.material = ProjectorMaterial;
        projectorComponent.orthographic = false;
        projectorComponent.ignoreLayers = ProjectorIgnoreMask;
        projectorComponent.nearClipPlane = NearPlane;
        projectorComponent.farClipPlane = FarPlane;

#if UNITY_EDITOR
        UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(projectorComponent, false);
#endif
        _projector = projectorComponent;
    }

    protected void InitializeOcclusionCameraIfNull()
    {
        if (_occlusionCamera != null)
        {
            return;
        }

        var cameraComponent = gameObject.GetComponent<Camera>();
        if (cameraComponent == null)
        {
            cameraComponent = gameObject.AddComponent<Camera>();
        }

        cameraComponent.hideFlags = HideFlags.HideInInspector;

        var depthDescriptor = new RenderTextureDescriptor
        {
            width = ProjectorIntrinsics.PixelWidth,
            height = ProjectorIntrinsics.PixelHeight,
            autoGenerateMips = false,
            colorFormat = RenderTextureFormat.Depth,
            depthBufferBits = 32,
            vrUsage = VRTextureUsage.None,
            volumeDepth = 1,
            msaaSamples = 1,
            dimension = TextureDimension.Tex2D
        };
        var occlusionTexture = new RenderTexture(depthDescriptor);

        cameraComponent.enabled = false;
        cameraComponent.clearFlags = CameraClearFlags.Depth;
        cameraComponent.cullingMask = ~Projector.ignoreLayers;
        cameraComponent.targetTexture = occlusionTexture;
        cameraComponent.allowHDR = false;
        cameraComponent.allowDynamicResolution = false;
        cameraComponent.targetDisplay = 7;

#if UNITY_EDITOR
        UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(cameraComponent, false);
#endif
        _occlusionCamera = cameraComponent;
    }

    protected void SyncProjectorAndCameraComponentProperties()
    {
        // FoV computation: https://stackoverflow.com/a/41137160
        Projector.fieldOfView =
            2F * Mathf.Atan(ProjectorIntrinsics.PixelWidth / (2 * ProjectorIntrinsics.FocalLengths.y)) * Mathf.Rad2Deg;
        Projector.aspectRatio = ProjectorIntrinsics.PixelWidth / (float)ProjectorIntrinsics.PixelHeight;
        Projector.ignoreLayers = ProjectorIgnoreMask;
        Projector.farClipPlane = FarPlane;
        Projector.nearClipPlane = NearPlane;

        OcclusionCamera.projectionMatrix = _projection;
        OcclusionCamera.fieldOfView = Projector.fieldOfView;
        OcclusionCamera.aspect = Projector.aspectRatio;
        OcclusionCamera.nearClipPlane = Projector.nearClipPlane;
        OcclusionCamera.farClipPlane = Projector.farClipPlane;
        OcclusionCamera.cullingMask = ~ProjectorIgnoreMask;
    }

    public void UpdateMaterialProperties()
    {
        ProjectorMaterial.SetTexture("_MainTex", ImageSource);

        ProjectorMaterial.SetTexture("_AlphaTex", LuminosityMask);
        ProjectorMaterial.SetMatrix("_WorldToProjectorClip", GetWorldToClipShaderMatrix());
        ProjectorMaterial.SetMatrix("_ProjectorToWorld", OcclusionCamera.cameraToWorldMatrix);
        ProjectorMaterial.SetVector("_WorldSpaceProjPos", transform.position);
        ProjectorMaterial.SetFloat("_Luminosity", Luminosity);

        ProjectorMaterial.SetFloat("_Intrinsics", 1);
        ProjectorMaterial.EnableKeyword("INTRINSICS");

        ProjectorMaterial.SetVector("_RadialDistortion", ProjectorIntrinsics.RadialDistortionCoefficients);
        ProjectorMaterial.SetVector("_TangentialDistortion", ProjectorIntrinsics.TangentialDistortionCoefficients);

        ProjectorMaterial.SetFloat("_Occlusion", Convert.ToInt32(UseOcclusion));
        if (UseOcclusion)
        {
            ProjectorMaterial.EnableKeyword("OCCLUSION");

            ProjectorMaterial.SetTexture("_OcclusionTex", OcclusionCamera.targetTexture);
        }
        else
        {
            ProjectorMaterial.DisableKeyword("OCCLUSION");
        }
    }

    private void OnEnable()
    {
        if (_projector != null)
        {
            DestroyImmediate(_projector);
        }

        if (_occlusionCamera != null)
        {
            DestroyImmediate(_occlusionCamera);
        }

        _projector = null;
        _projectorMaterial = null;
        _occlusionCamera = null;
        _projection = Matrix4x4.identity;
    }

    private void LateUpdate()
    {
        _projection = Projection.ComputeProjectionMatrixFromIntrinsics(ProjectorIntrinsics, NearPlane, FarPlane);
        SyncProjectorAndCameraComponentProperties();
        if (UseOcclusion)
        {
            OcclusionCamera.Render();
        }

        UpdateMaterialProperties();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Bizmos.DrawProjectionFrustum(_projection, OcclusionCamera.worldToCameraMatrix);
    }

    public GeometryTexture GenerateGeometryTexture(Transform surface)
    {
        _projection = Projection.ComputeProjectionMatrixFromIntrinsics(ProjectorIntrinsics, NearPlane, FarPlane);

        var tmp = new GameObject("Temporary Renderer");
        var script = tmp.AddComponentWithInit<GeometryTextureRenderer>(t =>
        {
            t.transform.position = transform.position;
            t.transform.rotation = transform.rotation;
        });

        var positionTexture = new Texture2D(ProjectorIntrinsics.PixelWidth, ProjectorIntrinsics.PixelHeight,
                TextureFormat.RGBAFloat, false)
        {
            name = $"{gameObject.name} : {surface.gameObject.name} positions texture"
        };

        var normalTexture = new Texture2D(ProjectorIntrinsics.PixelWidth, ProjectorIntrinsics.PixelHeight,
            TextureFormat.RGBAFloat, false)
        {
            name = $"{gameObject.name} : {surface.gameObject.name} normals texture"
        };

        script.ComputeGeometryTextureData(_projection, NearPlane, FarPlane, surface, positionTexture, normalTexture);
        Destroy(tmp);

        return new GeometryTexture(positionTexture, normalTexture);
    }
}