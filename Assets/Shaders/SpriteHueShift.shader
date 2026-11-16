// Hue rotation, saturation and value scale for sprites (Road-Eater variants): keeps the painted detail of the art and only shifts its colour. One material per variant, no per-frame allocation.
Shader "RoadBuilder/SpriteHueShift"
{
    Properties { _MainTex ("Sprite", 2D) = "white" {} _Hue ("Hue degrees", Float) = 0 _Sat ("Saturation scale", Float) = 1 _Val ("Value scale", Float) = 1 }
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
                float4 _MainTex_ST; half _Hue; half _Sat; half _Val;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            Varyings vert(Attributes i) { Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; o.color = i.color; return o; }
            half4 frag(Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 c = t.rgb;
                // YIQ rotation: hue turns the chroma plane, luma is kept so shading and detail survive
                half3 yiq = half3(dot(c, half3(0.299, 0.587, 0.114)), dot(c, half3(0.596, -0.274, -0.322)), dot(c, half3(0.211, -0.523, 0.312)));
                half a = radians(_Hue); half cs = cos(a), sn = sin(a);
                half2 iq = half2(yiq.y * cs - yiq.z * sn, yiq.y * sn + yiq.z * cs) * _Sat;
                half3 o = half3(yiq.x + 0.956 * iq.x + 0.621 * iq.y, yiq.x - 0.272 * iq.x - 0.647 * iq.y, yiq.x - 1.106 * iq.x + 1.703 * iq.y);
                o = saturate(o) * _Val;
                return half4(o * i.color.rgb, t.a * i.color.a);
            }
            ENDHLSL
        }
    }
}
