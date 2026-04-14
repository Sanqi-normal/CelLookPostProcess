// CelLookPostProcess.shader

Shader "Hidden/CelLookPostProcess"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
        
        _StencilRef ("Stencil Reference", Int) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Int) = 8
        
        _UseRampMap ("Use Ramp Map", Float) = 0
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
        int    _KuwaharaRadius;
        
        float  _UseRampMap;
        TEXTURE2D(_RampMap);
        SAMPLER(sampler_RampMap);

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
        float4 _LineColor;
        float  _LineIntensity;
        float  _DepthFalloff;
        
        float  _FinalSaturation;
        float  _FinalContrast;
        float4 _ShadowTint;
        float4 _HighlightTint;
        float  _ShadowInfluence;
        float  _HighlightInfluence;
        
        float  _EnableSilhouette;
        float4 _SilhouetteShadowColor;
        float4 _SilhouetteMidColor;
        float4 _SilhouetteHighColor;
        float  _SilhouetteThreshold1;
        float  _SilhouetteThreshold2;

        float  _EnablePixelate;
        float  _PixelSize;

        float  _EnableRetroCRT;
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
        
        TEXTURE2D_X(_OriginalCameraTex);

        float2 GetPixelatedUV(float2 uv)
        {
            if (_EnablePixelate > 0.5)
            {
                float2 pixelRes = _ScreenParams.xy / max(1.0, _PixelSize);
                return (floor(uv * pixelRes) + 0.5) / pixelRes;
            }
            return uv;
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
        ENDHLSL

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
                // Kuwahara 在原始分辨率上操作，与像素化无关。
                // 像素化（PixelSize）只影响 Uber pass 的颜色采样 UV，
                // 不应在此处混入，否则 Kuwahara 窗口会错位/跨格。
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

        Pass
        {
            Name "UberNPRPass"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUber

            half4 FragUber(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 pixelatedUV = uv;

                if (_EnableRetroCRT > 0.5)
                {
                    uv = uv * 2.0 - 1.0;
                    float2 offset = abs(uv.yx) / float2(_CRTCurve, _CRTCurve);
                    uv = uv + uv * offset * offset;
                    uv = uv * 0.5 + 0.5;
                    pixelatedUV = uv;
                    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return half4(0, 0, 0, 1);
                }

                // 像素化 UV：颜色采样吸附到像素格，描边/深度/法线采样保留原始 uv。
                // 两条路径分开，PixelSize 不会影响边缘检测偏移。
                float2 colorUV = GetPixelatedUV(pixelatedUV);

                half4 col = half4(0, 0, 0, 1);
                if (_EnableRetroCRT > 0.5)
                {
                    float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, colorUV + float2(_ChromaticAberration, 0)).r;
                    float g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, colorUV).g;
                    float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, colorUV - float2(_ChromaticAberration, 0)).b;
                    col = half4(r, g, b, 1.0);
                }
                else
                {
                    col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, colorUV);
                }

                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5 + _Brightness;
                col.rgb = clamp(col.rgb, 0.001, 1.0);

                float rawLuma = dot(col.rgb, float3(0.299, 0.587, 0.114));

                if (_EnableSilhouette > 0.5)
                {
                    float3 silCol = _SilhouetteShadowColor.rgb;
                    silCol = lerp(silCol, _SilhouetteMidColor.rgb, step(_SilhouetteThreshold1, rawLuma));
                    silCol = lerp(silCol, _SilhouetteHighColor.rgb, step(_SilhouetteThreshold2, rawLuma));
                    col.rgb = silCol;
                }
                else
                {
                    if (_UseRampMap > 0.5)
                    {
                        col.rgb *= SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(rawLuma, 0.5)).rgb;
                    }
                    else
                    {
                        float3 hsv = RGBtoHSV(col.rgb);
                        float isShadow = 1.0 - smoothstep(_ShadowThreshold - _ShadowSmoothness, _ShadowThreshold + _ShadowSmoothness, hsv.z);
                        float isLight = 1.0 - isShadow;

                        float3 shadowHSV = hsv;
                        shadowHSV.x = frac(shadowHSV.x + _ShadowHueShift);
                        shadowHSV.y = saturate(shadowHSV.y * (1.0 + _ShadowSatBoost));
                        shadowHSV.z = shadowHSV.z * _ShadowDarken;

                        float3 lightHSV = hsv;
                        lightHSV.y = saturate(lightHSV.y * _Saturation);

                        hsv = shadowHSV * isShadow + lightHSV * isLight;
                        col.rgb = HSVtoRGB(hsv);
                    }
                }

                float lumFinal = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float shadowW = saturate(1.0 - lumFinal * 2.5);
                float highlightW = saturate((lumFinal - 0.6) * 2.5);
                col.rgb += _ShadowTint.rgb * _ShadowInfluence * shadowW;
                col.rgb += _HighlightTint.rgb * _HighlightInfluence * highlightW;
                col.rgb = saturate((col.rgb - 0.5) * _FinalContrast + 0.5);
                
                lumFinal = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = saturate(lerp(float3(lumFinal, lumFinal, lumFinal), col.rgb, _FinalSaturation));

                if (_PatternType > 0)
                {
                    float2 screenAspect = float2(_ScreenParams.x / _ScreenParams.y, 1.0);
                    float2 patternUV = pixelatedUV * screenAspect * _PatternScale * 50.0;
                    patternUV = mul(GetRotationMatrix(_PatternAngle), patternUV);
                    
                    float isShape = 0.0;

                    if (_PatternType == 1)
                    {
                        float2 grid = frac(patternUV) - 0.5;
                        float dist = length(grid);
                        float radius = saturate(1.0 - lumFinal) * 0.7;
                        isShape = 1.0 - step(radius, dist);
                    }
                    else if (_PatternType == 2)
                    {
                        float lineGradient = frac(patternUV.x);
                        float thickness = saturate(1.0 - lumFinal) * 0.8;
                        isShape = 1.0 - step(thickness, lineGradient);
                    }

                    float patternMask = isShape * (1.0 - smoothstep(_PatternLumaThreshold - 0.05, _PatternLumaThreshold + 0.05, lumFinal)) * _PatternIntensity;
                    col.rgb = lerp(col.rgb, _PatternColor.rgb, patternMask);
                }

                if (_LineIntensity > 0.0)
                {
                    float rawDepth = SampleSceneDepth(uv);
                    
                    #if defined(UNITY_REVERSED_Z)
                    bool isSky = rawDepth < 0.000001;
                    #else
                    bool isSky = rawDepth > 0.999999;
                    #endif

                    if (!isSky)
                    {
                        // 描边检测步长始终为 1 个屏幕原始像素，不受 PixelSize 影响。
                        // PixelSize 只控制颜色 UV 的吸附，不应放大梯度采样偏移，
                        // 否则未开启 Kuwahara 时仅调 PixelSize 也会导致描边错位。
                        float2 ts = _LineThickness / _ScreenParams.xy;
                        
                        float depthEdge = RobertsCrossDepth(uv, ts);
                        float normalEdge = RobertsCrossNormal(uv, ts);

                        float edge = saturate(step(_DepthThreshold, depthEdge) + step(_NormalThreshold, normalEdge));
                        float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                        float fade = saturate(1.0 - sceneDepth / max(_DepthFalloff, 0.001));
                        edge *= lerp(1.0, fade, step(0.01, _DepthFalloff));
                        
                        col.rgb = lerp(col.rgb, _LineColor.rgb, edge * _LineIntensity);
                    }
                }

                if (_EnableRetroCRT > 0.5)
                {
                    float scanline = sin(uv.y * _ScanlineCount * 3.14159) * 0.5 + 0.5;
                    col.rgb -= (1.0 - scanline) * _ScanlineIntensity;
                    float2 vUV = uv * (1.0 - uv.yx);
                    float vig = pow(vUV.x * vUV.y * 15.0, _VignetteIntensity);
                    col.rgb *= saturate(vig);
                }

                half4 original = SAMPLE_TEXTURE2D_X(_OriginalCameraTex, sampler_LinearClamp, uv);
                return half4(lerp(original.rgb, col.rgb, _EffectIntensity), 1.0);
            }
            ENDHLSL
        }
    }
}