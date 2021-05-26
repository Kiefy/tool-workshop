using UnityEditor;
using UnityEngine;

public class SpawnablePrefab : MonoBehaviour
{
    public float height = 1f;
    public float lineWidth = 1f;
    public float handleSize = 1f;

    private void OnDrawGizmosSelected()
    {
        Transform t = transform;
        Vector3 up = t.up;

        Vector3 bot = t.position;
        Vector3 top = bot + up * height;

        Handles.color = Color.cyan;
        Handles.DrawAAPolyLine(lineWidth, bot, top);
        Handles.color = Color.white;

        DrawDisc(bot, up, handleSize);
        DrawDisc(top, up, handleSize);
    }

    private static void DrawDisc(Vector3 center, Vector3 normal, float radius)
    {
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(center, normal, radius);
        Handles.color = Color.white;
    }

    public void Test(GameObject foo)
    {
        foo.GetComponent<Test>();
        Physics.Raycast(new Ray(Vector3.one, Vector3.up));
    }
}