Shader "For Those Below/UI/Card Sheen"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _SheenColor ("Sheen Color", Color) = (1, 0.96, 0.83, 1)
        _SheenStrength ("Sheen Strength", Range(0, 1)) = 0.38
        _SheenWidth ("Sheen Width", Range(0.02, 0.6)) = 0.22
        _SheenAngle ("Sweep Angle", Range(-180, 180)) = 25
        _SheenPeriod ("Seconds Between Sweeps", Range(0.5, 10)) = 2.6

        [Header(Dissolve Reveal)]
        _DissolveDuration ("Duration (Seconds)", Range(0, 3)) = 0.85
        _DissolveScale ("Noise Scale", Range(4, 80)) = 24
        _DissolveEdgeWidth ("Edge Width", Range(0, 0.25)) = 0.09
        [HDR] _DissolveEdgeColor ("Edge Color", Color) = (1.6, 0.85, 0.25, 1)
        [ToggleUI] _PreviewDissolve ("Preview Dissolve", Float) = 0
        _DissolvePreviewProgress ("Test Progress", Range(0, 1)) = 0.5

        _CardUVRect ("Sprite UV Rect (Min XY, Size ZW)", Vector) = (0, 0, 1, 1)
        [HideInInspector] _SheenStartTime ("Sheen Start Time", Float) = -1

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex CardVertex
            #pragma fragment CardFragment
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _CardUVRect;
                half4 _SheenColor;
                float _SheenStrength;
                float _SheenWidth;
                float _SheenAngle;
                float _SheenPeriod;
                float _DissolveDuration;
                float _DissolveScale;
                float _DissolveEdgeWidth;
                half4 _DissolveEdgeColor;
                float _PreviewDissolve;
                float _DissolvePreviewProgress;
                float _SheenStartTime;
            CBUFFER_END

            float _CardSheenUnscaledTime;
            half4 _TextureSampleAdd;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float directionPosition : TEXCOORD1;
                float2 cardUV : TEXCOORD2;
                #ifdef UNITY_UI_CLIP_RECT
                float4 mask : TEXCOORD3;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings CardVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color;
                output.uv = input.uv;

                output.cardUV = (input.uv - _CardUVRect.xy) / max(_CardUVRect.zw, 0.0001);
                float2 direction;
                sincos(radians(_SheenAngle), direction.y, direction.x);
                float span = abs(direction.x) + abs(direction.y);
                output.directionPosition = dot(output.cardUV - 0.5, direction) / span + 0.5;

                #ifdef UNITY_UI_CLIP_RECT
                float2 projection = abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float2 pixelSize = abs(output.positionCS.w) / max(projection, 0.0001);
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                output.mask = float4(input.positionOS.xy * 2.0 - rect.xy - rect.zw,
                    0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + pixelSize));
                #endif
                return output;
            }

            float CellNoise(float2 cell)
            {
                float3 hash = frac(float3(cell.xyx) * 0.1031);
                hash += dot(hash, hash.yzx + 33.33);
                return frac((hash.x + hash.y) * hash.z);
            }

            float ValueNoise(float2 samplePosition)
            {
                float2 cell = floor(samplePosition);
                float2 fraction = frac(samplePosition);
                float2 weight = fraction * fraction * (3.0 - 2.0 * fraction);
                float bottom = lerp(CellNoise(cell), CellNoise(cell + float2(1.0, 0.0)), weight.x);
                float top = lerp(CellNoise(cell + float2(0.0, 1.0)), CellNoise(cell + 1.0), weight.x);
                return lerp(bottom, top, weight.y);
            }

            void ApplyDissolve(inout half4 card, float2 cardUV, float progress)
            {
                float2 noiseUV = cardUV * _DissolveScale;
                float noise = lerp(ValueNoise(noiseUV), ValueNoise(noiseUV * 2.13 + 7.0), 0.35);
                float field = lerp(noise, 1.0 - saturate(cardUV.y), 0.4);
                float softness = 0.015;
                float threshold = lerp(1.0, -(_DissolveEdgeWidth + softness), progress);
                float distanceFromEdge = field - threshold;
                float coverage = smoothstep(0.0, softness, distanceFromEdge);
                float edge = 1.0 - smoothstep(softness, softness + max(_DissolveEdgeWidth, 0.0001), distanceFromEdge);

                card.a *= coverage;
                card.rgb = lerp(card.rgb, _DissolveEdgeColor.rgb, edge * _DissolveEdgeColor.a * step(0.001, _DissolveEdgeWidth));
            }

            half4 CardFragment(Varyings input) : SV_Target
            {
                half4 card = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) + _TextureSampleAdd) * input.color;
                float time = _Time.y;
                float revealProgress = 1.0;
                if (_PreviewDissolve > 0.5)
                {
                    revealProgress = saturate(_DissolvePreviewProgress);
                    time = 0.0;
                }
                else if (_SheenStartTime >= 0.0)
                {
                    float elapsed = max(0.0, _CardSheenUnscaledTime - _SheenStartTime);
                    if (elapsed < _DissolveDuration)
                        revealProgress = elapsed / _DissolveDuration;

                    time = max(0.0, elapsed - _DissolveDuration);
                }
                if (revealProgress < 1.0)
                    ApplyDissolve(card, input.cardUV, revealProgress);

                #ifdef UNITY_UI_CLIP_RECT
                float2 clipMask = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                card.a *= clipMask.x * clipMask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(card.a - 0.001);
                #endif

                float phase = frac(time / max(_SheenPeriod, 0.01));
                float halfWidth = max(_SheenWidth * 0.5, 0.0001);
                float center = lerp(-halfWidth, 1.0 + halfWidth, saturate(phase / 0.55));
                half sheen = (1.0 - smoothstep(0.0, halfWidth, abs(input.directionPosition - center)))
                    * _SheenStrength * _SheenColor.a;
                card.rgb += (1.0 - saturate(card.rgb)) * _SheenColor.rgb * sheen;
                card.rgb *= card.a;
                return card;
            }
            ENDHLSL
        }
    }
}
