Shader "Unlit/InstancedCircleShader"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct ParticleData {
                float2 position;
                float2 velocity;
            };
            StructuredBuffer<ParticleData> _Particles;

            float4 _Color;
            float _Radius;

            struct appdata
            {
                float3 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v, uint id : SV_InstanceID)
            {
                v2f o;

                ParticleData data = _Particles[id];

                // build world-space position from unit quad
                float2 p = v.vertex.xy * (_Radius * 2.0) + data.position;

                // go straight to clip space (object-to-world is identity here)
                float4 world = float4(p, 0, 1);
                o.vertex = mul(UNITY_MATRIX_VP, world);

                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // cut a circle from the quad
                float dist = distance(i.uv, float2(0.5, 0.5));
                clip(0.5 - dist);
                return _Color;
            }
            ENDCG
        }
    }
}
