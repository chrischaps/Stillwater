Shader "Stillwater/Water2D"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Wave Settings)]
        _WaveSpeed ("Wave Speed", Range(0.1, 5.0)) = 1.0
        _WaveAmplitude ("Wave Amplitude", Range(0.0, 0.05)) = 0.01
        _WaveFrequency ("Wave Frequency", Range(1.0, 20.0)) = 8.0

        [Header(Color Animation)]
        _ColorShiftSpeed ("Color Shift Speed", Range(0.0, 2.0)) = 0.5
        _ColorShiftAmount ("Color Shift Amount", Range(0.0, 0.2)) = 0.05

        [Header(Sparkle Effect)]
        _SparkleIntensity ("Sparkle Intensity", Range(0.0, 1.0)) = 0.3
        _SparkleSpeed ("Sparkle Speed", Range(0.5, 5.0)) = 2.0
        _SparkleDensity ("Sparkle Density", Range(1.0, 20.0)) = 8.0

        [Header(Edge Foam)]
        _FoamColor ("Foam Color", Color) = (0.9, 0.95, 1.0, 1.0)
        _FoamIntensity ("Foam Intensity", Range(0.0, 1.0)) = 0.6
        _FoamNoiseScale ("Foam Noise Scale", Range(1.0, 30.0)) = 12.0
        _FoamSpeed ("Foam Animation Speed", Range(0.1, 3.0)) = 0.8
        _FoamPulseSpeed ("Foam Pulse Speed", Range(0.1, 2.0)) = 0.5
        _FoamPulseAmount ("Foam Pulse Amount", Range(0.0, 0.5)) = 0.2
        _FoamWaveSpeed ("Foam Wave Speed", Range(0.0, 2.0)) = 0.3

        [Header(Shore Detection)]
        _ShoreMaskTex ("Shore Mask (Generated)", 2D) = "white" {}
        _ShoreMaskBoundsMin ("Shore Bounds Min", Vector) = (0,0,0,0)
        _ShoreMaskBoundsSize ("Shore Bounds Size", Vector) = (10,10,1,0)
        _UseShoreDetection ("Use Shore Detection", Float) = 0
        [Toggle] _DebugShoreMask ("Debug: Show Shore Mask", Float) = 0
        [Toggle] _DebugForceAllFoam ("Debug: Force Foam Everywhere", Float) = 0

        [Header(Rendering)]
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Water2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ DEBUG_DISPLAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_ShoreMaskTex);
            SAMPLER(sampler_ShoreMaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                half4 _Color;
                half4 _RendererColor;
                float _WaveSpeed;
                float _WaveAmplitude;
                float _WaveFrequency;
                float _ColorShiftSpeed;
                float _ColorShiftAmount;
                float _SparkleIntensity;
                float _SparkleSpeed;
                float _SparkleDensity;
                half4 _FoamColor;
                float _FoamIntensity;
                float _FoamNoiseScale;
                float _FoamSpeed;
                float _FoamPulseSpeed;
                float _FoamPulseAmount;
                float _FoamWaveSpeed;
                float4 _ShoreMaskBoundsMin;
                float4 _ShoreMaskBoundsSize;
                float _UseShoreDetection;
                float _DebugShoreMask;
                float _DebugForceAllFoam;
            CBUFFER_END

            // Simple hash function for effects
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // 2D noise function for foam
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Fractal Brownian Motion for natural foam (2 octaves for performance)
            float fbm(float2 p, float time)
            {
                float2 offset = float2(time * 0.1, time * 0.15);
                float value = 0.5 * noise(p + offset);
                value += 0.25 * noise(p * 2.0 + offset * 1.5);
                return value;
            }

            // Sparkle noise function
            float sparkleNoise(float2 uv, float time)
            {
                float2 i = floor(uv * _SparkleDensity);
                float2 f = frac(uv * _SparkleDensity);

                float n = hash(i);
                float sparkle = sin(n * 100.0 + time * _SparkleSpeed) * 0.5 + 0.5;
                sparkle = pow(sparkle, 8.0);

                float2 fade = f * (1.0 - f) * 4.0;
                sparkle *= fade.x * fade.y;

                return sparkle;
            }

            // Sample shore mask based on world position
            float sampleShoreMask(float2 worldPos)
            {
                // Convert world position to UV in shore mask
                float2 uv = (worldPos - _ShoreMaskBoundsMin.xy) / _ShoreMaskBoundsSize.xy;

                // Check if within bounds
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    return 0;

                return SAMPLE_TEXTURE2D(_ShoreMaskTex, sampler_ShoreMaskTex, uv).r;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 worldPos = TransformObjectToWorld(input.positionOS);
                output.worldPos = worldPos.xy;

                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color * _RendererColor;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;

                // Wave distortion on UV coordinates
                float2 waveOffset;
                waveOffset.x = sin(input.worldPos.y * _WaveFrequency + time * _WaveSpeed) * _WaveAmplitude;
                waveOffset.y = cos(input.worldPos.x * _WaveFrequency * 0.8 + time * _WaveSpeed * 0.9) * _WaveAmplitude * 0.5;

                float2 distortedUV = input.uv + waveOffset;

                // Sample the main texture with distorted UVs
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);

                // Apply vertex color and tint
                half4 color = texColor * input.color;

                // Subtle color shift over time
                float colorShift = sin(time * _ColorShiftSpeed + input.worldPos.x * 2.0) * _ColorShiftAmount;
                color.g += colorShift * color.a;
                color.b += colorShift * 0.5 * color.a;

                // Add sparkle effect
                float sparkle = sparkleNoise(input.worldPos, time) * _SparkleIntensity;
                color.rgb += sparkle * color.a;

                // === EDGE FOAM FROM SHORE MASK ===
                float edgeFactor = 0.0;

                if (_DebugForceAllFoam > 0.5)
                {
                    // Debug: show foam everywhere to test foam rendering
                    edgeFactor = 0.5;
                }
                else if (_UseShoreDetection > 0.5)
                {
                    // Sample the shore mask generated by WaterShoreDetector
                    edgeFactor = sampleShoreMask(input.worldPos);
                }

                // === ANIMATED FOAM ===

                // Pulsing effect - foam breathes in and out
                float pulse = sin(time * _FoamPulseSpeed * 3.14159) * 0.5 + 0.5;
                float pulseOffset = pulse * _FoamPulseAmount;

                // Wave effect - foam moves along the shore
                float wavePhase = (input.worldPos.x + input.worldPos.y) * 0.5;
                float shoreWave = sin(wavePhase * 2.0 + time * _FoamWaveSpeed * 3.14159) * 0.5 + 0.5;

                // Modulate edge factor with pulse and wave
                float animatedEdge = edgeFactor;
                animatedEdge = animatedEdge * (1.0 - _FoamPulseAmount) + animatedEdge * pulseOffset * 2.0;
                animatedEdge *= 0.7 + shoreWave * 0.3 * _FoamWaveSpeed;

                // Generate animated foam pattern
                float2 foamUV = input.worldPos * _FoamNoiseScale;
                float foamNoise = fbm(foamUV, time * _FoamSpeed);

                // Create foam with noise-modulated edges
                float foamPattern = foamNoise * 0.6 + 0.4;
                float foam = animatedEdge * foamPattern * _FoamIntensity;

                // Add secondary smaller foam detail with movement
                float foamDetail = noise(foamUV * 2.0 + time * _FoamSpeed * 0.5) * 0.3;
                foam += animatedEdge * foamDetail * _FoamIntensity * 0.5;

                // Add subtle foam "lapping" effect - occasional brighter spots
                float lapNoise = noise(foamUV * 0.5 + time * _FoamPulseSpeed);
                float lap = pow(lapNoise, 3.0) * edgeFactor * _FoamPulseAmount * 2.0;
                foam += lap;

                // Blend foam color
                color.rgb = lerp(color.rgb, _FoamColor.rgb * color.a, saturate(foam));

                // Slightly increase brightness at foam edges for highlight
                color.rgb += foam * 0.1 * color.a;

                // Premultiply alpha for correct blending
                color.rgb *= color.a;

                // Debug: visualize shore mask
                if (_DebugShoreMask > 0.5)
                {
                    // Show the raw shore mask value as red
                    float debugValue = sampleShoreMask(input.worldPos);
                    color.rgb = lerp(color.rgb, float3(debugValue, 0, 0), 0.8);
                }

                return color;
            }
            ENDHLSL
        }

        // Fallback pass for non-2D rendering
        Pass
        {
            Name "Sprite Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_ShoreMaskTex);
            SAMPLER(sampler_ShoreMaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                half4 _Color;
                half4 _RendererColor;
                float _WaveSpeed;
                float _WaveAmplitude;
                float _WaveFrequency;
                float _ColorShiftSpeed;
                float _ColorShiftAmount;
                float _SparkleIntensity;
                float _SparkleSpeed;
                float _SparkleDensity;
                half4 _FoamColor;
                float _FoamIntensity;
                float _FoamNoiseScale;
                float _FoamSpeed;
                float _FoamPulseSpeed;
                float _FoamPulseAmount;
                float _FoamWaveSpeed;
                float4 _ShoreMaskBoundsMin;
                float4 _ShoreMaskBoundsSize;
                float _UseShoreDetection;
                float _DebugShoreMask;
                float _DebugForceAllFoam;
            CBUFFER_END

            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p, float time)
            {
                float2 offset = float2(time * 0.1, time * 0.15);
                float value = 0.5 * noise(p + offset);
                value += 0.25 * noise(p * 2.0 + offset * 1.5);
                return value;
            }

            float sparkleNoise(float2 uv, float time)
            {
                float2 i = floor(uv * _SparkleDensity);
                float2 f = frac(uv * _SparkleDensity);
                float n = hash(i);
                float sparkle = sin(n * 100.0 + time * _SparkleSpeed) * 0.5 + 0.5;
                sparkle = pow(sparkle, 8.0);
                float2 fade = f * (1.0 - f) * 4.0;
                sparkle *= fade.x * fade.y;
                return sparkle;
            }

            float sampleShoreMask(float2 worldPos)
            {
                float2 uv = (worldPos - _ShoreMaskBoundsMin.xy) / _ShoreMaskBoundsSize.xy;
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    return 0;
                return SAMPLE_TEXTURE2D(_ShoreMaskTex, sampler_ShoreMaskTex, uv).r;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS);
                output.worldPos = worldPos.xy;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color * _RendererColor;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;

                // Wave distortion on UV coordinates
                float2 waveOffset;
                waveOffset.x = sin(input.worldPos.y * _WaveFrequency + time * _WaveSpeed) * _WaveAmplitude;
                waveOffset.y = cos(input.worldPos.x * _WaveFrequency * 0.8 + time * _WaveSpeed * 0.9) * _WaveAmplitude * 0.5;

                float2 distortedUV = input.uv + waveOffset;

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);
                half4 color = texColor * input.color;

                float colorShift = sin(time * _ColorShiftSpeed + input.worldPos.x * 2.0) * _ColorShiftAmount;
                color.g += colorShift * color.a;
                color.b += colorShift * 0.5 * color.a;

                float sparkle = sparkleNoise(input.worldPos, time) * _SparkleIntensity;
                color.rgb += sparkle * color.a;

                // Edge foam from shore mask
                float edgeFactor = 0.0;
                if (_UseShoreDetection > 0.5)
                {
                    edgeFactor = sampleShoreMask(input.worldPos);
                }

                float2 foamUV = input.worldPos * _FoamNoiseScale;
                float foamNoise = fbm(foamUV, time * _FoamSpeed);
                float foamPattern = foamNoise * 0.6 + 0.4;
                float foam = edgeFactor * foamPattern * _FoamIntensity;
                float foamDetail = noise(foamUV * 2.0 + time * _FoamSpeed * 0.5) * 0.3;
                foam += edgeFactor * foamDetail * _FoamIntensity * 0.5;

                color.rgb = lerp(color.rgb, _FoamColor.rgb * color.a, saturate(foam));
                color.rgb += foam * 0.1 * color.a;

                color.rgb *= color.a;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
