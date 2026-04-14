// CelLookPreset.cs
using UnityEngine;
using UnityEngine.Rendering;

namespace CelLookPostProcess
{
    [System.Serializable]
    public class CelLookPreset
    {
        public string name = "New Preset";

        public float effectIntensity = 1f;

        // --- 新增：Stencil 模板测试 ---
        public bool enableStencil = false;
        public int stencilRef = 1;

        public int kuwaharaRadius = 2;

        // --- 新增：Ramp 贴图支持 ---
        public bool useRampMap = false;
        public Texture rampMap = null;

        public float saturation = 1.4f;
        public float contrast = 1.3f;
        public float brightness = 0f;

        public float shadowThreshold = 0.5f;
        public float shadowSmoothness = 0.05f; // 新增平滑度
        public float shadowHueShift = 0.04f;
        public float shadowSatBoost = 0.3f;
        public float shadowDarken = 0.6f;

        // 移除了 enableLines，统一由 lineIntensity 控制
        public float lineIntensity = 1f;
        public float lineThickness = 1.5f;
        public float depthThreshold = 0.01f;
        public float normalThreshold = 0.2f;
        public Color lineColor = new Color(0.05f, 0.05f, 0.08f);
        public float depthFalloff = 50f;

        public float finalSaturation = 1.1f;
        public float finalContrast = 1.1f;
        public Color shadowTint = new Color(0f, 0f, 0.05f);
        public Color highlightTint = new Color(0.05f, 0.03f, 0f);
        public float shadowInfluence = 0.4f;
        public float highlightInfluence = 0.3f;

        public bool enableSilhouette = false;
        public Color silhouetteShadowColor = new Color(0.1f, 0.1f, 0.2f);
        public Color silhouetteMidColor = new Color(0.4f, 0.2f, 0.5f);
        public Color silhouetteHighColor = new Color(0.9f, 0.4f, 0.6f);
        public float silhouetteThreshold1 = 0.3f;
        public float silhouetteThreshold2 = 0.7f;

        public bool enablePixelate = false;
        public float pixelSize = 4f;

        // --- Pattern 参数 ---
        public int patternType = 0;
        public float patternScale = 10f;
        public float patternAngle = 0.785398f;
        public float patternIntensity = 0.8f;
        public Color patternColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        public float patternLumaThreshold = 0.5f; // 新增图案控制阈值

        // --- Retro CRT 参数 ---
        public bool enableRetroCRT = false;
        public float crtCurve = 3.5f;
        public float chromaticAberration = 0.005f;
        public float scanlineCount = 600f;
        public float scanlineIntensity = 0.3f;
        public float vignetteIntensity = 1.5f;

        public void ApplyTo(CelLookSettings target)
        {
            target.effectIntensity.Override(effectIntensity);

            target.enableStencil.Override(enableStencil);
            target.stencilRef.Override(stencilRef);

            target.kuwaharaRadius.Override(kuwaharaRadius);

            target.useRampMap.Override(useRampMap);
            target.rampMap.Override(rampMap);

            target.saturation.Override(saturation);
            target.contrast.Override(contrast);
            target.brightness.Override(brightness);
            target.shadowThreshold.Override(shadowThreshold);
            target.shadowSmoothness.Override(shadowSmoothness);
            target.shadowHueShift.Override(shadowHueShift);
            target.shadowSatBoost.Override(shadowSatBoost);
            target.shadowDarken.Override(shadowDarken);

            target.lineIntensity.Override(lineIntensity);
            target.lineThickness.Override(lineThickness);
            target.depthThreshold.Override(depthThreshold);
            target.normalThreshold.Override(normalThreshold);
            target.lineColor.Override(lineColor);
            target.depthFalloff.Override(depthFalloff);

            target.finalSaturation.Override(finalSaturation);
            target.finalContrast.Override(finalContrast);
            target.shadowTint.Override(shadowTint);
            target.highlightTint.Override(highlightTint);
            target.shadowInfluence.Override(shadowInfluence);
            target.highlightInfluence.Override(highlightInfluence);

            target.enableSilhouette.Override(enableSilhouette);
            target.silhouetteShadowColor.Override(silhouetteShadowColor);
            target.silhouetteMidColor.Override(silhouetteMidColor);
            target.silhouetteHighColor.Override(silhouetteHighColor);
            target.silhouetteThreshold1.Override(silhouetteThreshold1);
            target.silhouetteThreshold2.Override(silhouetteThreshold2);

            target.enablePixelate.Override(enablePixelate);
            target.pixelSize.Override(pixelSize);

            target.patternType.Override(patternType);
            target.patternScale.Override(patternScale);
            target.patternAngle.Override(patternAngle);
            target.patternIntensity.Override(patternIntensity);
            target.patternColor.Override(patternColor);
            target.patternLumaThreshold.Override(patternLumaThreshold);

            target.enableRetroCRT.Override(enableRetroCRT);
            target.crtCurve.Override(crtCurve);
            target.chromaticAberration.Override(chromaticAberration);
            target.scanlineCount.Override(scanlineCount);
            target.scanlineIntensity.Override(scanlineIntensity);
            target.vignetteIntensity.Override(vignetteIntensity);
        }

        public void LoadFrom(CelLookSettings source)
        {
            effectIntensity = source.effectIntensity.value;

            enableStencil = source.enableStencil.value;
            stencilRef = source.stencilRef.value;

            kuwaharaRadius = source.kuwaharaRadius.value;

            useRampMap = source.useRampMap.value;
            rampMap = source.rampMap.value;

            saturation = source.saturation.value;
            contrast = source.contrast.value;
            brightness = source.brightness.value;
            shadowThreshold = source.shadowThreshold.value;
            shadowSmoothness = source.shadowSmoothness.value;
            shadowHueShift = source.shadowHueShift.value;
            shadowSatBoost = source.shadowSatBoost.value;
            shadowDarken = source.shadowDarken.value;

            lineIntensity = source.lineIntensity.value;
            lineThickness = source.lineThickness.value;
            depthThreshold = source.depthThreshold.value;
            normalThreshold = source.normalThreshold.value;
            lineColor = source.lineColor.value;
            depthFalloff = source.depthFalloff.value;

            finalSaturation = source.finalSaturation.value;
            finalContrast = source.finalContrast.value;
            shadowTint = source.shadowTint.value;
            highlightTint = source.highlightTint.value;
            shadowInfluence = source.shadowInfluence.value;
            highlightInfluence = source.highlightInfluence.value;

            enableSilhouette = source.enableSilhouette.value;
            silhouetteShadowColor = source.silhouetteShadowColor.value;
            silhouetteMidColor = source.silhouetteMidColor.value;
            silhouetteHighColor = source.silhouetteHighColor.value;
            silhouetteThreshold1 = source.silhouetteThreshold1.value;
            silhouetteThreshold2 = source.silhouetteThreshold2.value;

            enablePixelate = source.enablePixelate.value;
            pixelSize = source.pixelSize.value;

            patternType = source.patternType.value;
            patternScale = source.patternScale.value;
            patternAngle = source.patternAngle.value;
            patternIntensity = source.patternIntensity.value;
            patternColor = source.patternColor.value;
            patternLumaThreshold = source.patternLumaThreshold.value;

            enableRetroCRT = source.enableRetroCRT.value;
            crtCurve = source.crtCurve.value;
            chromaticAberration = source.chromaticAberration.value;
            scanlineCount = source.scanlineCount.value;
            scanlineIntensity = source.scanlineIntensity.value;
            vignetteIntensity = source.vignetteIntensity.value;
        }
    }
}