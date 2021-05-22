using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class PropCannon : EditorWindow
{
    // Create menu item
    [MenuItem("Tools/Prop Cannon")]
    public static void OpenPropCannonWindow() => GetWindow<PropCannon>();

    // Brush size
    [Range(0.01f, 50f)]
    public float radius = 2f;

    // Point density
    [Range(1f, 256f)]
    public int spawnCount = 8;

    // Prop prefabs
    public GameObject spawnPrefab = null;

    // Set up Serialized Properties
    private SerializedObject serializedObject;
    private SerializedProperty pRadius;
    private SerializedProperty pSpawnCount;
    private SerializedProperty pSpawnPrefab;

    // A place to store the points
    private Vector2[] randomPoints;

    // When Window is opened
    private void OnEnable()
    {
        // Assign Serialized Properties
        serializedObject = new SerializedObject(this);
        pRadius = serializedObject.FindProperty("radius");
        pSpawnCount = serializedObject.FindProperty("spawnCount");
        pSpawnPrefab = serializedObject.FindProperty("spawnPrefab");

        // Load config
        radius = EditorPrefs.GetFloat("PROP_CANNON_RADIUS", 2f);
        spawnCount = EditorPrefs.GetInt("PROP_CANNON_SPAWN_COUNT", 8);

        GenerateRandomPoints();
        SceneView.duringSceneGui += DuringSceneViewGUI;
    }

    // When the window is closed
    private void OnDisable()
    {
        // Save config
        EditorPrefs.SetFloat("PROP_CANNON_RADIUS", radius);
        EditorPrefs.SetInt("PROP_CANNON_SPAWN_COUNT", spawnCount);

        SceneView.duringSceneGui -= DuringSceneViewGUI;
    }

    private void OnGUI()
    {
        serializedObject.Update();

        // Add Serialized Properties to GUI
        EditorGUILayout.PropertyField(pRadius);
        pRadius.floatValue = pRadius.floatValue.AtLeast(0.1f);
        EditorGUILayout.PropertyField(pSpawnCount);
        pSpawnCount.intValue = pSpawnCount.intValue.AtLeast(1);
        EditorGUILayout.PropertyField(pSpawnPrefab);

        // Repaint Scene View if changes detected
        if (serializedObject.ApplyModifiedProperties())
        {
            GenerateRandomPoints();
            SceneView.RepaintAll();
        }

        // If mouse clicked in editor window, clear focus
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }

    private void DuringSceneViewGUI(SceneView sceneView)
    {
        // See Handles through objects
        Handles.zTest = CompareFunction.Always;

        // Run once per frame
        //if (Event.current.type != EventType.Repaint) return;

        // Get the Camera Transform
        Transform camTf = sceneView.camera.transform;

        // Update Scene View when moving mouse
        if (Event.current.type == EventType.MouseMove) { SceneView.RepaintAll(); }

        // Check if Shift or Ctrl is pressed
        bool holdingShift = (Event.current.modifiers & EventModifiers.Shift) != 0;
        bool holdingCtrl = (Event.current.modifiers & EventModifiers.Control) != 0;

        // Change radius and Spawn Count
        if (Event.current.type == EventType.ScrollWheel)
        {
            // Get roll direction
            float scrollDirection = Mathf.Sign(-Event.current.delta.y);

            if (holdingShift)
            {
                // Apply to radius
                serializedObject.Update();
                pRadius.floatValue *= 1f + scrollDirection * 0.05f;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                // Updates Editor Window
                Repaint();

                // Consume the event. Don't let it fall through
                Event.current.Use();
            }
            else if (holdingCtrl)
            {
                // Apply to spawn count
                serializedObject.Update();
                pSpawnCount.intValue += 1 * (int) scrollDirection;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                GenerateRandomPoints();
                // Updates Editor Window
                Repaint();

                // Consume the event. Don't let it fall through
                Event.current.Use();
            }
        }

        // Draw Gizmo's and Ghost Props
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Setting up tangent space
            Vector3 hNormal = hit.normal;
            Vector3 hTangent = Vector3.Cross(hNormal, camTf.up).normalized;
            Vector3 hBiTangent = Vector3.Cross(hNormal, hTangent);

            Ray GetTangentRay(Vector2 tangentSpacePos)
            {
                Vector3 rayOrigin = hit.point +
                                    (hTangent * tangentSpacePos.x + hBiTangent * tangentSpacePos.y) * radius;
                rayOrigin += hNormal * 2;
                Vector3 rayDirection = -hNormal;
                return new Ray(rayOrigin, rayDirection);
            }

            // Drawing points
            foreach (Vector2 point in randomPoints)
            {
                // Raycast to find a point on surface
                Ray ptRay = GetTangentRay(point);

                // If it hits something
                if (Physics.Raycast(ptRay, out RaycastHit ptHit))
                {
                    // Draw sphere and normal on surface
                    DrawGhostProp(ptHit.point);
                    Handles.DrawAAPolyLine(ptHit.point, ptHit.point + ptHit.normal);
                }
            }

            // Draw Axis handle
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(6f, hit.point, hit.point + hTangent);
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(6f, hit.point, hit.point + hBiTangent);
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(6f, hit.point, hit.point + hNormal);

            // Draw circle adapted to terrain
            const int CIRCLE_DETAIL = 256;
            const float TAU = 6.28318530718f;
            Vector3[] ringPoints = new Vector3[CIRCLE_DETAIL];
            for (int i = 0; i < CIRCLE_DETAIL; i++)
            {
                float t = i / ((float) CIRCLE_DETAIL - 1);
                float angRad = t * TAU;
                Vector2 dir = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
                Ray r = GetTangentRay(dir);
                if (Physics.Raycast(r, out RaycastHit cHit))
                {
                    ringPoints[i] = cHit.point + cHit.normal * 0.02f;
                }
                else
                {
                    ringPoints[i] = r.origin;
                }
            }

            Handles.DrawAAPolyLine(ringPoints);

            // Draw Scene Handles
            // Handles.color = Color.black;
            // Handles.DrawWireDisc(hit.point, hit.normal, radius, 2f);
            // Handles.color = Color.white;
        }

        // Spawn props
        if (holdingCtrl && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            TrySpawnProps();
        }
    }

    private void GenerateRandomPoints()
    {
        randomPoints = new Vector2[spawnCount];
        for (int i = 0; i < spawnCount; i++)
        {
            randomPoints[i] = Random.insideUnitCircle;
        }
    }

    private static void DrawGhostProp(Vector3 pos)
    {
        Quaternion qid = Quaternion.identity;
        const EventType RP = EventType.Repaint;
        Handles.SphereHandleCap(-1, pos, qid, 0.1f, RP);
    }

    private void TrySpawnProps()
    {
        if (spawnPrefab == null) { return; }
    }
}