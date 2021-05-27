using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// A place to store random placement data
public struct SpawnData
{
    public Vector2 pointInDisc;
    public float randAngleDeg;
    public GameObject prefab;

    public void SetRandomValues(List<GameObject> prefabs)
    {
        pointInDisc = Random.insideUnitCircle;
        randAngleDeg = Random.value * 360;
        if (prefabs != null)
        {
            prefab = prefabs.Count == 0 ? default : prefabs[Random.Range(0, prefabs.Count)];
        }
    }
}

public class SpawnPoint
{
    public SpawnData spawnData;
    public Vector3 position;
    public Quaternion rotation;
    public readonly bool isValid;

    public Vector3 up => rotation * Vector3.up;

    public SpawnPoint(Vector3 position, Quaternion rotation, SpawnData spawnData)
    {
        this.spawnData = spawnData;
        this.position = position;
        this.rotation = rotation;

        // Check if this mesh can be placed/fit the current location
        SpawnablePrefab spawnablePrefab = null;
        if (spawnData.prefab != null)
        {
            spawnablePrefab = spawnData.prefab.GetComponent<SpawnablePrefab>();
        }

        if (spawnablePrefab == null)
        {
            isValid = true;
        }
        else
        {
            float h = spawnablePrefab.height;
            Ray ray = new Ray(position, up);
            isValid = Physics.Raycast(ray, h) == false;
        }
    }
}

public class PropCannon : EditorWindow
{
    [MenuItem("Tools/Prop Cannon")] public static void OpenPropCannonWindow() => GetWindow<PropCannon>();

    [Range(0.01f, 50f)] public float radius = 2f;
    [Range(1f, 256f)] public int spawnCount = 8;

    private SerializedObject so;
    private SerializedProperty propRadius;
    private SerializedProperty propSpawnCount;
    private SerializedProperty propSpawnPrefab;
    private SerializedProperty propPreviewMaterial;

    private SpawnData[] spawnDataPoints;
    private GameObject[] prefabs;
    public List<GameObject> spawnPrefabs = new List<GameObject>();

    private Material materialInvalid;

    [SerializeField] public bool[] prefabSelectionStates;

