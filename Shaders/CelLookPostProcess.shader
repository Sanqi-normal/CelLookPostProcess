Shader "Hidden/CelLookPostProcess"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
        
        // --- 剪影风格新增属性 ---
        [Toggle] _EnableSilhouette ("Enable Silhouette Mode", Float) = 0
        _SilhouetteShadowColor ("Silhouette Shadow", Color) = (0.1, 0.1, 0.2, 1)
        _SilhouetteMidColor ("Silhouette Midtone", Color) = (0.4, 0.2, 0.5, 1)
        _SilhouetteHighColor ("Silhouette Highlight", Color) = (0.9, 0.4, 0.6, 1)
        _SilhouetteThreshold1 ("Silhouette Threshold 1", Range(0, 1)) = 0.3
        _SilhouetteThreshold2 ("Silhouette Threshold 2", Range(0, 1)) = 0.7

        // --- 像素化风格新增属性 ---
        [Toggle] _EnablePixelate ("Enable Pixelate Mode", Float) = 0
        _PixelSize ("Pixel Block Size", Range(1, 32)) = 4
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float  _EffectIntensity;
        int    _KuwaharaRadius;
        float  _Saturation;
        float  _Contrast;
        float  _Brightness;
        float  _ShadowHueShift;
        float  _ShadowSatBoost;
        float  _ShadowThreshold;
        float  _ShadowDarken;
        float  _LineThickness;
        float  _DepthThreshold;
        float  _NormalThreshold;
        float4 _LineColor;
        float  _LineIntensity;
        float  _DepthFalloff;
        float  _FinalSaturation;
        float  _FinalContrast;
        float4 _ShadowTint;
        float4 _HighlightTint;
        float  _ShadowInfluence;
        float  _HighlightInfluence;
        
        // 剪影模式变量
        float  _EnableSilhouette;
        float4 _SilhouetteShadowColor;
        float4 _SilhouetteMidColor;
        float4 _SilhouetteHighColor;
        float  _SilhouetteThreshold1;
        float  _SilhouetteThreshold2;

        // 像素化模式变量
        float  _EnablePixelate;
        float  _PixelSize;
        
        TEXTURE2D_X(_OriginalCameraTex);

        // ---------- 像素化 UV 坐标处理 ----------
        float2 GetPixelatedUV(float2 uv)
        {
            if (_EnablePixelate > 0.5)
            {
                // 基于屏幕分辨率和设定的像素块大小计算网格分辨率
                float2 pixelRes = _ScreenParams.xy / max(1.0, _PixelSize);
                // 加上 0.5 以确保采样点落在像素块中心，避免 LinearClamp 引起的边缘模糊
                return (floor(uv * pixelRes) + 0.5) / pixelRes;
            }
            return uv;
        }

        // ---------- 颜色空间转换 ----------
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
        ENDHLSL

        // ============================================================
        // Pass 0: Kuwahara Filter (平滑去噪点，保留硬边缘)
        // ============================================================
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
                float2 uv = GetPixelatedUV(input.texcoord);
                
                if (_KuwaharaRadius <= 0) 
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                
                float2 ts = 1.0 / _ScreenParams.xy;
                // 如果开启了像素化，扩大 Kuwahara 采样步长，确保跨像素块比较方差
                if (_EnablePixelate > 0.5) 
                    ts *= max(1.0, _PixelSize);

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

        // ============================================================
        // Pass 1: Strict Two-Tone Binarization / Silhouette Mapping
        // ============================================================
        Pass
        {
            Name "ColorBinarization"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragColorBin

            half4 FragColorBin(Varyings input) : SV_Target
            {
                float2 uv = GetPixelatedUV(input.texcoord);
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                
                // 预处理对比度
                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5 + _Brightness;
                col.rgb = clamp(col.rgb, 0.001, 1.0);

                if (_EnableSilhouette > 0.5)
                {
                    // --- 剪影风格映射逻辑 ---
                    float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                    float3 silCol = _SilhouetteShadowColor.rgb;
                    silCol = lerp(silCol, _SilhouetteMidColor.rgb, step(_SilhouetteThreshold1, lum));
                    silCol = lerp(silCol, _SilhouetteHighColor.rgb, step(_SilhouetteThreshold2, lum));
                    
                    return half4(silCol, 1.0);
                }
                else
                {
                    // --- 原有严格二分逻辑 ---
                    float3 hsv = RGBtoHSV(col.rgb);
                    float isShadow = step(hsv.z, _ShadowThreshold);
                    float isLight = 1.0 - isShadow;

                    float3 shadowHSV = hsv;
                    shadowHSV.x = frac(shadowHSV.x + _ShadowHueShift);
                    shadowHSV.y = saturate(shadowHSV.y * (1.0 + _ShadowSatBoost));
                    shadowHSV.z = shadowHSV.z * _ShadowDarken;

                    float3 lightHSV = hsv;
                    lightHSV.y = saturate(lightHSV.y * _Saturation);

                    hsv = shadowHSV * isShadow + lightHSV * isLight;
                    col.rgb = HSVtoRGB(hsv);

                    return half4(col.rgb, 1.0);
                }
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 2: Manga Line Art (附带天空盒剔除)
        // ============================================================
        Pass
        {
            Name "MangaLineArt"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragLineArt

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float RobertsCrossDepth(float2 uv, float2 ts)
            {
                float d00 = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float d11 = LinearEyeDepth(SampleSceneDepth(uv + ts), _ZBufferParams);
                float d10 = LinearEyeDepth(SampleSceneDepth(uv + float2(ts.x, 0)), _ZBufferParams);
                float d01 = LinearEyeDepth(SampleSceneDepth(uv + float2(0, ts.y)), _ZBufferParams);
                return sqrt(pow(d11 - d00, 2) + pow(d10 - d01, 2));
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

            half4 FragLineArt(Varyings input) : SV_Target
            {
                float2 uv = GetPixelatedUV(input.texcoord);
                half4 baseCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float rawDepth = SampleSceneDepth(uv);
                #if defined(UNITY_REVERSED_Z)
                    if (rawDepth < 0.000001) return baseCol;
                #else
                    if (rawDepth > 0.999999) return baseCol;
                #endif

                float2 ts = _LineThickness / _ScreenParams.xy;
                // 如果开启了像素化，同步放大边缘检测采样的跨度，否则线条会细碎断裂
                if (_EnablePixelate > 0.5) 
                    ts *= max(1.0, _PixelSize);

                float depthEdge = RobertsCrossDepth(uv, ts);
                float normalEdge = RobertsCrossNormal(uv, ts);

                float edge = saturate(step(_DepthThreshold, depthEdge) + step(_NormalThreshold, normalEdge));
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float fade = saturate(1.0 - sceneDepth / max(_DepthFalloff, 0.001));
                edge *= lerp(1.0, fade, step(0.01, _DepthFalloff));
                
                return lerp(baseCol, half4(_LineColor.rgb, 1.0), edge * _LineIntensity);
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 3: Final Grading & Intensity Blend
        // ============================================================
        Pass
        {
            Name "PopColorGrading"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragColorGrade

            half4 FragColorGrade(Varyings input) : SV_Target
            {
                float2 uv = GetPixelatedUV(input.texcoord);
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float shadowW = saturate(1.0 - lum * 2.5);
                float highlightW = saturate((lum - 0.6) * 2.5);
                
                col.rgb += _ShadowTint.rgb * _ShadowInfluence * shadowW;
                col.rgb += _HighlightTint.rgb * _HighlightInfluence * highlightW;
                col.rgb = saturate(col.rgb);
                
                col.rgb = saturate((col.rgb - 0.5) * _FinalContrast + 0.5);

                float lumFinal = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = saturate(lerp(float3(lumFinal, lumFinal, lumFinal), col.rgb, _FinalSaturation));

                // 此处混合也使用量化后的 UV，保证不受到高分辨率底图的影响
                half4 original = SAMPLE_TEXTURE2D_X(_OriginalCameraTex, sampler_LinearClamp, uv);
                return half4(lerp(original.rgb, col.rgb, _EffectIntensity), original.a);
            }
            ENDHLSL
        }
    }
}