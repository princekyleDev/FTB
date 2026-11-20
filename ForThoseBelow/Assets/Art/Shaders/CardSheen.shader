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
                #ifdef UNITY_UI_CLIP_RECT
                float4 mask : TEXCOORD2;
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

                float2 cardUV = (input.uv - _CardUVRect.xy) / max(_CardUVRect.zw, 0.0001);
                float2 direction;
                sincos(radians(_SheenAngle), direction.y, direction.x);
                float span = abs(direction.x) + abs(direction.y);
                output.directionPosition = dot(cardUV - 0.5, direction) / span + 0.5;

                #ifdef UNITY_UI_CLIP_RECT
                float2 projection = abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float2 pixelSize = abs(output.positionCS.w) / max(projection, 0.0001);
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                output.mask = float4(input.positionOS.xy * 2.0 - rect.xy - rect.zw,
                    0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + pixelSize));
                #endif
                return output;
            }

            half4 CardFragment(Varyings input) : SV_Target
            {
                half4 card = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) + _TextureSampleAdd) * input.color;

                #ifdef UNITY_UI_CLIP_RECT
                float2 clipMask = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                card.a *= clipMask.x * clipMask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(card.a - 0.001);
                #endif

                float time = _SheenStartTime < 0.0 ? _Time.y : max(0.0, _CardSheenUnscaledTime - _SheenStartTime);
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
