Shader "Game/Effects/Underground Seal"
{
    Properties
    {
        [HDR] _Color ("Seal Color", Color) = (0.25, 1.2, 1.6, 1)
        _Opacity ("Opacity", Range(0, 1)) = 1
        [Enum(Barrier, 0, Symbol, 1)] _Layer ("Visual Layer", Float) = 0
        _PulseSpeed ("Pulse Speed", Range(0, 3)) = 0.6
        _BorderRotationSpeed ("Border Rotation Speed (Degrees/sec)", Range(-60, 60)) = 14.4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Seal"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Opacity;
                float _Layer;
                float _PulseSpeed;
                float _BorderRotationSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Stroke(float distance, float width)
            {
                float aa = max(fwidth(distance), 0.001);
                return 1 - smoothstep(width, width + aa, abs(distance));
            }

            float Segment(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                return length(p - a - ab * saturate(dot(p - a, ab) / dot(ab, ab)));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2 - 1;
                float time = _Time.y * _PulseSpeed;
                float pulse = 0.87 + 0.13 * sin(time * 2);
                float alpha;
                float light;

                if (_Layer < 0.5)
                {
                    // A quiet translucent surface, with a brighter rim and slow ripples.
                    float edge = 1 - max(abs(p.x), abs(p.y));
                    float rim = exp(-edge * 32);
                    float ripple = pow(saturate(0.5 + 0.5 * sin(p.y * 18 + p.x * 5 - time)), 8);
                    float weave = sin(p.x * 13 + sin(p.y * 7 + time)) * sin(p.y * 11 - time);
                    alpha = (0.13 + 0.23 * rim + 0.055 * ripple + 0.025 * weave)
                        * smoothstep(0, 0.025, edge);
                    light = 0.38 + 0.5 * rim + 0.12 * ripple;
                }
                else
                {
                    float radius = length(p);
                    float angle = atan2(p.y, p.x) - radians(_Time.y * _BorderRotationSpeed);
                    float rings = max(Stroke(radius - 0.86, 0.009), Stroke(radius - 0.79, 0.005));
                    rings = max(rings, Stroke(radius - 0.55, 0.008));

                    // Twelve repeated angular glyphs between the two ring groups.
                    float sector = 6.2831853 / 12;
                    float localAngle = angle - sector * floor(angle / sector + 0.5);
                    float2 glyph = float2(sin(localAngle), cos(localAngle)) * radius;
                    float rune = Stroke(Segment(glyph, float2(0, 0.61), float2(0, 0.73)), 0.007);
                    rune = max(rune, Stroke(Segment(glyph, float2(-0.045, 0.70), float2(0, 0.66)), 0.007));
                    rune = max(rune, Stroke(Segment(glyph, float2(0, 0.66), float2(0.045, 0.70)), 0.007));

                    // Central interlocking diamonds and a small suspended keyhole.
                    float diamond = Stroke((abs(p.x) + abs(p.y) - 0.43) * 0.7071, 0.012);
                    float inner = Stroke((abs(p.x) + abs(p.y) - 0.30) * 0.7071, 0.006);
                    float keyhole = Stroke(length(p - float2(0, 0.065)) - 0.06, 0.012);
                    keyhole = max(keyhole, Stroke(Segment(p, float2(0, 0.005), float2(0, -0.13)), 0.014));
                    float symbol = max(max(rings, rune), max(max(diamond, inner), keyhole));
                    float halo = 0.10 * exp(-abs(radius - 0.86) * 60)
                        + 0.06 * exp(-abs(radius - 0.55) * 60);
                    alpha = saturate(symbol + halo) * pulse;
                    light = 0.8 + 0.2 * symbol;
                }

                return half4(_Color.rgb * light, saturate(alpha * _Opacity * _Color.a));
            }
            ENDHLSL
        }
    }
}
