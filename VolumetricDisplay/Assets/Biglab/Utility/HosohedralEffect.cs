using System;
using System.IO;
using UnityEngine;
using Biglab.Displays;
using Biglab.Extensions;

namespace Biglab.Utility
{
    public class HosohedralEffect : MonoBehaviour
    {
        [Range(3, 32)]
        public int Lunes = 4;

        [NonSerialized]
        private Material _material;

        private bool _saveScreenshot;

        private void Awake()
        {
            var shader = Shader.Find("Biglab/HosohedralProjection");

            if (shader == null)
            {
                throw new NullReferenceException(nameof(shader));
            }

            _material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Alpha1))
            {
                _saveScreenshot = true;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            var viewer = DisplaySystem.Instance.PrimaryViewer;
            _material.SetInt("_Lunes", Lunes);
            _material.SetMatrix("_Volume2World", DisplaySystem.Instance.VolumeToWorld);
            _material.SetMatrix("_ViewerMatrix", viewer.GetShaderMatrix(Camera.MonoOrStereoscopicEye.Left, false));
            _material.SetTexture("_ViewerTex", viewer.GetEyeTexture(Camera.MonoOrStereoscopicEye.Left));
            _material.SetVector("_ViewerPosition", VolumetricCamera.Instance.transform.InverseTransformPoint(viewer.LeftAnchor.position));

            var currentDestination = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            Graphics.Blit(source, currentDestination, _material);
            var currentSource = currentDestination;
            Graphics.Blit(currentSource, destination, _material);

            if (_saveScreenshot)
            {
                var bytes = currentSource.ExtractTexture2D().EncodeToPNG();

                File.WriteAllBytes(Guid.NewGuid() + ".png", bytes);

                _saveScreenshot = false;
            }

            RenderTexture.ReleaseTemporary(currentSource);
        }
    }
}