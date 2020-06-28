using UnityEngine;

[ExecuteInEditMode]
public class MaterialPropertyBlockSetter : MonoBehaviour
{
    public const string MainColor = "_Color";

    public Renderer Renderer;

    public MaterialPropertyBlock Block { get; private set; }

    #region MonoBehaviour

    private void Awake()
    {
        if (Block == null)
        {
            Block = new MaterialPropertyBlock();
        }

        if (Renderer != null)
        {
            return; // Renderer was set manually. Everything is good;
        }

        if (Renderer == null)
        {
            Renderer = GetComponent<Renderer>();
        }

        if (Renderer != null)
        {
            return; // Found a renderer. Everything is good.
        }

        Debug.LogWarning(
            $"No renderer is set or found on this gameobject, but {typeof(MaterialPropertyBlockSetter)} requires one. {gameObject.name} has been disabled.");
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (Renderer == null)
        {
            return;
        }

        Renderer.SetPropertyBlock(Block);
    }

    #endregion
}