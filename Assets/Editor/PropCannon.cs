using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// A place to store random placement data
public struct RawPlacement
{
    public Vector2 Point2D;
    public float AngleDegrees;

    public void Randomize()
    {
        Point2D = Random.insideUnitCircle;
        AngleDegrees = Random.value * 360;
    }
}

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

    //public Material previewMaterial;

    // Set up Serialized Properties
    private SerializedObject serializedObject;
    private SerializedProperty pRadius;
    private SerializedProperty pSpawnCount;

    //private SerializedProperty pSpawnPrefab;
    //private SerializedProperty pPreviewMaterial;

    private RawPlacement[] rawPlacements;
    private GameObject[] prefabs;
    public List<GameObject> spawnPrefabs;

    [SerializeField]
    public bool[] propSelectionStates;

    private void OnEnable()
    {
        // Assign Serialized Properties
        serializedObject = new SerializedObject(this);
        pRadius = serializedObject.FindProperty("radius");
        pSpawnCount = serializedObject.FindProperty("spawnCount");
        //pSpawnPrefab = serializedObject.FindProperty("spawnPrefab");
        //pPreviewMaterial = serializedObject.FindProperty("previewMaterial");

        // Load config
        radius = EditorPrefs.GetFloat("PROP_CANNON_RADIUS", 2f);
        spawnCount = EditorPrefs.GetInt("PROP_CANNON_SPAWN_COUNT", 8);

        GenerateRandomPoints();
        SceneView.duringSceneGui += DuringSceneViewGUI;

        // Load Prefabs
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] {"Assets/Prefabs"});
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
        if (propSelectionStates == null || propSelectionStates.Length != prefabs.Length)
        {
            propSelectionStates = new bool[prefabs.Length];
        }
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
        // See Handles through objects
        Handles.zTest = CompareFunction.Always;

        serializedObject.Update();

        // Add Serialized Properties to GUI
        EditorGUILayout.PropertyField(pRadius);
        pRadius.floatValue = pRadius.floatValue.AtLeast(0.1f);
        EditorGUILayout.PropertyField(pSpawnCount);
        pSpawnCount.intValue = pSpawnCount.intValue.AtLeast(1);
        //EditorGUILayout.PropertyField(pSpawnPrefab);
        //EditorGUILayout.PropertyField(pPreviewMaterial);

        // Repaint if changes detected
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
        Handles.BeginGUI();

        Rect rect = new Rect(8f, 8f, 64f, 64f);
        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            Texture2D icon = AssetPreview.GetAssetPreview(prefab);

            EditorGUI.BeginChangeCheck();
            propSelectionStates[i] = GUI.Toggle(rect, propSelectionStates[i], new GUIContent(icon));
            if (EditorGUI.EndChangeCheck())
            {
                // Update selection list
                spawnPrefabs.Clear();
                for (int j = 0; j < prefabs.Length; j++)
                {
                    if (propSelectionStates[i])
                    {
                        spawnPrefabs.Add(prefabs[i]);
                    }
                }
            }


            serializedObject.Update();
            //spawnPrefabs[i] = prefab;
            //pSpawnPrefab.objectReferenceValue = spawnPrefabs[i]=prefabs[i];
            serializedObject.ApplyModifiedProperties();
            Repaint();
            rect.y += rect.height + 2f;
        }

        Handles.EndGUI();

        // Update Scene View when moving mouse
        if (Event.current.type == EventType.MouseMove) { SceneView.RepaintAll(); }

        // Check if Shift or Ctrl is pressed
        bool isHoldingShift = (Event.current.modifiers & EventModifiers.Shift) != 0;
        bool isHoldingCtrl = (Event.current.modifiers & EventModifiers.Control) != 0;

        // Change radius and Spawn Count
        if (Event.current.type == EventType.ScrollWheel)
        {
            // Get roll direction
            float scrollDirection = Mathf.Sign(-Event.current.delta.y);

            if (isHoldingShift)
            {
                // Apply to radius
                serializedObject.Update();
                pRadius.floatValue *= 1f + scrollDirection * 0.05f;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                // Update Editor Window
                Repaint();

                // Consume event. Don't let it fall through
                Event.current.Use();
            }
            else if (isHoldingCtrl)
            {
                // Apply to spawn count
                serializedObject.Update();
                pSpawnCount.intValue += 1 * (int) scrollDirection;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                GenerateRandomPoints();
                // Update Editor Window
                Repaint();

                // Consume event. Don't let it fall through
                Event.current.Use();
            }
        }

        RenderProps(sceneView.camera);
    }

    private void RenderProps(Camera cam)
    {
        Vector3 cameraUp = cam.transform.up;

        // Draw Gizmo's and Ghost Props
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(mouseRay, out RaycastHit mouseRaycastHit))
        {
            // Drawing points
            List<Pose> correctedPlacements = new List<Pose>();
            foreach (RawPlacement rawPlacement in rawPlacements)
            {
                // Raycast to find a point on surface. If Ray misses, continue
                Ray tangentRay = GetTangentRay(mouseRaycastHit, cameraUp, rawPlacement.Point2D);
                if (!Physics.Raycast(tangentRay, out RaycastHit placementHit)) continue;

                // Calculate rotation and assign to pose with position
                Quaternion randomRotationZ = Quaternion.Euler(0f, 0f, rawPlacement.AngleDegrees);
                Quaternion placementHitNormalDirection = Quaternion.LookRotation(placementHit.normal);
                Quaternion offsetX = Quaternion.Euler(90f, 0f, 0f);
                Quaternion correctedRotation = placementHitNormalDirection * (randomRotationZ * offsetX);

                Pose correctedPose = new Pose(placementHit.point, correctedRotation);
                correctedPlacements.Add(correctedPose);

                //DrawSpawnPreviews(correctedPlacements, cam);
                DrawPropMarker(placementHit);
                DrawPropPreview(correctedPose);
            }

            if (Event.current.type == EventType.Repaint)
            {
                DrawAxisHandle(mouseRaycastHit, cameraUp);
                DrawLiveCircle(mouseRaycastHit, cameraUp);
            }

            // Spawn props
            bool ctrl = (Event.current.modifiers & EventModifiers.Control) != 0;
            bool lmb = Event.current.type == EventType.MouseDown && Event.current.button == 0;
            if (ctrl && lmb) { PlaceProps(correctedPlacements); }
        }
    }

    void DrawSpawnPreviews(List<Pose> spawnPoses, Camera cam)
    {
        foreach (Pose pose in spawnPoses)
        {
            if (spawnPrefabs != null && spawnPrefabs.Count > 0)
            {
                // draw preview of all meshes in the prefab
                Matrix4x4 poseToWorld = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
                //DrawPrefab(spawnPrefabs[0], poseToWorld, cam);
            }
            else
            {
                // prefab missing, draw sphere and normal on surface instead
                Handles.SphereHandleCap(-1, pose.position, Quaternion.identity, 0.1f, EventType.Repaint);
                Handles.DrawAAPolyLine(pose.position, pose.position + pose.up);
            }
        }
    }

    private void DrawPropPreview(Pose correctedPlacement)
    {
        if (spawnPrefabs == null) return;
        Matrix4x4 targetWorldMatrix = Matrix4x4.TRS(
            correctedPlacement.position,
            correctedPlacement.rotation,
            Vector3.one
        );

        List<MeshFilter> filters = new List<MeshFilter>();
        //MeshFilter[] filters = { };

        foreach (GameObject t in spawnPrefabs)
        {
            filters.AddRange(t.GetComponentsInChildren<MeshFilter>());
        }

        foreach (MeshFilter filter in filters)
        {
            Matrix4x4 propWorldMatrix = filter.transform.localToWorldMatrix;
            Matrix4x4 finalWorldMatrix = targetWorldMatrix * propWorldMatrix;

            Mesh mesh = filter.sharedMesh;
            Material mat = filter.GetComponent<MeshRenderer>().sharedMaterial;
            mat.SetPass(0);
            Graphics.DrawMeshNow(mesh, finalWorldMatrix);
        }
    }

    private void DrawAxisHandle(RaycastHit mouseHit, Vector3 camTfUp)
    {
        Handles.color = Color.red;
        Handles.DrawAAPolyLine(
            6f,
            mouseHit.point,
            mouseHit.point + Vector3.Cross(mouseHit.normal, camTfUp).normalized
        );
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(
            6f,
            mouseHit.point,
            mouseHit.point +
            Vector3.Cross(mouseHit.normal, Vector3.Cross(mouseHit.normal, camTfUp).normalized)
        );
        Handles.color = Color.blue;
        Handles.DrawAAPolyLine(6f, mouseHit.point, mouseHit.point + mouseHit.normal);
    }

    private void DrawLiveCircle(RaycastHit mouseHit, Vector3 camTfUp)
    {
        // Draw circle adapted to terrain
        const int CIRCLE_DETAIL = 256;
        const float TAU = 6.28318530718f;
        Vector3[] ringPoints = new Vector3[CIRCLE_DETAIL];
        for (int i = 0; i < CIRCLE_DETAIL; i++)
        {
            float t = i / ((float) CIRCLE_DETAIL - 1);
            float angRad = t * TAU;
            Vector2 dir = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
            Ray r = GetTangentRay(mouseHit, camTfUp, dir);
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
    }

    private Ray GetTangentRay(RaycastHit mouseHit, Vector3 cameraUp, Vector2 tangentSpacePos)
    {
        Vector3 hitNormal = mouseHit.normal;
        Vector3 hitTangent = Vector3.Cross(hitNormal, cameraUp).normalized;
        Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);
        Vector3 mysteryMath = hitTangent * tangentSpacePos.x + hitBitangent * tangentSpacePos.y;

        Vector3 rayOrigin = mouseHit.point + mysteryMath * radius;
        rayOrigin += hitNormal * 2;

        Vector3 rayDirection = -hitNormal;
        return new Ray(rayOrigin, rayDirection);
    }

    private void GenerateRandomPoints()
    {
        rawPlacements = new RawPlacement[spawnCount];
        for (int i = 0; i < spawnCount; i++)
        {
            rawPlacements[i].Randomize();
        }
    }

    private static void DrawPropMarker(RaycastHit h)
    {
        Handles.SphereHandleCap(-1, h.point, Quaternion.identity, 0.1f, EventType.Repaint);
        Handles.DrawAAPolyLine(h.point, h.point + h.normal);
    }

    private void PlaceProps(IEnumerable<Pose> poses)
    {
        if (spawnPrefabs == null) { return; }

        foreach (Pose pose in poses)
        {
            // Spawn prefab
            GameObject prop = (GameObject) PrefabUtility.InstantiatePrefab(spawnPrefabs[0]);
            Undo.RegisterCreatedObjectUndo(prop, "Spawn Props");
            prop.transform.position = pose.position;
            prop.transform.rotation = pose.rotation;
        }

        GenerateRandomPoints();
    }

    bool TryRaycastFromCamera(Vector2 cameraUp, out Matrix4x4 tangentToWorldMatrix)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Setting up tangent space
            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hitNormal, cameraUp).normalized;
            Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);
            tangentToWorldMatrix = Matrix4x4.TRS(hit.point, Quaternion.LookRotation(hitNormal, hitBitangent), Vector3.one);
            return true;
        }

        tangentToWorldMatrix = default;
        return false;
    }
}