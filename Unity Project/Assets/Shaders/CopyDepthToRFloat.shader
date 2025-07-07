Shader "Hidden/CopyDepthToRFloat"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass
        {
            ZWrite Off
            ZTest  Always
            Cull   Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // -------- vertex: full-screen triangle --------
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f Vert (uint id : SV_VertexID)
            {
                v2f o;
                o.uv  = float2((id << 1) & 2, id & 2);   //  (0,0)(2,0)(0,2)
                o.pos = float4(o.uv * 2.0f - 1.0f, 0.0f, 1.0f);
                return o;
            }

            // -------- fragment: fetch camera depth --------
            Texture2D     _CameraDepthTexture;
            SamplerState  sampler_CameraDepthTexture;

            float Frag (v2f i) : SV_Target       // returns single float → R channel
            {
                float rawDepth = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, i.uv).r;
                return Linear01Depth(rawDepth);  // 0..1 linear depth
            }
            ENDHLSL
        }
    }
    Fallback Off
}
