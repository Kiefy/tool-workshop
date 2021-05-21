using UnityEditor;
using UnityEngine;

public class PropCannon : EditorWindow
{
    [MenuItem("Tools/Prop Cannon")]
    public static void OpenPropCannonWindow() => GetWindow<PropCannon>();

    [Range(0.01f, 10f)]
    public float radius = 2f;

    [Range(4f, 64f)]
    public int spawnCount = 8;

    // Serialized Properties
    private SerializedObject serializedObject;
    private SerializedProperty pRadius;
    private SerializedProperty pSpawnCount;

    private void OnEnable()
    {
        // Serialized Properties
        serializedObject = new SerializedObject(this);
        pRadius = serializedObject.FindProperty("radius");
        pSpawnCount = serializedObject.FindProperty("spawnCount");

        // Load config
        radius = EditorPrefs.GetFloat("PROP_CANNON_RADIUS", 2f);
        spawnCount = EditorPrefs.GetInt("PROP_CANNON_SPAWN_COUNT", 8);

        SceneView.duringSceneGui += DuringSceneViewGUI;
        autoRepaintOnSceneChange = true;
    }

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
        EditorGUILayout.PropertyField(pSpawnCount);

        // Repaint Scene View if changes detected
        if (serializedObject.ApplyModifiedProperties()) { SceneView.RepaintAll(); }
    }

    private void DuringSceneViewGUI(SceneView sceneView)
    {
        // Run once per frame
        if (Event.current.type != EventType.Repaint) return;

        // Get the Camera Transform
        Transform camTf = sceneView.camera.transform;

        // Cast a Ray through Camera
        Ray ray = new Ray(camTf.position, camTf.forward);

        // If Ray hits something
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Draw Scene Handles
            Handles.color = Color.black;
            Handles.DrawAAPolyLine(4f, hit.point, hit.point + hit.normal);
            Handles.DrawWireDisc(hit.point, hit.normal, radius);
            Handles.color = Color.white;

            // Refresh Scene View
            //UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}