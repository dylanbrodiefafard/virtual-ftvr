using System;
using UnityEngine;

namespace Biglab.Utility
{
    public sealed class EyeCallback : MonoBehaviour
    {
        public event Action<Camera.MonoOrStereoscopicEye> RenderingEye;

        public Camera.MonoOrStereoscopicEye Eye = Camera.MonoOrStereoscopicEye.Mono;

        private void OnPreRender()
        {
            if (Camera.current.stereoActiveEye == Eye)
            {
                RenderingEye?.Invoke(Eye);
            }
        }
    }
}