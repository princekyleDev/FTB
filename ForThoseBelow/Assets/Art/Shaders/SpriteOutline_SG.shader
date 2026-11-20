Shader "For Those Below/Sprite Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        [Enum(Outer, 0, Inner, 1)] _OutlinePosition ("Outline Position", Float) = 0
        _OutlineWidth ("Outline Width (Texels)", Range(0, 4)) = 1
        _GlowIntensity ("Glow Intensity", Range(0, 16)) = 0
        [HideInInspector] _ProximityGlow ("Proximity Glow", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                float4 _OutlineColor;
                float _OutlinePosition;
                float _OutlineWidth;
                float _GlowIntensity;
                float _ProximityGlow;
            CBUFFER_END

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();

                output.positionCS = TransformObjectToHClip(UnityFlipSprite(input.positionOS, unity_SpriteProps.xy));
                output.uv = input.uv;
                output.color = input.color * unity_SpriteColor;
                return output;
            }

            half SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            float4 OutlineFragment(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;
                float2 horizontal = float2(offset.x, 0);
                float2 vertical = float2(0, offset.y);
                float2 otherDiagonal = float2(offset.x, -offset.y);

                half4 positiveSamples = half4(SampleAlpha(input.uv + horizontal), SampleAlpha(input.uv + vertical),
                    SampleAlpha(input.uv + offset), SampleAlpha(input.uv + otherDiagonal));
                half4 negativeSamples = half4(SampleAlpha(input.uv - horizontal), SampleAlpha(input.uv - vertical),
                    SampleAlpha(input.uv - offset), SampleAlpha(input.uv - otherDiagonal));
                bool inner = _OutlinePosition > 0.5;
                half4 edge = inner ? min(positiveSamples, negativeSamples) : max(positiveSamples, negativeSamples);
                half coverage = inner
                    ? sprite.a - min(min(edge.x, edge.y), min(edge.z, edge.w))
                    : max(max(edge.x, edge.y), max(edge.z, edge.w)) - sprite.a;

                half outlineAlpha = saturate(coverage) * _OutlineColor.a;
                // An inner outline replaces part of the sprite's existing coverage.
                half spriteAlpha = inner ? sprite.a - outlineAlpha : sprite.a;
                float3 outlineRgb = _OutlineColor.rgb * outlineAlpha * (1.0 + _GlowIntensity + _ProximityGlow);
                float3 rgb = sprite.rgb * input.color.rgb * spriteAlpha + outlineRgb;
                return float4(rgb, spriteAlpha + outlineAlpha) * input.color.a;
            }
            ENDHLSL
        }
    }
}
