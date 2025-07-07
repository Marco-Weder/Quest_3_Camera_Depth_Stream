Shader "Custom/DepthToMeters"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"

            UNITY_DECLARE_TEX2DARRAY(_EnvironmentDepthTexture);
            float _Near;
            float _Far;
            int _Layer;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float frag(v2f i) : SV_Target
            {
                float depthNorm = UNITY_SAMPLE_TEX2DARRAY(_EnvironmentDepthTexture, float3(i.uv, _Layer)).r;

                if (depthNorm < 1e-6)
                    return 0;

                float linearDepth = (_Far * _Near) / (_Far - depthNorm * (_Far - _Near));
                return linearDepth;
            }

            ENDHLSL
        }
    }
}
