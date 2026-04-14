// CelLookSettings.cs

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CelLookPostProcess
{
    [System.Serializable, VolumeComponentMenu("Custom/Cel Look Post Process")]
    public class CelLookSettings : VolumeComponent, IPostProcessComponent
    {
        [Header("== Global Settings ==")]
        [Tooltip("整体效果强度。")]
        public ClampedFloatParameter effectIntensity = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        [Header("== Stencil Mask ==")]
        [Tooltip("是否启用模板测试屏蔽特定对象（如第一人称武器）。")]
        public BoolParameter enableStencil = new BoolParameter(false);
        [Tooltip("被屏蔽对象的 Stencil ID。")]
        public IntParameter stencilRef = new IntParameter(1);

        [Header("== Pre-Filter ==")]
        [Tooltip("Kuwahara滤波半径。为0时跳过此Pass。")]
        public ClampedIntParameter kuwaharaRadius = new ClampedIntParameter(0, 0, 6);

        [Header("== Color & Luminance Mapping ==")]
        [Tooltip("是否使用Ramp贴图代替数学二分法。")]
        public BoolParameter useRampMap = new BoolParameter(false);
        [Tooltip("一维阶调渐变贴图（Ramp Texture）。")]
        public TextureParameter rampMap = new TextureParameter(null);

        [Tooltip("二分法光影阈值（不使用Ramp时生效）。")]
        public ClampedFloatParameter shadowThreshold = new ClampedFloatParameter(0.5f, 0.1f, 0.9f);
        [Tooltip("二分法边缘平滑度。")]
        public ClampedFloatParameter shadowSmoothness = new ClampedFloatParameter(0.05f, 0.0f, 0.5f);

        [Tooltip("亮部饱和度倍数。")]
        public ClampedFloatParameter saturation = new ClampedFloatParameter(1.4f, 0.5f, 3.0f);
        [Tooltip("对比度。")]
        public ClampedFloatParameter contrast = new ClampedFloatParameter(1.3f, 0.5f, 3.0f);
        [Tooltip("亮度偏移。")]
        public ClampedFloatParameter brightness = new ClampedFloatParameter(0.0f, -0.3f, 0.3f);

        [Tooltip("暗部色相偏移量。")]
        public ClampedFloatParameter shadowHueShift = new ClampedFloatParameter(0.04f, -0.2f, 0.2f);
        [Tooltip("暗部饱和度额外增强。")]
        public ClampedFloatParameter shadowSatBoost = new ClampedFloatParameter(0.3f, 0.0f, 1.5f);
        [Tooltip("暗部亮度压暗系数。")]
        public ClampedFloatParameter shadowDarken = new ClampedFloatParameter(0.6f, 0.1f, 1.0f);

        [Header("== Silhouette Mode ==")]
        public BoolParameter enableSilhouette = new BoolParameter(false);
        public ColorParameter silhouetteShadowColor = new ColorParameter(new Color(0.1f, 0.1f, 0.2f, 1f), true, false, true);
        public ColorParameter silhouetteMidColor = new ColorParameter(new Color(0.4f, 0.2f, 0.5f, 1f), true, false, true);
        public ColorParameter silhouetteHighColor = new ColorParameter(new Color(0.9f, 0.4f, 0.6f, 1f), true, false, true);
        public ClampedFloatParameter silhouetteThreshold1 = new ClampedFloatParameter(0.3f, 0f, 1f);
        public ClampedFloatParameter silhouetteThreshold2 = new ClampedFloatParameter(0.7f, 0f, 1f);

        [Header("== Manga Line Art ==")]
        [Tooltip("线条强度。0时关闭描边。")]
        public ClampedFloatParameter lineIntensity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter lineThickness = new ClampedFloatParameter(1.5f, 0.5f, 5.0f);
        public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(0.01f, 0.0001f, 0.5f);
        public ClampedFloatParameter normalThreshold = new ClampedFloatParameter(0.2f, 0.01f, 1.5f);
        public ColorParameter lineColor = new ColorParameter(new Color(0.05f, 0.05f, 0.08f, 1f), true, false, true);
        public ClampedFloatParameter depthFalloff = new ClampedFloatParameter(50f, 0f, 200f);

        [Header("== Pattern Shading ==")]
        [Tooltip("0: 关闭, 1: 圆点, 2: 排线")]
        public ClampedIntParameter patternType = new ClampedIntParameter(0, 0, 2);
        public ClampedFloatParameter patternScale = new ClampedFloatParameter(10f, 1f, 50f);
        public ClampedFloatParameter patternAngle = new ClampedFloatParameter(0.785398f, 0f, 3.14159f);
        public ClampedFloatParameter patternIntensity = new ClampedFloatParameter(0.8f, 0f, 1f);
        public ColorParameter patternColor = new ColorParameter(new Color(0.1f, 0.1f, 0.1f, 1f), true, false, true);
        [Tooltip("图案显示的亮度阈值（仅在此亮度以下的暗部生成图案）。")]
        public ClampedFloatParameter patternLumaThreshold = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Header("== Color Grading ==")]
        public ClampedFloatParameter finalSaturation = new ClampedFloatParameter(1.1f, 0.0f, 3.0f);
        public ClampedFloatParameter finalContrast = new ClampedFloatParameter(1.1f, 0.5f, 2.0f);
        public ColorParameter shadowTint = new ColorParameter(new Color(0.0f, 0.0f, 0.05f, 1f), true, false, true);
        public ColorParameter highlightTint = new ColorParameter(new Color(0.05f, 0.03f, 0.0f, 1f), true, false, true);
        public ClampedFloatParameter shadowInfluence = new ClampedFloatParameter(0.4f, 0.0f, 1.0f);
        public ClampedFloatParameter highlightInfluence = new ClampedFloatParameter(0.3f, 0.0f, 1.0f);

        [Header("== Screen FX ==")]
        public BoolParameter enablePixelate = new BoolParameter(false);
        public ClampedFloatParameter pixelSize = new ClampedFloatParameter(4.0f, 1.0f, 32.0f);

        public BoolParameter enableRetroCRT = new BoolParameter(false);
        public ClampedFloatParameter crtCurve = new ClampedFloatParameter(3.5f, 1.0f, 10.0f);
        public ClampedFloatParameter chromaticAberration = new ClampedFloatParameter(0.005f, 0.0f, 0.05f);
        public ClampedFloatParameter scanlineCount = new ClampedFloatParameter(600f, 100f, 1500f);
        public ClampedFloatParameter scanlineIntensity = new ClampedFloatParameter(0.3f, 0.0f, 1.0f);
        public ClampedFloatParameter vignetteIntensity = new ClampedFloatParameter(1.5f, 0.0f, 5.0f);

        public bool IsActive() => active && effectIntensity.value > 0.001f;
        public bool IsTileCompatible() => false;
    }
}