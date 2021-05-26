using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class Barrel : MonoBehaviour
{
    [FormerlySerializedAs("Type")] public BarrelType type;

    private MaterialPropertyBlock mpb;
    private MaterialPropertyBlock materialBlock => mpb ??= new MaterialPropertyBlock();
    private static readonly int ShaderColor = Shader.PropertyToID("_Color");

    public void TryApplyColor()
    {
        Color color = new Color(0.5f, 0.5f, 0.5f);

        if (type != null) color = type.color;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        materialBlock.SetColor(ShaderColor, color);
        meshRenderer.SetPropertyBlock(materialBlock);
    }

    private void OnEnable() => BarrelManager.AllTheBarrels.Add(this);
    private void OnDisable() => BarrelManager.AllTheBarrels.Remove(this);
    private void OnValidate() => TryApplyColor();

    private void OnDrawGizmosSelected()
    {
        if (type == null) return;

        Gizmos.color = type.color;
        Gizmos.DrawWireSphere(transform.position, type.radius);
    }
}