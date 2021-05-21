using UnityEngine;

public static class ExtensionMethods
{
    // Round Vector3.xz (Remove decimals on XZ axis only)
    public static Vector3 Round(this Vector3 v)
    {
        return new Vector3(Mathf.Round(v.x), v.y, Mathf.Round(v.z));
    }

    // Round Vector3 v to increments of float s
    public static Vector3 Round(this Vector3 v, float s) { return (v / s).Round() * s; }

    // Round float v to increments of float s 
    public static float Round(this float v, float s) { return Mathf.Round(v / s) * s; }

    // Easy log - ie. "Hello".Log();
    public static void Log(this string s) { Debug.Log(s); }
}