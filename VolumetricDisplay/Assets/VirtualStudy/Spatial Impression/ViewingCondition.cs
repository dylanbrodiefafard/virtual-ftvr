using Biglab.Extensions;
using UnityEngine;
using UnityEngine.Events;

public class ViewingCondition : MonoBehaviour
{
    public GameObject ViewerViewingCondtion;
    public Color OriginalColor;
    public Color HighlightColor;
    public UnityEvent ViewingConditionMet;

    private MaterialPropertyBlockSetter _blockSetter;

    private void Awake()
    {
        _blockSetter = gameObject.GetOrAddComponent<MaterialPropertyBlockSetter>();
        _blockSetter.Block.SetColor(MaterialPropertyBlockSetter.MainColor, OriginalColor);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == ViewerViewingCondtion)
        {
            _blockSetter.Block.SetColor(MaterialPropertyBlockSetter.MainColor, HighlightColor);
            ViewingConditionMet?.Invoke();
        }
    }

    public void ResetColor()
    {
        if (_blockSetter != null)
        {
            _blockSetter.Block.SetColor(MaterialPropertyBlockSetter.MainColor, OriginalColor);
        }
    }
}
