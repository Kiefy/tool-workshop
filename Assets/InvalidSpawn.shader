Shader "Unlit/InvalidSpawn"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 w_pos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.w_pos = mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1));
                o.normal = mul((float3x3)UNITY_MATRIX_M, v.normal);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                const float3 dir_to_cam = normalize(_WorldSpaceCameraPos.xyzx - i.w_pos);
                float fresnel = pow(1 - dot(dir_to_cam, normalize(i.normal)), 2);
                fresnel = lerp(0.1, 0.4, fresnel);
                return float4(1, 0, 0, fresnel);
            }
            ENDCG
        }
    }
}