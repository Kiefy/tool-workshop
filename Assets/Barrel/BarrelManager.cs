using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;

#endif

[ExecuteAlways]
public class BarrelManager : MonoBehaviour
{
    public bool showRadius = true;
    public bool showWires = true;
    public bool alwaysVisible;
    [Range(0.01f, 10f)] public float wireThickness = 2.5f;
    [Range(0.01f, 100f)] public float radiusThickness = 10f;

    [FormerlySerializedAs("opacity")] [Range(0f, 1f)]
    public float wireOpacity = 1f;

    [Range(0f, 1f)] public float radiusOpacity = 1f;

    public static readonly List<Barrel> AllTheBarrels = new List<Barrel>();

    public static void UpdateAllBarrelColors()
    {
        foreach (Barrel barrel in AllTheBarrels)
        {
            barrel.TryApplyColor();
        }
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (!showWires && !showRadius) return;

        if (!alwaysVisible) Handles.zTest = CompareFunction.LessEqual;
        Vector3 managerPosition = transform.position;

        foreach (Barrel barrel in AllTheBarrels)
        {
            if (barrel.type == null) continue;

            Vector3 barrelPosition = barrel.transform.position;
            float halfHeight = (managerPosition.y - barrelPosition.y) * 0.5f;
            Vector3 offset = Vector3.up * halfHeight;
            Color color = barrel.type.color;

            if (showWires)
            {
                Handles.DrawBezier(
                    managerPosition,
                    barrelPosition,
                    managerPosition - offset,
                    barrelPosition + offset,
                    color * wireOpacity,
                    EditorGUIUtility.whiteTexture,
                    wireThickness
                );
            }

            if (showRadius)
            {
                Handles.color = color * radiusOpacity;
                Handles.DrawWireDisc(
                    barrelPosition,
                    Vector3.up,
                    barrel.type.radius,
                    radiusThickness
                );
            }
        }
    }

#endif
}