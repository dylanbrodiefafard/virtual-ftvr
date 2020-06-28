using System.Linq;

using UnityEngine;

[ExecuteInEditMode]
public class MaterialPropertyBlockColors : MonoBehaviour
{
    public MaterialPropertyBlock Block { get; private set; }

    public Renderer[] Renderers;

    [Space]

    public Color AlbedoColor;

    public Color EmissionColor;

    public bool ApplyEmission = false;

    #region MonoBehaviour

    private void Awake()
    {
        PrepareBlockAndRenderers();
    }

    private void Reset()
    {
        PrepareBlockAndRenderers();
    }

    private void PrepareBlockAndRenderers()
    {
        if (Block == null)
        {
            Block = new MaterialPropertyBlock();
        }

        if (Renderers != null)
        {
            return; // Renderer was set manually. Everything is good;
        }

        if (Renderers == null)
        {
            Renderers = GetComponentsInChildren<Renderer>();
        }
    }

    private void LateUpdate()
    {
        // Quit if no renderers found
        if (Renderers == null || Renderers.Length == 0) { return; }

        // Create block if not existing
        if (Block == null) { Block = new MaterialPropertyBlock(); }

        // Set colors
        Block.SetColor("_Color", AlbedoColor);
        if (ApplyEmission) { Block.SetColor("_EmissionColor", EmissionColor); }

        // Apply the block
        foreach (var renderer in Renderers.Where(r => !r.HasPropertyBlock()))
        {
            renderer.SetPropertyBlock(Block);
        }
    }

    #endregion
}