    private void OnEnable()
    {
        so = new SerializedObject(this);
        propRadius = so.FindProperty("radius");
        propSpawnCount = so.FindProperty("spawnCount");
        SceneView.duringSceneGui += DuringSceneViewGUI;

        Shader sh = Shader.Find("Unlit/InvalidSpawn");
        materialInvalid = new Material(sh);

        // Load config
        radius = EditorPrefs.GetFloat("PROP_CANNON_RADIUS", 2f);
        spawnCount = EditorPrefs.GetInt("PROP_CANNON_SPAWN_COUNT", 8);

        // Load Prefabs
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] {"Assets/Prefabs"});
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
        if (prefabSelectionStates == null || prefabSelectionStates.Length != prefabs.Length)
        {
            prefabSelectionStates = new bool[prefabs.Length];
        }

        GenerateRandomPoints();
    }

    // When the window is closed
    private void OnDisable()
    {
        // Save config
        EditorPrefs.SetFloat("PROP_CANNON_RADIUS", radius);
        EditorPrefs.SetInt("PROP_CANNON_SPAWN_COUNT", spawnCount);

        SceneView.duringSceneGui -= DuringSceneViewGUI;

        DestroyImmediate(materialInvalid);
    }

    private void GenerateRandomPoints()
    {
        spawnDataPoints = new SpawnData[spawnCount];
        for (int i = 0; i < spawnCount; i++)
        {
            spawnDataPoints[i].SetRandomValues(spawnPrefabs);
        }
    }

    private void OnGUI()
    {
        // See Handles through objects
        Handles.zTest = CompareFunction.Always;

        so.Update();
        EditorGUILayout.PropertyField(propRadius);
        propRadius.floatValue = propRadius.floatValue.AtLeast(0.1f);
        EditorGUILayout.PropertyField(propSpawnCount);
        propSpawnCount.intValue = propSpawnCount.intValue.AtLeast(1);

        if (so.ApplyModifiedProperties())
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

    private void TrySpawnObjects(IEnumerable<SpawnPoint> spawnPoints)
    {
        if (spawnPrefabs.Count == 0)
        {
            return;
        }

        foreach (SpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint.isValid == false)
            {
                continue;
            }

            // Spawn prefab
            GameObject spawnedThing = (GameObject) PrefabUtility.InstantiatePrefab(spawnPoint.spawnData.prefab);
            Undo.RegisterCreatedObjectUndo(spawnedThing, "Spawn Props");
            spawnedThing.transform.position = spawnPoint.position;
            spawnedThing.transform.rotation = spawnPoint.rotation;
        }

        GenerateRandomPoints();
    }

    private static bool TryRaycastFromCamera(Vector2 cameraUp, out Matrix4x4 tangentToWorldMatrix)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Setting up tangent space
            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hitNormal, cameraUp).normalized;
            Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);
            tangentToWorldMatrix = Matrix4x4.TRS(
                hit.point,
                Quaternion.LookRotation(hitNormal, hitBitangent),
                Vector3.one
            );
            return true;
        }

        tangentToWorldMatrix = default;
        return false;
    }

    private void DuringSceneViewGUI(SceneView sceneView)
    {
        PrefabSelectorGUI();

        Handles.zTest = CompareFunction.LessEqual;
        Transform camTf = sceneView.camera.transform;

        // Make sure it repaints on mouse move
        if (Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        // Change radius
        bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
        if (Event.current.type == EventType.ScrollWheel && holdingAlt == false)
        {
            float scrollDir = Mathf.Sign(Event.current.delta.y);
            so.Update();
            propRadius.floatValue *= 1f + scrollDir * 0.05f;
            so.ApplyModifiedProperties();
            Repaint(); // updates editor window
            Event.current.Use(); // consume the event, don't let it fall through
        }

        // If the cursor is pointing on valid ground
        if (TryRaycastFromCamera(camTf.up, out Matrix4x4 tangentToWorldMatrix))
        {
            List<SpawnPoint> spawnPoints = GetSpawnPoints(tangentToWorldMatrix);

            if (Event.current.type == EventType.Repaint)
            {
                DrawCircleRegion(tangentToWorldMatrix);
                DrawSpawnPreviews(spawnPoints, sceneView.camera);
            }

            // Spawn on press
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
            {
                TrySpawnObjects(spawnPoints);
            }
        }
    }

    private void PrefabSelectorGUI()
    {
        // button on top of the scene view to select prefabs
        Handles.BeginGUI();
        Rect rect = new Rect(8f, 8f, 64f, 64f);
        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            Texture2D icon = AssetPreview.GetAssetPreview(prefab);

            EditorGUI.BeginChangeCheck();
            prefabSelectionStates[i] = GUI.Toggle(rect, prefabSelectionStates[i], new GUIContent(icon));
            if (EditorGUI.EndChangeCheck())
            {
                // Update selection list
                // if (spawnPrefabs != null)
                // {
                // TODO: Fix me!
                spawnPrefabs.Clear();
                for (int j = 0; j < prefabs.Length; j++)
                {
                    if (prefabSelectionStates[j])
                    {
                        spawnPrefabs.Add(prefabs[j]);
                    }
                }
                // }

                GenerateRandomPoints();
            }

            rect.y += rect.height + 2f;
        }

        Handles.EndGUI();
    }

    private void DrawSpawnPreviews(IEnumerable<SpawnPoint> spawnPoints, Camera cam)
    {
        foreach (SpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint.spawnData.prefab != null)
            {
                // draw preview of all meshes in the prefab
                Matrix4x4 poseToWorld = Matrix4x4.TRS(spawnPoint.position, spawnPoint.rotation, Vector3.one);
                DrawPrefab(spawnPoint.spawnData.prefab, poseToWorld, cam, spawnPoint.isValid);
            }
            else
            {
                // prefab missing, draw sphere and normal on surface instead
                Handles.SphereHandleCap(-1, spawnPoint.position, Quaternion.identity, 0.1f, EventType.Repaint);
                Handles.DrawAAPolyLine(spawnPoint.position, spawnPoint.position + spawnPoint.up);
            }
        }
    }

    private void DrawPrefab(GameObject prefab, Matrix4x4 poseToWorld, Camera cam, bool valid)
    {
        MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter filter in filters)
        {
            Matrix4x4 childToPose = filter.transform.localToWorldMatrix;
            Matrix4x4 childToWorldMatrix = poseToWorld * childToPose;
            Mesh mesh = filter.sharedMesh;

            Material mat = valid ? filter.GetComponent<MeshRenderer>().sharedMaterial : materialInvalid;
            Graphics.DrawMesh(mesh, childToWorldMatrix, mat, 0, cam);
        }
    }

    private List<SpawnPoint> GetSpawnPoints(Matrix4x4 tangentToWorld)
    {
        List<SpawnPoint> hitSpawnPoints = new List<SpawnPoint>();
        foreach (SpawnData rndDataPoint in spawnDataPoints)
        {
            // Create ray for this point
            Ray ptRay = GetCircleRay(tangentToWorld, rndDataPoint.pointInDisc);
            // Raycast to find point on surface
            if (Physics.Raycast(ptRay, out RaycastHit ptHit))
            {
                // Calculate rotation and assign to pose together with position
                Quaternion randRot = Quaternion.Euler(0f, 0f, rndDataPoint.randAngleDeg);
                Quaternion rot = Quaternion.LookRotation(ptHit.normal) *
                                 (randRot * Quaternion.Euler(90f, 0f, 0f));
                SpawnPoint spawnPoint = new SpawnPoint(ptHit.point, rot, rndDataPoint);
                hitSpawnPoints.Add(spawnPoint);
            }
        }

        return hitSpawnPoints;
    }

    private Ray GetCircleRay(Matrix4x4 tangentToWorld, Vector2 pointInCircle)
    {
        Vector3 origin = tangentToWorld.MultiplyPoint3x4(Vector3.zero);
        Vector3 normal = tangentToWorld.MultiplyVector(Vector3.forward);
        Vector3 hitBitangent2 = tangentToWorld.MultiplyVector(Vector3.up);

        Vector3 hitTangent = Vector3.Cross(normal, origin + hitBitangent2).normalized;
        Vector3 hitBitangent = Vector3.Cross(normal, hitTangent);
        Vector3 mysteryMath = hitTangent * pointInCircle.x + hitBitangent * pointInCircle.y;

        Vector3 rayOrigin = origin + mysteryMath * radius;
        rayOrigin += normal * 2;

        Vector3 rayDirection = -normal;
        return new Ray(rayOrigin, rayDirection);
    }

    private void DrawCircleRegion(Matrix4x4 localToWorld)
    {
        DrawAxis(localToWorld);

        // Draw circle adapted to terrain
        const int CIRCLE_DETAIL = 256;
        const float TAU = 6.28318530718f;

        Vector3[] ringPoints = new Vector3[CIRCLE_DETAIL];
        for (int i = 0; i < CIRCLE_DETAIL; i++)
        {
            float t = i / ((float) CIRCLE_DETAIL - 1);
            float angRad = t * TAU;
            Vector2 dir = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
            Ray r = GetCircleRay(localToWorld, dir);
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

    private static void DrawAxis(Matrix4x4 localToWorld)
    {
        Vector3 origin = localToWorld.MultiplyPoint3x4(Vector3.zero);
        Vector3 normal = localToWorld.MultiplyVector(Vector3.forward);

        Handles.color = Color.red;
        Handles.DrawAAPolyLine(
            6f,
            origin,
            origin + Vector3.Cross(normal, origin).normalized
        );
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(
            6f,
            origin,
            origin +
            Vector3.Cross(normal, Vector3.Cross(normal, origin).normalized)
        );
        Handles.color = Color.blue;
        Handles.DrawAAPolyLine(6f, origin, origin + normal);
    }
}