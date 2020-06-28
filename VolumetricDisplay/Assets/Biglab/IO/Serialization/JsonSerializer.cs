namespace Biglab.IO.Serialization
{
    public static class JsonSerializer
        // TODO: CC: Documentation
    {
        public static string SerializeJson<T>(this T @this, bool pretty = false)
        {
            return UnityEngine.JsonUtility.ToJson(@this, pretty);
            // return JsonConvert.SerializeObject(@this, pretty ? Formatting.Indented : Formatting.None);
        }

        public static T DeserializeJson<T>(this string @this)
        {
            return UnityEngine.JsonUtility.FromJson<T>(@this);
            // return JsonConvert.DeserializeObject<T>(@this);
        }
    }
}