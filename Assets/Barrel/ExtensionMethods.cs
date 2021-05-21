using UnityEngine;

public static class ExtensionMethods
{
    public static Vector3 Round(this Vector3 v)
    {
        return new Vector3(Mathf.Round(v.x), 0f, Mathf.Round(v.z));
    }

    public static Vector3 Round(this Vector3 v, float s) { return (v / s).Round() * s; }

    public static float Round(this float v, float s) { return Mathf.Round(v / s) * s; }
}