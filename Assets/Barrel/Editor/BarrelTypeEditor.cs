using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(BarrelType))]
public class BarrelTypeEditor : Editor
{
    private SerializedObject so;

    private SerializedProperty propRadius;
    private SerializedProperty propDamage;
    private SerializedProperty propColor;

    private void OnEnable()
    {
        Undo.undoRedoPerformed += OnUndo;

        so = serializedObject;

        propRadius = so.FindProperty("radius");
        propDamage = so.FindProperty("damage");
        propColor = so.FindProperty("color");
    }

    private static void OnUndo() => BarrelManager.UpdateAllBarrelColors();

    public override void OnInspectorGUI()
    {
        so.Update();

        EditorGUILayout.PropertyField(propRadius);
        EditorGUILayout.PropertyField(propDamage);
        EditorGUILayout.PropertyField(propColor);

        if (so.ApplyModifiedProperties())
        {
            BarrelManager.UpdateAllBarrelColors();
        }
    }
}