// CelLookSettings.cs

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CelLookPostProcess
{
    public enum PreFilterMode { None = 0, Kuwahara = 1, Bilateral = 2 }
    public enum SpeedLineMode { Horizontal = 0, Vertical = 1, Radial = 2 }

    [System.Serializable]
    public sealed class PreFilterModeParameter : VolumeParameter<PreFilterMode>
    {
        public PreFilterModeParameter(PreFilterMode value, bool overrideState = false) : base(value, overrideState) {}
    }

    [System.Serializable]
    public sealed class SpeedLineModeParameter : VolumeParameter<SpeedLineMode>
    {
        public SpeedLineModeParameter(SpeedLineMode value, bool overrideState = false) : base(value, overrideState) {}
    }

    [System.Serializable, VolumeComponentMenu("Custom/Cel Look Post Process")]
    public class CelLookSettings : VolumeComponent, IPostProcessComponent
    {
        [Header("== Global Settings (全局设置) ==")]
        [Tooltip("Overall effect intensity / 整体效果强度。")]
        public ClampedFloatParameter effectIntensity = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        [Header("== Stencil Mask (模板遮罩) ==")]
        [Tooltip("Enable stencil test to mask specific objects / 是否启用模板测试屏蔽特定对象（如第一人称武器）。")]
        public BoolParameter enableStencil = new BoolParameter(false);
        [Tooltip("Stencil ID of masked objects / 被屏蔽对象的 Stencil ID。")]
        public IntParameter stencilRef = new IntParameter(1);

        [Header("== Pre-Filter (预过滤) ==")]
        [Tooltip("Pre-filter mode: Smooths lighting while preserving edges / 预过滤模式：抹平光影同时保留边缘。")]
        public PreFilterModeParameter preFilterMode = new PreFilterModeParameter(PreFilterMode.None);
        [Tooltip("Kuwahara radius (Kuwahara mode only) / Kuwahara 半径（仅 Kuwahara 模式）。")]
        public ClampedIntParameter kuwaharaRadius = new ClampedIntParameter(2, 1, 6);
        [Tooltip("Bilateral color tolerance (Bilateral mode only) / Bilateral 颜色容差（仅 Bilateral 模式）。")]
        public ClampedFloatParameter bilateralColorSigma = new ClampedFloatParameter(0.1f, 0.01f, 1.0f);
        [Tooltip("Bilateral spatial tolerance (Bilateral mode only) / Bilateral 空间距离容差（仅 Bilateral 模式）。")]
        public ClampedFloatParameter bilateralSpatialSigma = new ClampedFloatParameter(2.0f, 0.1f, 10.0f);

        [Header("== Color & Luminance Mapping (色彩与亮度映射) ==")]
        [Tooltip("Enable Cel Shading mapping / 开启色阶与明暗二分映射。取消勾选将完全关闭此部分效果。")]
        public BoolParameter enableColorMapping = new BoolParameter(true);
        [Tooltip("Use Ramp map instead of analytical cel steps / 是否使用Ramp贴图代替数学色阶法。")]
        public BoolParameter useRampMap = new BoolParameter(false);
        [Tooltip("1D Ramp Texture / 一维阶调渐变贴图（Ramp Texture）。")]
        public TextureParameter rampMap = new TextureParameter(null);

        [Tooltip("Analytical cel steps (effective without Ramp map) / 分析式色阶步数（无Ramp贴图时生效）。")]
        public ClampedIntParameter celSteps = new ClampedIntParameter(3, 1, 10);
        [Tooltip("Cel step smoothness / 色阶边缘平滑度。")]
        public ClampedFloatParameter celStepSmoothness = new ClampedFloatParameter(0.02f, 0.0f, 0.2f);

        [Tooltip("Shadow split threshold / 阴影分割阈值。")]
        public ClampedFloatParameter shadowThreshold = new ClampedFloatParameter(0.5f, 0.1f, 0.9f);
        [Tooltip("Shadow threshold smoothness / 阴影阈值平滑度。")]
        public ClampedFloatParameter shadowSmoothness = new ClampedFloatParameter(0.05f, 0.0f, 0.5f);

        [Tooltip("Highlight saturation multiplier / 亮部饱和度倍数。")]
        public ClampedFloatParameter saturation = new ClampedFloatParameter(1.4f, 0.5f, 3.0f);
        [Tooltip("Contrast / 对比度。")]
        public ClampedFloatParameter contrast = new ClampedFloatParameter(1.3f, 0.5f, 3.0f);
        [Tooltip("Brightness offset / 亮度偏移。")]
        public ClampedFloatParameter brightness = new ClampedFloatParameter(0.0f, -0.3f, 0.3f);

        [Tooltip("Shadow hue shift amount / 暗部色相偏移量。")]
        public ClampedFloatParameter shadowHueShift = new ClampedFloatParameter(0.04f, -0.2f, 0.2f);
        [Tooltip("Shadow extra saturation boost / 暗部饱和度额外增强。")]
        public ClampedFloatParameter shadowSatBoost = new ClampedFloatParameter(0.3f, 0.0f, 1.5f);
        [Tooltip("Shadow darken factor / 暗部亮度压暗系数。")]
        public ClampedFloatParameter shadowDarken = new ClampedFloatParameter(0.6f, 0.1f, 1.0f);

        [Header("== Silhouette Mode (剪影模式) ==")]
        public BoolParameter enableSilhouette = new BoolParameter(false);
        public ColorParameter silhouetteShadowColor = new ColorParameter(new Color(0.1f, 0.1f, 0.2f, 1f), true, false, true);
        public ColorParameter silhouetteMidColor = new ColorParameter(new Color(0.4f, 0.2f, 0.5f, 1f), true, false, true);
        public ColorParameter silhouetteHighColor = new ColorParameter(new Color(0.9f, 0.4f, 0.6f, 1f), true, false, true);
        public ClampedFloatParameter silhouetteThreshold1 = new ClampedFloatParameter(0.3f, 0f, 1f);
        public ClampedFloatParameter silhouetteThreshold2 = new ClampedFloatParameter(0.7f, 0f, 1f);

        [Header("== Manga Line Art (漫画描边) ==")]
        [Tooltip("Enable manga outline rendering / 开启描边渲染。取消勾选将完全关闭此效果。")]
        public BoolParameter enableMangaLines = new BoolParameter(true);
        [Tooltip("Line intensity. 0 disables outline / 线条强度。")]
        public ClampedFloatParameter lineIntensity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter lineThickness = new ClampedFloatParameter(1.5f, 0.5f, 5.0f);
        public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(0.01f, 0.0001f, 0.5f);
        public ClampedFloatParameter normalThreshold = new ClampedFloatParameter(0.2f, 0.01f, 1.5f);
        [Tooltip("Luma color edge detection threshold (catches inner lines) / 亮度颜色边缘检测阈值（抓取内部线条）。")]
        public ClampedFloatParameter colorThreshold = new ClampedFloatParameter(0.2f, 0.01f, 1.0f);
        public ColorParameter lineColor = new ColorParameter(new Color(0.05f, 0.05f, 0.08f, 1f), true, false, true);
        [Tooltip("Distance where lines start to fade / 线条开始淡出的起始距离。")]
        public FloatParameter lineFadeStart = new FloatParameter(5f);
        [Tooltip("Distance where lines completely disappear / 线条完全消失的截止距离。")]
        public FloatParameter lineFadeEnd = new FloatParameter(50f);

        [Tooltip("Line wiggle intensity (boiling effect) / 线条抖动强度（沸腾效果）。")]
        public ClampedFloatParameter lineWiggleIntensity = new ClampedFloatParameter(0.0f, 0.0f, 0.01f);
        [Tooltip("Line wiggle speed (frame rate of boiling) / 线条抖动速度（沸腾频率）。")]
        public ClampedFloatParameter lineWiggleSpeed = new ClampedFloatParameter(12.0f, 0.0f, 60.0f);

        [Header("== Speed Lines (动态速度线) ==")]
        public BoolParameter enableSpeedLines = new BoolParameter(false);
        public SpeedLineModeParameter speedLineMode = new SpeedLineModeParameter(SpeedLineMode.Radial);
        public ClampedFloatParameter speedLineIntensity = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public ClampedFloatParameter speedLineDensity = new ClampedFloatParameter(20.0f, 1.0f, 100.0f);
        public ClampedFloatParameter speedLineSpeed = new ClampedFloatParameter(10.0f, 0.0f, 50.0f);
        public ClampedFloatParameter speedLineWidth = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public ColorParameter speedLineColor = new ColorParameter(Color.black, true, false, true);

        [Header("== Pattern Shading & Dynamic Halftone (动态网点纸) ==")]
        [Tooltip("0: Off (关闭), 1: Dynamic Dots (动态圆点), 2: Dynamic Hatching (动态排线)")]
        public ClampedIntParameter patternType = new ClampedIntParameter(0, 0, 2);
        public ClampedFloatParameter patternScale = new ClampedFloatParameter(10f, 1f, 50f);
        public ClampedFloatParameter patternAngle = new ClampedFloatParameter(0.785398f, 0f, 3.14159f);
        public ClampedFloatParameter patternIntensity = new ClampedFloatParameter(0.8f, 0f, 2f);
        public ColorParameter patternColor = new ColorParameter(new Color(0.1f, 0.1f, 0.1f, 1f), true, false, true);
        [Tooltip("Luma threshold for pattern (generates pattern in dark areas below this luma) / 图案显示的亮度阈值（仅在此亮度以下的暗部生成图案）。")]
        public ClampedFloatParameter patternLumaThreshold = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Header("== Color Grading (最终调色) ==")]
        [Tooltip("Enable color grading / 开启最终调色与画面微调。")]
        public BoolParameter enableColorGrading = new BoolParameter(true);
        public ClampedFloatParameter finalSaturation = new ClampedFloatParameter(1.1f, 0.0f, 3.0f);
        public ClampedFloatParameter finalContrast = new ClampedFloatParameter(1.1f, 0.5f, 2.0f);
        public ColorParameter shadowTint = new ColorParameter(new Color(0.0f, 0.0f, 0.05f, 1f), true, false, true);
        public ClampedFloatParameter shadowInfluence = new ClampedFloatParameter(0.4f, 0.0f, 1.0f);

        [Header("== Screen FX (屏幕特效) ==")]
        public BoolParameter enablePixelate = new BoolParameter(false);
        [Tooltip("Pixel size / 像素块大小。")]
        public ClampedFloatParameter pixelSize = new ClampedFloatParameter(4.0f, 1.0f, 32.0f);
        [Tooltip("Color quantization levels (e.g., 8 means 8 colors per channel) / 色彩量化层级（如8代表每个通道只有8种颜色）。")]
        public ClampedIntParameter pixelColorCount = new ClampedIntParameter(8, 2, 64);
        [Tooltip("Bayer dithering intensity / 拜耳抖动强度。")]
        public ClampedFloatParameter pixelDitherIntensity = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        public BoolParameter enableRetroCRT = new BoolParameter(false);
        public ClampedFloatParameter crtCurve = new ClampedFloatParameter(3.5f, 1.0f, 10.0f);
        public ClampedFloatParameter chromaticAberration = new ClampedFloatParameter(0.005f, 0.0f, 0.05f);
        public ClampedFloatParameter scanlineCount = new ClampedFloatParameter(600f, 100f, 1500f);
        public ClampedFloatParameter scanlineIntensity = new ClampedFloatParameter(0.3f, 0.0f, 1.0f);
        public ClampedFloatParameter vignetteIntensity = new ClampedFloatParameter(1.5f, 0.0f, 5.0f);

        [Header("== Vaporwave FX (蒸汽波故障特效) ==")]
        public BoolParameter enableVaporwave = new BoolParameter(false);
        public TextureParameter noiseTex = new TextureParameter(null);
        [Tooltip("Glitch frequency (controls occurrence probability) / 故障发生频率（控制出现概率）")]
        public ClampedFloatParameter glitchFrequency = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        [Tooltip("Glitch scroll and switch speed / 故障滚动和切换速度")]
        public ClampedFloatParameter glitchSpeed = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);
        [Tooltip("Glitch tear intensity / 故障撕裂幅度")]
        public ClampedFloatParameter glitchIntensity = new ClampedFloatParameter(0.02f, 0.0f, 0.2f);
        [Tooltip("Film grain intensity / 胶卷颗粒强度")]
        public ClampedFloatParameter filmGrainIntensity = new ClampedFloatParameter(0.05f, 0.0f, 0.5f);

        public bool IsActive() => active && effectIntensity.value > 0.001f;
        public bool IsTileCompatible() => false;
    }
}