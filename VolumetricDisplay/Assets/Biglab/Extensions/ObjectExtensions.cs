namespace Biglab.Extensions
{
    public static class ObjectExtensions
    {
        public static bool IsNull(this object T)
        {
            return T == null;
        }
    }
}