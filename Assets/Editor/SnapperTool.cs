using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;

[ExecuteAlways] public class SnapperTool : EditorWindow
{
    public enum GridType
    {
        Cartesian,
        Polar
    }

    // Add a menu item to create and open the Window
    [MenuItem("Window/Snapper Tool")]
    public static void OpenSnapperWindow() => GetWindow<SnapperTool>("Snapper Tool");

    public GridType gridType = GridType.Cartesian;

    // The spacing between grid lines
    [Range(0.01f, 10f)]
    public float gridSize = 1f;

    [Range(4, 64)]
    public int angularDivisions = 24;

    // Allows Undo on var changes
    private SerializedObject so;
    private SerializedProperty propGridSize;
    private SerializedProperty propGridType;
    private SerializedProperty propAngularDivisions;

    private const float TAU = 6.28318530718f;

    // When Window is instantiated
    private void OnEnable()
    {
        so = new SerializedObject(this);
        propGridSize = so.FindProperty("gridSize");
        propGridType = so.FindProperty("gridType");
        propAngularDivisions = so.FindProperty("angularDivisions");

        // Load saved config
        gridSize = EditorPrefs.GetFloat("SNAPPER_TOOL_GRID_SIZE", 1f);
        gridType = (GridType) EditorPrefs.GetInt("SNAPPER_TOOL_GRID_TYPE", 0);
        angularDivisions = EditorPrefs.GetInt("SNAPPER_TOOL_ANGULAR_DIVISIONS", 24);

        //Selection.selectionChanged += Repaint;
        SceneView.duringSceneGui += DuringSceneGUI;

        autoRepaintOnSceneChange = true;
    }

    // When Window is closed
    private void OnDisable()
    {
        // Save config
        EditorPrefs.SetFloat("SNAPPER_TOOL_GRID_SIZE", gridSize);
        EditorPrefs.SetInt("SNAPPER_TOOL_GRID_TYPE", (int) gridType);
        EditorPrefs.SetInt("SNAPPER_TOOL_ANGULAR_DIVISIONS", angularDivisions);

        //Selection.selectionChanged -= Repaint;
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    private void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(propGridType);
        EditorGUILayout.PropertyField(propGridSize);
        if (gridType == GridType.Polar)
        {
            EditorGUILayout.PropertyField(propAngularDivisions);
            propAngularDivisions.intValue = Mathf.Max(4, propAngularDivisions.intValue);
        }

        so.ApplyModifiedProperties();

        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("Snap Selection")) SnapSelection();
        }
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        if (Event.current.type != EventType.Repaint) return;

        const float GRID_DRAW_EXTENT = 16f;

        if (gridType == GridType.Cartesian)
        {
            DrawGridCartesian(GRID_DRAW_EXTENT);
        }
        else
        {
            DrawGridPolar(GRID_DRAW_EXTENT);
        }

        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    private void DrawGridPolar(float gridDrawExtent)
    {
        int ringCount = Mathf.RoundToInt(gridDrawExtent / gridSize);

        float radiusOuter = (ringCount - 1f) * gridSize;

        Handles.zTest = CompareFunction.LessEqual;
        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        // Radial rings
        for (int i = 1; i < ringCount; i++)
        {
            Handles.DrawWireDisc(Vector3.zero, Vector3.up, i * gridSize);
        }


        // Angular lines
        for (int i = 0; i < angularDivisions; i++)
        {
            float t = i / (float) angularDivisions;
            float angRad = t * TAU; // turns to radians
            float x = Mathf.Cos(angRad);
            float z = Mathf.Sin(angRad);
            Vector3 dir = new Vector3(x, 0f, z);

            Handles.DrawAAPolyLine(Vector2.zero, dir * radiusOuter);
        }
    }

    private void DrawGridCartesian(float gridDrawExtent)
    {
        int lineCount = Mathf.RoundToInt((gridDrawExtent * 2) / gridSize);

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
    }

    private void SnapSelection()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go.transform, "Snap Selected GameObjects");
            go.transform.position = GetSnappedPosition(go.transform.position);
            //;
        }
    }

    private Vector3 GetSnappedPosition(Vector3 originalPosition)
    {
        switch (gridType)
        {
            case GridType.Cartesian:
                return originalPosition.Round(gridSize);
            case GridType.Polar:
                Vector2 vec = new Vector2(originalPosition.x, originalPosition.z);

                // Distance
                float dist = vec.magnitude;
                float distSnapped = dist.Round(gridSize);

                // Angle
                float angRad = Mathf.Atan2(vec.y, vec.x); // 0 to TAU
                float angTurns = angRad / TAU; // 0 to 1
                float angTurnsSnapped = angTurns.Round(1f / angularDivisions);
                float angRadSnapped = angTurnsSnapped * TAU;

                Vector2 dirSnapped = new Vector2(Mathf.Cos(angRadSnapped), Mathf.Sin(angRadSnapped));
                Vector2 vecSnapped = dirSnapped * distSnapped;

                return new Vector3(vecSnapped.x, originalPosition.y, vecSnapped.y);
            default: return default;
        }
    }
}
#endif