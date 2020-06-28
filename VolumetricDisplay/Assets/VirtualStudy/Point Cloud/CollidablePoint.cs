public class CollidablePoint : InteractivePoint
{
    public void OnCollisionEnter()
    {
        SetColor(HighlightColor);
    }

    public void OnCollisionExit()
    {
        SetColor(OriginalColor);
    }
}
