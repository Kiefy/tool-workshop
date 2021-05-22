using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class PropCannon : EditorWindow
{
    [MenuItem("Tools/Prop Cannon")]
    public static void OpenPropCannonWindow() => GetWindow<PropCannon>();

    [Range(0.01f, 50f)]
    public float radius = 2f;

    [Range(1f, 256f)]
    public int spawnCount = 8;

    // Serialized Properties
    private SerializedObject serializedObject;
    private SerializedProperty pRadius;
    private SerializedProperty pSpawnCount;

    private Vector2[] randomPoints;

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
        //autoRepaintOnSceneChange = true;
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
        pRadius.floatValue = pRadius.floatValue.AtLeast(0.1f);
        EditorGUILayout.PropertyField(pSpawnCount);
        pSpawnCount.intValue = pSpawnCount.intValue.AtLeast(1);

        // Repaint Scene View if changes detected
        if (serializedObject.ApplyModifiedProperties())
        {
            GenerateRandomPoints();
            SceneView.RepaintAll();
        }

        // If mouse clicked in editor window
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
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
            Handles.zTest = CompareFunction.Always;

            // Setting up tangent space
            Vector3 hNormal = hit.normal;
            Vector3 hTangent = Vector3.Cross(hNormal, camTf.up).normalized;
            Vector3 hBiTangent = Vector3.Cross(hNormal, hTangent);

            // Drawing points
            foreach (Vector2 point in randomPoints)
            {
                // Create ray for this point
                Vector3 rayOrigin = hit.point + (hTangent * point.x + hBiTangent * point.y) * radius;
                rayOrigin += hNormal * 2;
                Vector3 rayDirection = -hNormal;

                // Raycast to find point on surface
                Ray ptRay = new Ray(rayOrigin, rayDirection);
                if (Physics.Raycast(ptRay, out RaycastHit ptHit))
                {
                    // Draw sphere and normal on surface
                    DrawSphere(ptHit.point);
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

            // Draw Scene Handles
            Handles.color = Color.black;
            Handles.DrawWireDisc(hit.point, hit.normal, radius, 2f);
            Handles.color = Color.white;
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

    void DrawSphere(Vector3 pos)
    {
        Quaternion qid = Quaternion.identity;
        const EventType RP = EventType.Repaint;
        Handles.SphereHandleCap(-1, pos, qid, 0.1f, RP);
    }
}