using UnityEditor;
using UnityEngine;

public static class Snapper
{
    [MenuItem("GameObject/Snap Selected Objects %&S", isValidateFunction: true)]
    public static bool CanSnapTheThings()
    {
        return Selection.gameObjects.Length > 0;
    }

    [MenuItem("GameObject/Snap Selected Objects %&S")]
    public static void SnapTheThings()
    {
        const string UNDO_STR_SNAP = "Snap Selected GameObjects";

        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go.transform, UNDO_STR_SNAP);
            go.transform.position = go.transform.position.Round();
        }
    }

    // public static Vector3 Round(this Vector3 v)
    // {
    //     v.x = Mathf.Round(v.x);
    //     v.y = Mathf.Round(v.y);
    //     v.z = Mathf.Round(v.z);
    //     return v;
    // }
}