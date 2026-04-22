// CelLookPostProcess.shader

Shader "Hidden/CelLookPostProcess"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        
        _StencilRef ("Stencil Reference", Int) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Int) = 8
        
        _RampMap ("Ramp Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Stencil
        {
            Ref [_StencilRef]
            Comp [_StencilComp]
            Pass Keep
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

        float  _EffectIntensity;
        
        // Filter params
        int    _KuwaharaRadius;
        float  _BilateralColorSigma;
        float  _BilateralSpatialSigma;
        
        // Style params
        float  _GlitchFrequency;
        float  _GlitchSpeed;
        float  _GlitchIntensity;
        float  _FilmGrainIntensity;
        TEXTURE2D(_NoiseTex);
        SAMPLER(sampler_NoiseTex);

        TEXTURE2D(_RampMap);
        SAMPLER(sampler_RampMap);

        int    _CelSteps;
        float  _CelStepSmoothness;

        float  _Saturation;
        float  _Contrast;
        float  _Brightness;
        float  _ShadowHueShift;
        float  _ShadowSatBoost;
        float  _ShadowThreshold;
        float  _ShadowSmoothness;
        float  _ShadowDarken;
        
        float  _LineThickness;
        float  _DepthThreshold;
        float  _NormalThreshold;
        float  _ColorThreshold;
        float4 _LineColor;
        float  _LineIntensity;
        float  _LineFadeStart;
        float  _LineFadeEnd;

        float  _FinalSaturation;
        float  _FinalContrast;
        float4 _ShadowTint;
        float  _ShadowInfluence;
        
        float4 _SilhouetteShadowColor;
        float4 _SilhouetteMidColor;
        float4 _SilhouetteHighColor;
        float  _SilhouetteThreshold1;
        float  _SilhouetteThreshold2;

        float  _PixelSize;
        int    _PixelColorCount;
        float  _PixelDitherIntensity;
        float  _CRTCurve;
        float  _ChromaticAberration;
        float  _ScanlineCount;
        float  _ScanlineIntensity;
        float  _VignetteIntensity;

        int    _PatternType;
        float  _PatternScale;
        float  _PatternAngle;
        float  _PatternIntensity;
        float4 _PatternColor;
        float  _PatternLumaThreshold;
        
        float  _LineWiggleIntensity;
        float  _LineWiggleSpeed;

        int    _SpeedLineMode;
        float  _SpeedLineIntensity;
        float  _SpeedLineDensity;
        float  _SpeedLineSpeed;
        float  _SpeedLineWidth;
        float4 _SpeedLineColor;

        TEXTURE2D_X(_OriginalCameraTex);

        float Hash11(float p)
        {
            p = frac(p * .1031);
            p *= p + 33.33;
            p *= p + p;
            return frac(p);
        }

        float2 Hash22(float2 p)
        {
            float3 p3 = frac(float3(p.xyx) * float3(.1031, .1030, .0973));
            p3 += dot(p3, p3.yzx + 33.33);
            return frac((p3.xx + p3.yz) * p3.zy);
        }

        float2 GetLineWiggleUV(float2 uv)
        {
            if (_LineWiggleIntensity <= 0.0) return uv;
            float time = floor(_Time.y * _LineWiggleSpeed);
            // Use much lower frequency noise for coherent wiggle (large grid blocks)
            float2 noise = Hash22(floor(uv * 100.0) / 100.0 + time) - 0.5;
            return uv + noise * _LineWiggleIntensity;
        }

        float GetBayer4x4(float2 uv)
        {
            float2 pixel = floor(uv * _ScreenParams.xy / max(1.0, _PixelSize));
            int x = int(pixel.x) % 4;
            int y = int(pixel.y) % 4;
            int index = x + y * 4;
            float bayer[16] = {
                0.0 / 16.0, 8.0 / 16.0, 2.0 / 16.0, 10.0 / 16.0,
                12.0 / 16.0, 4.0 / 16.0, 14.0 / 16.0, 6.0 / 16.0,
                3.0 / 16.0, 11.0 / 16.0, 1.0 / 16.0, 9.0 / 16.0,
                15.0 / 16.0, 7.0 / 16.0, 13.0 / 16.0, 5.0 / 16.0
            };
            return bayer[index];
        }

        float2 GetPixelatedUV(float2 uv)
        {
            #if defined(_ENABLE_PIXELATE)
            float2 pixelRes = _ScreenParams.xy / max(1.0, _PixelSize);
            // Use floor for hard edges
            return floor(uv * pixelRes) / pixelRes;
            #else
            return uv;
            #endif
        }

        float3 RGBtoHSV(float3 rgb)
        {
            float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
            float4 p = lerp(float4(rgb.bg, K.wz), float4(rgb.gb, K.xy), step(rgb.b, rgb.g));
            float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));
            float d = q.x - min(q.w, q.y);
            float e = 1.0e-10;
            return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
        }

        float3 HSVtoRGB(float3 hsv)
        {
            float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
            float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
            return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
        }

        float2x2 GetRotationMatrix(float angle)
        {
            float s = sin(angle);
            float c = cos(angle);
            return float2x2(c, -s, s, c);
        }

        float RobertsCrossDepth(float2 uv, float2 ts)
        {
            float d00 = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            float d11 = LinearEyeDepth(SampleSceneDepth(uv + ts), _ZBufferParams);
            float d10 = LinearEyeDepth(SampleSceneDepth(uv + float2(ts.x, 0)), _ZBufferParams);
            float d01 = LinearEyeDepth(SampleSceneDepth(uv + float2(0, ts.y)), _ZBufferParams);
            
            float baseDepth = max(d00, 0.001);
            float diff1 = abs(d11 - d00) / baseDepth;
            float diff2 = abs(d10 - d01) / baseDepth;
            
            return sqrt(diff1 * diff1 + diff2 * diff2);
        }

        float RobertsCrossNormal(float2 uv, float2 ts)
        {
            float3 n00 = SampleSceneNormals(uv);
            float3 n11 = SampleSceneNormals(uv + ts);
            float3 n10 = SampleSceneNormals(uv + float2(ts.x, 0));
            float3 n01 = SampleSceneNormals(uv + float2(0, ts.y));
            float3 g0 = n11 - n00;
            float3 g1 = n10 - n01;
            return sqrt(dot(g0, g0) + dot(g1, g1));
        }

        float RobertsCrossLuma(float2 uv, float2 ts)
        {
            float l00 = dot(SAMPLE_TEXTURE2D_X(_OriginalCameraTex, sampler_LinearClamp, uv).rgb, float3(0.299, 0.587, 0.114));
            float l11 = dot(SAMPLE_TEXTURE2D_X(_OriginalCameraTex, sampler_LinearClamp, uv + ts).rgb, float3(0.299, 0.587, 0.114));
            float l10 = dot(SAMPLE_TEXTURE2D_X(_OriginalCameraTex, sampler_LinearClamp, uv + float2(ts.x, 0)).rgb, float3(0.299, 0.587, 0.114));
            float l01 = dot(SAMPLE_TEXTURE2D_X(_OriginalCameraTex, sampler_LinearClamp, uv + float2(0, ts.y)).rgb, float3(0.299, 0.587, 0.114));
            float d1 = l11 - l00;
            float d2 = l10 - l01;
            return sqrt(d1 * d1 + d2 * d2);
        }
        ENDHLSL

        // PASS 0: Kuwahara Filter
        Pass
        {
            Name "FlattenKuwahara"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragKuwahara

            float4 GetMeanAndVariance(float2 uv, float2 texelSize, int2 offset, int radius) 
            {
                float3 mean = 0.0;
                float3 m2 = 0.0;
                int count = 0;
                for(int x = 0; x <= radius; x++) 
                {
                    for(int y = 0; y <= radius; y++) 
                    {
                        float2 sampleUV = uv + float2(x * offset.x, y * offset.y) * texelSize;
                        float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUV).rgb;
                        mean += col;
                        m2 += col * col;
                        count++;
                    }
                }
                mean /= count;
                m2 /= count;
                float variance = dot(abs(m2 - mean * mean), float3(1.0, 1.0, 1.0));
                return float4(mean, variance);
            }

            half4 FragKuwahara(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                if (_KuwaharaRadius <= 0) 
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                
                float2 ts = 1.0 / _ScreenParams.xy;
                int r = _KuwaharaRadius;
                
                float4 m0 = GetMeanAndVariance(uv, ts, int2(-1, -1), r);
                float4 m1 = GetMeanAndVariance(uv, ts, int2( 1, -1), r);
                float4 m2 = GetMeanAndVariance(uv, ts, int2(-1,  1), r);
                float4 m3 = GetMeanAndVariance(uv, ts, int2( 1,  1), r);

                float minVar = m0.a;
                float3 finalCol = m0.rgb;
                if(m1.a < minVar) { minVar = m1.a; finalCol = m1.rgb; }
                if(m2.a < minVar) { minVar = m2.a; finalCol = m2.rgb; }
                if(m3.a < minVar) { minVar = m3.a; finalCol = m3.rgb; }

                return half4(finalCol, 1.0);
            }
            ENDHLSL
        }

        // PASS 1: Bilateral Filter (Edge-Preserving Blur)
        Pass
        {
            Name "FlattenBilateral"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBilateral

            half4 FragBilateral(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 ts = 1.0 / _ScreenParams.xy;
                
                int r = 4;
                
                float3 centerCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                float3 num = 0.0;
                float weightSum = 0.0;

                // Max to prevent division by zero errors
                float spatialDenom = 2.0 * max(_BilateralSpatialSigma, 0.001) * max(_BilateralSpatialSigma, 0.001);
                float colorDenom = 2.0 * max(_BilateralColorSigma, 0.001) * max(_BilateralColorSigma, 0.001);

                for(int x = -r; x <= r; x++)
                {
                    for(int y = -r; y <= r; y++)
                    {
                        float2 offset = float2(x, y);
                        float2 sampleUV = uv + offset * ts;
                        float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUV).rgb;
                        
                        float spatialDist2 = dot(offset, offset);
                        float spatialWeight = exp(-spatialDist2 / spatialDenom);
                        
                        float3 colorDiff = col - centerCol;
                        float colorDist2 = dot(colorDiff, colorDiff);
                        float colorWeight = exp(-colorDist2 / colorDenom);
                        
                        float w = spatialWeight * colorWeight;
                        num += col * w;
                        weightSum += w;
                    }
                }
                return half4(num / max(weightSum, 0.001), 1.0);
            }
            ENDHLSL
        }

        // PASS 2: Uber NPR Pass
        Pass
        {
            Name "UberNPRPass"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUber

            #pragma multi_compile_local _ _ENABLE_COLOR_MAPPING
            #pragma multi_compile_local _ _ENABLE_MANGA_LINES
            #pragma multi_compile_local _ _ENABLE_COLOR_GRADING

            #pragma multi_compile_local _ _ENABLE_VAPORWAVE
            #pragma multi_compile_local _ _ENABLE_RETRO_CRT
            #pragma multi_compile_local _ _ENABLE_PIXELATE
            #pragma multi_compile_local _ _ENABLE_SILHOUETTE
            #pragma multi_compile_local _ _ENABLE_SPEED_LINES
            #pragma multi_compile_local _ _USE_RAMP_MAP
            #pragma multi_compile_local _ _ENABLE_COMIC_PATTERN

            half4 FragUber(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 pixelatedUV = uv;

                #if defined(_ENABLE_RETRO_CRT)
                uv = uv * 2.0 - 1.0;
                float2 offset = abs(uv.yx) / max(float2(_CRTCurve, _CRTCurve), 0.001);
                uv = uv + uv * offset * offset;
                uv = uv * 0.5 + 0.5;
                pixelatedUV = uv;
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return half4(0, 0, 0, 1);
                #endif

                float2 colorUV = GetPixelatedUV(pixelatedUV);
                half4 col = half4(0, 0, 0, 1);

                #if defined(_ENABLE_VAPORWAVE)
                float time = _Time.y * _GlitchSpeed;
                float2 glitchUV = colorUV;
                
                float blockY = floor(colorUV.y * 12.0 - time * 0.5); 
                float blockTrigger = frac(sin(dot(float2(blockY, floor(time * 2.0)), float2(12.9898, 78.233))) * 43758.5453);
                float dynamicCA = 0.0;
                float blockThreshold = 1.0 - (_GlitchFrequency * 0.15);

                if (blockTrigger > blockThreshold)
                {
                    float glitchOffset = (frac(sin(blockY * 12.34) * 43758.5453) - 0.5) * 2.0;
                    glitchUV.x += glitchOffset * _GlitchIntensity;
                    dynamicCA = abs(glitchOffset) * _GlitchIntensity * 0.5;
                }
                
                float tearTrigger = frac(sin(dot(float2(floor(colorUV.y * 150.0), floor(time * 5.0)), float2(12.9898, 78.233))) * 43758.5453);
                float tearThreshold = 1.0 - (_GlitchFrequency * 0.05);

                if (tearTrigger > tearThreshold)
                {
                    glitchUV.x += (frac(sin(colorUV.y * 50.0) * 43758.5453) - 0.5) * _GlitchIntensity * 0.5;
                }

                float totalCA = 0.0 + dynamicCA;
                #if defined(_ENABLE_RETRO_CRT)
                totalCA += _ChromaticAberration;
                #endif
                
                if (totalCA > 0.0001)
                {
                    float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, glitchUV + float2(totalCA, 0)).r;
                    float g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, glitchUV).g;
                    float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, glitchUV - float2(totalCA, 0)).b;
                    col = half4(r, g, b, 1.0);
                }
                else
                {
                    col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, glitchUV);
                }

                float2 noiseUV = colorUV * 3.0 + float2(frac(time * 1.1), frac(time * 0.8));
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                col.rgb += (noise - 0.5) * _FilmGrainIntensity;

                float3 n = SampleSceneNormals(colorUV);
                float3 vaporColor = float3(1.0, 0.4, 0.8);
                col.rgb += n * vaporColor * 0.05 * _GlitchIntensity;

                #else

                #if defined(_ENABLE_RETRO_CRT)
                float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, colorUV + float2(_ChromaticAberration, 0)).r;
                float g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, colorUV).g;
                float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, colorUV - float2(_ChromaticAberration, 0)).b;
                col = half4(r, g, b, 1.0);
                #else
                col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, colorUV);
                #endif

                #endif

                // --- 1. Color Mapping (Cel Shading / Silhouette) ---
                #if defined(_ENABLE_COLOR_MAPPING)
                
                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5 + _Brightness;
                col.rgb = clamp(col.rgb, 0.001, 1.0);

                float rawLuma = dot(col.rgb, float3(0.299, 0.587, 0.114));

                #if defined(_ENABLE_SILHOUETTE)
                float3 silCol = _SilhouetteShadowColor.rgb;
                silCol = lerp(silCol, _SilhouetteMidColor.rgb, step(_SilhouetteThreshold1, rawLuma));
                silCol = lerp(silCol, _SilhouetteHighColor.rgb, step(_SilhouetteThreshold2, rawLuma));
                col.rgb = silCol;
                #else
                    #if defined(_USE_RAMP_MAP)
                    col.rgb *= SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(rawLuma, 0.5)).rgb;
                    #else
                    
                    float3 hsv = RGBtoHSV(col.rgb);
                    
                    // Fixed Analytical Cel Shading
                    // Formula maps hsv.z from [0, 1] into discrete bins smoothly.
                    float steps = max(1.0, float(_CelSteps));
                    float stepSize = 1.0 / steps;
                    
                    float steppedZ = (floor(hsv.z * steps) + smoothstep(0.5 - _CelStepSmoothness, 0.5 + _CelStepSmoothness, frac(hsv.z * steps))) * stepSize;
                    // Ensure the darkest step is never completely black (unless original is pure black), 
                    // this prevents the screen from going completely dark when steps are low.
                    steppedZ = max(steppedZ, stepSize * 0.5);
                    
                    float isShadow = 1.0 - smoothstep(_ShadowThreshold - _ShadowSmoothness, _ShadowThreshold + _ShadowSmoothness, hsv.z);
                    float isLight = 1.0 - isShadow;

                    float3 shadowHSV = hsv;
                    shadowHSV.x = frac(shadowHSV.x + _ShadowHueShift);
                    shadowHSV.y = saturate(shadowHSV.y * (1.0 + _ShadowSatBoost));
                    shadowHSV.z = steppedZ * _ShadowDarken;

                    float3 lightHSV = hsv;
                    lightHSV.y = saturate(lightHSV.y * _Saturation);
                    lightHSV.z = steppedZ;

                    hsv = shadowHSV * isShadow + lightHSV * isLight;
                    col.rgb = HSVtoRGB(hsv);

                    #endif // RAMP_MAP
                #endif // SILHOUETTE
                
                #endif // ENABLE_COLOR_MAPPING
                
                // Keep rawLuma updated for patterns
                float currentLuma = dot(col.rgb, float3(0.299, 0.587, 0.114));

                // --- 2. Pop Art Pattern Shading ---
                #if defined(_ENABLE_COMIC_PATTERN)
                if (_PatternType > 0)
                {
                    float2 screenAspect = float2(_ScreenParams.x / _ScreenParams.y, 1.0);
                    float2 patternUV = pixelatedUV * screenAspect * _PatternScale * 50.0;
                    patternUV = mul(GetRotationMatrix(_PatternAngle), patternUV);
                    
                    // The darker the pixel, the smaller the color dot should be (revealing more black gaps)
                    float dotRadius = saturate(currentLuma / max(_PatternLumaThreshold, 0.001)) * 0.7 * _PatternIntensity;
                    float isDot = 0.0;
                    
                    if (_PatternType == 1) // Dots
                    {
                        float2 grid = frac(patternUV) - 0.5;
                        isDot = 1.0 - smoothstep(dotRadius - 0.05, dotRadius + 0.05, length(grid));
                    }
                    else if (_PatternType == 2) // Hatching
                    {
                        float lineGradient = abs(frac(patternUV.x) - 0.5) * 2.0;
                        isDot = 1.0 - smoothstep(dotRadius - 0.05, dotRadius + 0.05, lineGradient);
                    }

                    float patternRegion = 1.0 - smoothstep(_PatternLumaThreshold - 0.05, _PatternLumaThreshold + 0.05, currentLuma);
                    
                    if (patternRegion > 0.0) 
                    {
                        // 网点内保留原像素颜色，网点边缘（缝隙）使用 PatternColor（通常为黑色）
                        float3 dotColor = col.rgb;
                        float3 gapColor = _PatternColor.rgb;
                        
                        float3 halftoneCol = lerp(gapColor, dotColor, isDot);
                        col.rgb = lerp(col.rgb, halftoneCol, patternRegion);
                    }
                }
                #endif

                // --- 3. Color Grading ---
                #if defined(_ENABLE_COLOR_GRADING)
                float shadowW = saturate(1.0 - currentLuma * 2.0);
                col.rgb += _ShadowTint.rgb * _ShadowInfluence * shadowW;
                
                col.rgb = saturate((col.rgb - 0.5) * _FinalContrast + 0.5);
                float finalLuma = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = saturate(lerp(float3(finalLuma, finalLuma, finalLuma), col.rgb, _FinalSaturation));
                #endif

                // --- 4. Manga Lines ---
                #if defined(_ENABLE_MANGA_LINES)
                if (_LineIntensity > 0.0)
                {
                    float2 wiggleUV = GetLineWiggleUV(uv);
                    float rawDepth = SampleSceneDepth(wiggleUV);
                    float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                    #if defined(UNITY_REVERSED_Z)
                    bool isSky = rawDepth < 0.000001;
                    #else
                    bool isSky = rawDepth > 0.999999;
                    #endif

                    if (!isSky)
                    {
                        // Calculate distance-based attenuation for intensity and thickness
                        float lineFade = saturate((_LineFadeEnd - sceneDepth) / max(_LineFadeEnd - _LineFadeStart, 0.001));
                        
                        // Scale thickness down with distance to prevent distant objects from becoming blobs of black
                        float thicknessScale = lerp(0.5, 1.0, lineFade);
                        float2 ts = (_LineThickness * thicknessScale) / _ScreenParams.xy;
                        
                        float depthEdge = RobertsCrossDepth(wiggleUV, ts);
                        float normalEdge = RobertsCrossNormal(wiggleUV, ts);
                        float colorEdge = RobertsCrossLuma(wiggleUV, ts);

                        float edge = saturate(step(max(_DepthThreshold, 0.001), depthEdge) + 
                                              step(max(_NormalThreshold, 0.001), normalEdge) + 
                                              step(max(_ColorThreshold, 0.001), colorEdge));
                        
                        col.rgb = lerp(col.rgb, _LineColor.rgb, edge * _LineIntensity * lineFade);
                    }
                }
                #endif

                // --- 5. Speed Lines ---
                #if defined(_ENABLE_SPEED_LINES)
                float speedLines = 0.0;
                float2 screenCenter = 0.5;
                float2 speedUV = uv;
                float animTime = _Time.y * _SpeedLineSpeed;
                
                if (_SpeedLineMode == 0) // Horizontal
                {
                    float row = floor(speedUV.y * _SpeedLineDensity);
                    float noise = Hash11(row + floor(animTime));
                    // Add movement along X
                    float colNoise = Hash11(row + floor(speedUV.x * 2.0 - animTime * 0.5));
                    if (noise > 0.4 && colNoise > 0.5)
                    {
                        float lineVal = smoothstep(_SpeedLineWidth, _SpeedLineWidth - 0.1, abs(frac(speedUV.y * _SpeedLineDensity) - 0.5));
                        speedLines = lineVal;
                    }
                }
                else if (_SpeedLineMode == 1) // Vertical
                {
                    float col = floor(speedUV.x * _SpeedLineDensity);
                    float noise = Hash11(col + floor(animTime));
                    // Add movement along Y
                    float rowNoise = Hash11(col + floor(speedUV.y * 2.0 - animTime * 0.5));
                    if (noise > 0.4 && rowNoise > 0.5)
                    {
                        float lineVal = smoothstep(_SpeedLineWidth, _SpeedLineWidth - 0.1, abs(frac(speedUV.x * _SpeedLineDensity) - 0.5));
                        speedLines = lineVal;
                    }
                }
                else // Radial
                {
                    float2 dir = speedUV - screenCenter;
                    float dist = length(dir);
                    float angle = atan2(dir.y, dir.x);
                    float slice = floor((angle / 6.283185 + 0.5) * _SpeedLineDensity);
                    float noise = Hash11(slice + floor(animTime));
                    // Add movement outward
                    float distNoise = Hash11(slice + floor(dist * 5.0 - animTime * 0.5));
                    if (noise > 0.4 && distNoise > 0.5)
                    {
                        float lineVal = smoothstep(_SpeedLineWidth, _SpeedLineWidth - 0.1, abs(frac((angle / 6.283185 + 0.5) * _SpeedLineDensity) - 0.5));
                        speedLines = lineVal * smoothstep(0.1, 0.4, dist);
                    }
                }
                col.rgb = lerp(col.rgb, _SpeedLineColor.rgb, speedLines * _SpeedLineIntensity);
                #endif

                // --- 6. Retro CRT Finish ---
                #if defined(_ENABLE_RETRO_CRT)
                float scanline = sin(uv.y * _ScanlineCount * 3.14159) * 0.5 + 0.5;
                col.rgb -= (1.0 - scanline) * _ScanlineIntensity;
                float2 vUV = uv * (1.0 - uv.yx);
                float vig = pow(vUV.x * vUV.y * 15.0, _VignetteIntensity);
                col.rgb *= saturate(vig);
                #endif

                // --- 7. Pixel Art Post-Process (Quantization & Dithering) ---
                #if defined(_ENABLE_PIXELATE)
                float dither = GetBayer4x4(pixelatedUV);
                float3 colWithDither = col.rgb + (dither - 0.5) * _PixelDitherIntensity;

                // Color Quantization
                float levels = max(2.0, float(_PixelColorCount));
                col.rgb = floor(colWithDither * (levels - 1.0) + 0.5) / (levels - 1.0);
                #endif

                half4 original = SAMPLE_TEXTURE2D_X(_OriginalCameraTex, sampler_LinearClamp, input.texcoord);
                return half4(lerp(original.rgb, col.rgb, _EffectIntensity), 1.0);
            }
            ENDHLSL
        }
    }
}