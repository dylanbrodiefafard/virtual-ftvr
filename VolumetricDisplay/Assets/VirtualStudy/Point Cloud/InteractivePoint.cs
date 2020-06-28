using System.Collections;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MaterialPropertyBlockSetter))]
public abstract class InteractivePoint : MonoBehaviour
{
    public Color OriginalColor;
    public Color HighlightColor;

    private MaterialPropertyBlockSetter _blockSetter;

    protected void Awake()
    {
        _blockSetter = GetComponent<MaterialPropertyBlockSetter>();
    }

    protected void Start()
    {
        SetColor(OriginalColor);
    }

    public void SetColor(Color color)
        => _blockSetter.Block.SetColor(MaterialPropertyBlockSetter.MainColor, color);

    public IEnumerable SetColor(Color color, float forSeconds)
    {
        _blockSetter.Block.SetColor(MaterialPropertyBlockSetter.MainColor, color);

        yield return new WaitForSeconds(forSeconds);
        _blockSetter.Block.SetColor(MaterialPropertyBlockSetter.MainColor, OriginalColor);
    }
}
