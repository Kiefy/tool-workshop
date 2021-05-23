using UnityEngine;

public static class ExtensionMethods
{
    // Round Vector3.xz (Remove decimals on XZ axis only)
    public static Vector3 Round(this Vector3 v)
    {
        return new Vector3(Mathf.Round(v.x), v.y, Mathf.Round(v.z));
    }

    // Round to increments of float s
    public static Vector3 Round(this Vector3 v, float s) { return (v / s).Round() * s; }
    public static float Round(this float v, float s) { return Mathf.Round(v / s) * s; }

    // Force a minimum
    public static int AtLeast(this int v, int min) { return Mathf.Max(v, min); }
    public static float AtLeast(this float v, float min) { return Mathf.Max(v, min); }

    // Force a maximum
    public static int AtMost(this int v, int max) { return Mathf.Min(v, max); }
    public static float AtMost(this float v, float max) { return Mathf.Min(v, max); }

    // Easy log - ie. "Hello".Log();
    public static void Log(this string s) { Debug.Log(s); }
}