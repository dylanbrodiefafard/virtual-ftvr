using Biglab.Displays;
using Biglab.Extensions;
using UnityEngine;

namespace Biglab.Utility
{
    public class GeometryTextureRenderer : MonoBehaviour
    {
        private Camera _renderCamera;
        private Mesh _mesh;
        private Transform _surface;
        private Texture2D _positionOutput;
        private Texture2D _normalOutput;
        private Material _positionMaterial;
        private Material _normalMaterial;

        private void Awake()
        {
            _renderCamera = gameObject.GetComponent<Camera>();
            if (_renderCamera == null)
            {
                _renderCamera = gameObject.AddComponent<Camera>();
            }

            _renderCamera.enabled = false;
            _renderCamera.cullingMask = 0;
            _renderCamera.clearFlags = CameraClearFlags.SolidColor;
            _renderCamera.backgroundColor = Color.black;

            // Setup materials
            _positionMaterial = new Material(Shader.Find("Biglab/GeometryTexture/Position"));
            _normalMaterial = new Material(Shader.Find("Biglab/GeometryTexture/Normal"));
        }

        public void ComputeGeometryTextureData(Matrix4x4 projection, float near, float far, Transform surface, Texture2D positionTexture, Texture2D normalTexture)
        {
            // TODO: Implement this process using intrinsics
            _positionMaterial.SetMatrix("_WorldToVolume", DisplaySystem.Instance.PhysicalToVolume * surface.worldToLocalMatrix);
            _positionMaterial.SetMatrix("_WorldToVolumeNormal", DisplaySystem.Instance.PhysicalToVolume.NormalMatrix() * surface.worldToLocalMatrix.NormalMatrix());

            // set private variables
            _mesh = surface.gameObject.GetComponent<MeshFilter>().mesh;
            _surface = surface;
            _positionOutput = positionTexture;
            _normalOutput = normalTexture;

            // Setup the camera
            _renderCamera.projectionMatrix = projection;
            _renderCamera.nearClipPlane = near;
            _renderCamera.farClipPlane = far;

            _renderCamera.targetTexture = RenderTexture.GetTemporary(positionTexture.width, positionTexture.height, 0, RenderTextureFormat.ARGBFloat);
            // TODO: Maybe allow RGBAHalf as well if we need it

            // Render!
            _renderCamera.Render(); // This should call OnPostRender()
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            // Use the position shader
            _positionMaterial.SetPass(0);
            // Render the mesh into the texture
            Graphics.DrawMeshNow(_mesh, _surface.localToWorldMatrix);
            // Compute the positions
            Graphics.Blit(src, dest, _positionMaterial);
            // Copy the data
            _renderCamera.targetTexture.ExtractTexture2D(TextureFormat.RGBAFloat, _positionOutput);

            // Use the normal shader
            _normalMaterial.SetPass(0);
            // Render the mesh into the texture
            Graphics.DrawMeshNow(_mesh, _surface.localToWorldMatrix);
            // Compute the normals
            Graphics.Blit(src, dest, _normalMaterial);
            // Copy the data
            _renderCamera.targetTexture.ExtractTexture2D(TextureFormat.RGBAFloat, _normalOutput);

            // Get rid of the temporary rendertexture now
            RenderTexture.ReleaseTemporary(_renderCamera.targetTexture);
            _renderCamera.targetTexture = null;
        }
    }
}