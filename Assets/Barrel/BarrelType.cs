using UnityEngine;

[CreateAssetMenu]
public class BarrelType : ScriptableObject
{
    [Range(1f, 8f)] public float radius = 1;
    public Color color = new Color(0.5f, 0.5f, 0.5f);
    public float damage = 10;
}