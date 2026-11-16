// White silhouette overlay for hit flashes (RO-B0): draws the sprite's alpha as white scaled by _Flash. Set on one material instance, no per-frame allocation.
Shader "RoadBuilder/SpriteWhiteFlash"
{
    Properties { _MainTex ("Sprite", 2D) = "white" {} _Flash ("Flash", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; half _Flash;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes i) { Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }
            half4 frag(Varyings i) : SV_Target { half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a * _Flash; return half4(1, 1, 1, a); }
            ENDHLSL
        }
    }
}
