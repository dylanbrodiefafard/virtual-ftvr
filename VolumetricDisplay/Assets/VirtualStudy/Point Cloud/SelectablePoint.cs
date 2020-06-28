public class SelectablePoint : InteractivePoint
{
    public void OnSelected()
    {
        SetColor(HighlightColor);
    }

    public void OnDeselected()
    {
        SetColor(OriginalColor);
    }

}
