// CelLookSettings.cs
// Cel Look Post Process —— Volume Component 参数定义

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CelLookPostProcess
{
    [System.Serializable, VolumeComponentMenu("Custom/Cel Look Post Process")]
    public class CelLookSettings : VolumeComponent, IPostProcessComponent
    {
        [Header("== Global Settings (全局控制) ==")]
        [Tooltip("整体效果强度。默认为0，即关闭三渲二效果。拉到1为完全生效。")]
        public ClampedFloatParameter effectIntensity = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        [Header("== Pop Color Block (二分法色块) ==")]
        [Tooltip("色块平滑半径(Kuwahara滤波)。抹除纹理和光照产生的暗斑/噪点，保留强边缘。推荐 2~4。")]
        public ClampedIntParameter kuwaharaRadius = new ClampedIntParameter(2, 0, 6);

        [Tooltip("亮部饱和度倍数。Cel Look 风格应偏高，推荐 1.2~1.8。")]
        public ClampedFloatParameter saturation = new ClampedFloatParameter(1.4f, 0.5f, 3.0f);

        [Tooltip("对比度。让画面基底更鲜明，推荐 1.2~1.8。")]
        public ClampedFloatParameter contrast = new ClampedFloatParameter(1.3f, 0.5f, 3.0f);

        [Tooltip("亮度偏移（小量调整整体亮度）。")]
        public ClampedFloatParameter brightness = new ClampedFloatParameter(0.0f, -0.3f, 0.3f);

        [Header("== Shadow Hue Shift (阴影二分控制) ==")]
        [Tooltip("是否启用剪影模式（Silhouette）。启用后忽略阴影二分控制，使用剪影色块映射。")]
        public BoolParameter enableSilhouette = new BoolParameter(false);

        [Tooltip("剪影模式阴影色。")]
        public ColorParameter silhouetteShadowColor = new ColorParameter(new Color(0.1f, 0.1f, 0.2f, 1f), true, false, true);

        [Tooltip("剪影模式中间调色。")]
        public ColorParameter silhouetteMidColor = new ColorParameter(new Color(0.4f, 0.2f, 0.5f, 1f), true, false, true);

        [Tooltip("剪影模式高光色。")]
        public ColorParameter silhouetteHighColor = new ColorParameter(new Color(0.9f, 0.4f, 0.6f, 1f), true, false, true);

        [Tooltip("剪影阈值1，控制阴影到中间调的过渡。")]
        public ClampedFloatParameter silhouetteThreshold1 = new ClampedFloatParameter(0.3f, 0f, 1f);

        [Tooltip("剪影阈值2，控制中间调到高光的过渡。")]
        public ClampedFloatParameter silhouetteThreshold2 = new ClampedFloatParameter(0.7f, 0f, 1f);

        [Tooltip("二分法光影阈值。亮度低于此值即被强制划分为暗部。推荐 0.4~0.6。")]
        public ClampedFloatParameter shadowThreshold = new ClampedFloatParameter(0.5f, 0.1f, 0.9f);

        [Tooltip("暗部色相偏移量。让阴影有生动的色相而不是死黑，推荐 0.02~0.08。")]
        public ClampedFloatParameter shadowHueShift = new ClampedFloatParameter(0.04f, -0.2f, 0.2f);

        [Tooltip("暗部饱和度额外增强倍数。")]
        public ClampedFloatParameter shadowSatBoost = new ClampedFloatParameter(0.3f, 0.0f, 1.5f);

        [Tooltip("暗部亮度压暗系数。为了和亮部拉开明显层级，推荐 0.5~0.8。")]
        public ClampedFloatParameter shadowDarken = new ClampedFloatParameter(0.6f, 0.1f, 1.0f);

        [Header("== Manga Line Art (漫画线条) ==")]
        [Tooltip("是否开启漫画线条描边。")]
        public BoolParameter enableLines = new BoolParameter(true);

        [Tooltip("描边采样步长（像素单位）。越大线越粗，推荐 1.0~3.0。")]
        public ClampedFloatParameter lineThickness = new ClampedFloatParameter(1.5f, 0.5f, 5.0f);

        [Tooltip("深度边缘检测灵敏度。越小越容易出线，推荐 0.005~0.05。")]
        public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(0.01f, 0.0001f, 0.5f);

        [Tooltip("法线边缘检测灵敏度。推荐 0.1~0.5。")]
        public ClampedFloatParameter normalThreshold = new ClampedFloatParameter(0.2f, 0.01f, 1.5f);

        [Tooltip("线条颜色。Cel Look 风格用角色主色调的深色，不一定是纯黑。")]
        public ColorParameter lineColor = new ColorParameter(new Color(0.05f, 0.05f, 0.08f, 1f), true, false, true);

        [Tooltip("线条强度。推荐 0.8~1.0。")]
        public ClampedFloatParameter lineIntensity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("深度描边远处淡化距离（世界单位）。0 = 不淡化。推荐 30~80。")]
        public ClampedFloatParameter depthFalloff = new ClampedFloatParameter(50f, 0f, 200f);

        [Header("== Pop Color Grading (最终调色) ==")]
        [Tooltip("最终整体饱和度。推荐 1.0~1.3。")]
        public ClampedFloatParameter finalSaturation = new ClampedFloatParameter(1.1f, 0.0f, 3.0f);

        [Tooltip("最终整体对比度。推荐 1.0~1.3。")]
        public ClampedFloatParameter finalContrast = new ClampedFloatParameter(1.1f, 0.5f, 2.0f);

        [Tooltip("阴影区染色（Lift）。偏蓝紫让阴影更有动漫感。")]
        public ColorParameter shadowTint = new ColorParameter(new Color(0.0f, 0.0f, 0.05f, 1f), true, false, true);

        [Tooltip("高光区染色（Gain）。偏暖黄让亮区有青春活力感。")]
        public ColorParameter highlightTint = new ColorParameter(new Color(0.05f, 0.03f, 0.0f, 1f), true, false, true);

        [Tooltip("阴影染色影响强度。推荐 0.3~0.7。")]
        public ClampedFloatParameter shadowInfluence = new ClampedFloatParameter(0.4f, 0.0f, 1.0f);

        [Tooltip("高光染色影响强度。推荐 0.2~0.5。")]
        public ClampedFloatParameter highlightInfluence = new ClampedFloatParameter(0.3f, 0.0f, 1.0f);

        [Header("== Pixelate Mode (像素化风格) ==")]
        [Tooltip("是否启用像素化风格。将画面分割成块状像素。")]
        public BoolParameter enablePixelate = new BoolParameter(false);

        [Tooltip("像素块大小。数值越大像素越粗。")]
        public ClampedFloatParameter pixelSize = new ClampedFloatParameter(4.0f, 1.0f, 32.0f);

        // IPostProcessComponent 接口
        public bool IsActive()
        {
            // 只有当组件激活，且强度大于 0 时才执行三渲二（无 override 时默认为0不生效）
            return active && effectIntensity.value > 0.001f;
        }

        public bool IsTileCompatible() => false;
    }
}