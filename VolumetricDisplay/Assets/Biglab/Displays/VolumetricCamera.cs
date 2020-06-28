using System;
using UnityEngine;

namespace Biglab.Displays
{
    public sealed class VolumetricCamera : SingletonMonobehaviour<VolumetricCamera>
    {
        /// <summary>
        /// Get or set the culling mask of this viewer.
        /// </summary>
        public LayerMask CullingMask
        {
            get { return _cullingMask; }

            set
            {
                if (value == _cullingMask)
                {
                    return;
                }

                _cullingMask = value;
                PropertiesChanged?.Invoke();
            }
        }

        /// <summary>
        /// Get or set the color of this viewer.
        /// </summary>
        public Color ClearColor
        {
            get { return _clearColor; }

            set
            {
                if (value == _clearColor)
                {
                    return;
                }

                _clearColor = value;
                PropertiesChanged?.Invoke();
            }
        }

        /// <summary>
        /// Get or set the color of this viewer.
        /// </summary>
        public CameraClearFlags ClearFlags
        {
            get { return _clearFlags; }

            set
            {
                if (value == _clearFlags)
                {
                    return;
                }

                _clearFlags = value;
                PropertiesChanged?.Invoke();
            }
        }

        /// <summary>
        /// The replacement shader used when rendering from the viewers perspective.
        /// </summary>
        public Shader ReplacementShader => _replacementShader;

        public Mesh BoundaryMesh;

        public Material BoundaryMaterial;

        [SerializeField] private LayerMask _cullingMask = -1;

        [SerializeField] private CameraClearFlags _clearFlags = CameraClearFlags.Color;

        [SerializeField] private Color _clearColor = new Color(0.54f, 0.82f, 0.55f);

        [Space] [SerializeField] private Shader _replacementShader;

        public event Action PropertiesChanged;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, 0.5F); 
            // DF: Changed radius from 1 to 0.5f. This gives the camera a diameter of 1.
        }
    }
}