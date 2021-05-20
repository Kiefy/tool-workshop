using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;

[ExecuteAlways] public class SnapperTool : EditorWindow
{
    // Add a menu item to create and open the Window
    [MenuItem("Window/Snapper Tool")]
    public static void OpenSnapperWindow() => GetWindow<SnapperTool>("Snapper Tool");

    // The spacing between grid lines
    [Range(0.01f, 10f)]
    public float gridSize = 1f;

    // Allows Undo on var changes
    private SerializedObject so;
    private SerializedProperty propGridSize;

    // When Window is instantiated
    private void OnEnable()
    {
        so = new SerializedObject(this);
        propGridSize = so.FindProperty("gridSize");

        //Selection.selectionChanged += Repaint;
        SceneView.duringSceneGui += DuringSceneGUI;

        autoRepaintOnSceneChange = true;
    }

    // When Window is closed
    private void OnDisable()
    {
        //Selection.selectionChanged -= Repaint;
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        if (Event.current.type != EventType.Repaint) return;

        const float GRID_DRAW_EXTENT = 16f;

        int lineCount = Mathf.RoundToInt((GRID_DRAW_EXTENT * 2) / gridSize);

        if (lineCount % 2 == 0) { lineCount++; }

        int halfLineCount = lineCount / 2;

        for (int i = 0; i < lineCount; i++)
        {
            int intOffset = i - halfLineCount;
            float xCoord = intOffset * gridSize;

            float zCoordStart = halfLineCount * gridSize;
            float zCoordEnd = -halfLineCount * gridSize;

            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            Vector3 p0 = new Vector3(xCoord, 0f, zCoordStart);
            Vector3 p1 = new Vector3(xCoord, 0f, zCoordEnd);
            Handles.DrawAAPolyLine(p0, p1);

            p0 = new Vector3(zCoordStart, 0f, xCoord);
            p1 = new Vector3(zCoordEnd, 0f, xCoord);
            Handles.DrawAAPolyLine(p0, p1);
        }

        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    private void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(propGridSize);
        so.ApplyModifiedProperties();

        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button(
                    "Snap Selection" /*,
                    GUILayout.Width(100f)*/
                )
            ) SnapSelection();
        }
    }

    private void SnapSelection()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go.transform, "Snap Selected GameObjects");
            go.transform.position = go.transform.position.Round(gridSize);
        }
    }
}
#endif