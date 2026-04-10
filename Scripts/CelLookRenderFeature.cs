// CelLookRenderFeature.cs
// Cel Look Post Process —— URP Renderer Feature

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CelLookPostProcess
{
    public class CelLookRenderFeature : ScriptableRendererFeature
    {
        [Tooltip("后处理插入时机，建议 AfterRenderingPostProcessing 或 BeforeRenderingPostProcessing。")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        private CelLookRenderPass _pass;

        public override void Create()
        {
            _pass = new CelLookRenderPass(renderPassEvent);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview) return;

            var stack = VolumeManager.instance.stack;
            var settings = stack.GetComponent<CelLookSettings>();

            // 使用 settings.IsActive() 拦截：如果强度为 0，则直接跳过整个 Feature
            if (settings == null || !settings.IsActive()) return;

            _pass.Setup(settings);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
        }
    }

    // ================================================================
    //  Render Pass 实现
    // ================================================================
    public class CelLookRenderPass : ScriptableRenderPass
    {
        private const int PASS_FLATTEN = 0; // Kuwahara 去噪
        private const int PASS_COLOR_BIN = 1; // 二分法色块
        private const int PASS_LINE_ART = 2; // 漫画线条
        private const int PASS_COLOR_GRADE = 3; // 最终调色与强度混合

        private Material _material;
        private CelLookSettings _settings;
        private RenderTextureDescriptor _rtDescriptor;

        private RTHandle _tempRT0;
        private RTHandle _tempRT1;
        private RTHandle _tempRT2;
        private RTHandle _originalRT; // 用于保存原图以便根据 intensity 混合

        // Shader 属性 ID 缓存
        private static readonly int ID_EffectIntensity = Shader.PropertyToID("_EffectIntensity");
        private static readonly int ID_KuwaharaRadius = Shader.PropertyToID("_KuwaharaRadius");
        private static readonly int ID_Saturation = Shader.PropertyToID("_Saturation");
        private static readonly int ID_Contrast = Shader.PropertyToID("_Contrast");
        private static readonly int ID_Brightness = Shader.PropertyToID("_Brightness");
        private static readonly int ID_ShadowHueShift = Shader.PropertyToID("_ShadowHueShift");
        private static readonly int ID_ShadowSatBoost = Shader.PropertyToID("_ShadowSatBoost");
        private static readonly int ID_ShadowThreshold = Shader.PropertyToID("_ShadowThreshold");
        private static readonly int ID_ShadowDarken = Shader.PropertyToID("_ShadowDarken");
        private static readonly int ID_LineThickness = Shader.PropertyToID("_LineThickness");
        private static readonly int ID_DepthThreshold = Shader.PropertyToID("_DepthThreshold");
        private static readonly int ID_NormalThreshold = Shader.PropertyToID("_NormalThreshold");
        private static readonly int ID_LineColor = Shader.PropertyToID("_LineColor");
        private static readonly int ID_LineIntensity = Shader.PropertyToID("_LineIntensity");
        private static readonly int ID_DepthFalloff = Shader.PropertyToID("_DepthFalloff");
        private static readonly int ID_FinalSaturation = Shader.PropertyToID("_FinalSaturation");
        private static readonly int ID_FinalContrast = Shader.PropertyToID("_FinalContrast");
        private static readonly int ID_ShadowTint = Shader.PropertyToID("_ShadowTint");
        private static readonly int ID_HighlightTint = Shader.PropertyToID("_HighlightTint");
        private static readonly int ID_ShadowInfluence = Shader.PropertyToID("_ShadowInfluence");
        private static readonly int ID_HighlightInfluence = Shader.PropertyToID("_HighlightInfluence");
        private static readonly int ID_OriginalCameraTex = Shader.PropertyToID("_OriginalCameraTex");

        private static readonly int ID_EnableSilhouette = Shader.PropertyToID("_EnableSilhouette");
        private static readonly int ID_SilhouetteShadowColor = Shader.PropertyToID("_SilhouetteShadowColor");
        private static readonly int ID_SilhouetteMidColor = Shader.PropertyToID("_SilhouetteMidColor");
        private static readonly int ID_SilhouetteHighColor = Shader.PropertyToID("_SilhouetteHighColor");
        private static readonly int ID_SilhouetteThreshold1 = Shader.PropertyToID("_SilhouetteThreshold1");
        private static readonly int ID_SilhouetteThreshold2 = Shader.PropertyToID("_SilhouetteThreshold2");

        private static readonly int ID_EnablePixelate = Shader.PropertyToID("_EnablePixelate");
        private static readonly int ID_PixelSize = Shader.PropertyToID("_PixelSize");

        public CelLookRenderPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        public void Setup(CelLookSettings settings)
        {
            _settings = settings;

            if (_material == null)
            {
                var shader = Shader.Find("Hidden/CelLookPostProcess");
                if (shader == null)
                {
                    return;
                }
                _material = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        private void UpdateMaterialProperties()
        {
            _material.SetFloat(ID_EffectIntensity, _settings.effectIntensity.value);
            _material.SetInteger(ID_KuwaharaRadius, _settings.kuwaharaRadius.value);
            _material.SetFloat(ID_Saturation, _settings.saturation.value);
            _material.SetFloat(ID_Contrast, _settings.contrast.value);
            _material.SetFloat(ID_Brightness, _settings.brightness.value);
            _material.SetFloat(ID_ShadowHueShift, _settings.shadowHueShift.value);
            _material.SetFloat(ID_ShadowSatBoost, _settings.shadowSatBoost.value);
            _material.SetFloat(ID_ShadowThreshold, _settings.shadowThreshold.value);
            _material.SetFloat(ID_ShadowDarken, _settings.shadowDarken.value);
            _material.SetFloat(ID_LineThickness, _settings.lineThickness.value);
            _material.SetFloat(ID_DepthThreshold, _settings.depthThreshold.value);
            _material.SetFloat(ID_NormalThreshold, _settings.normalThreshold.value);
            _material.SetColor(ID_LineColor, _settings.lineColor.value);
            _material.SetFloat(ID_LineIntensity, _settings.enableLines.value ? _settings.lineIntensity.value : 0f);
            _material.SetFloat(ID_DepthFalloff, _settings.depthFalloff.value);
            _material.SetFloat(ID_FinalSaturation, _settings.finalSaturation.value);
            _material.SetFloat(ID_FinalContrast, _settings.finalContrast.value);
            _material.SetColor(ID_ShadowTint, _settings.shadowTint.value);
            _material.SetColor(ID_HighlightTint, _settings.highlightTint.value);
            _material.SetFloat(ID_ShadowInfluence, _settings.shadowInfluence.value);
            _material.SetFloat(ID_HighlightInfluence, _settings.highlightInfluence.value);
            _material.SetFloat(ID_EnableSilhouette, _settings.enableSilhouette.value ? 1f : 0f);
            _material.SetColor(ID_SilhouetteShadowColor, _settings.silhouetteShadowColor.value);
            _material.SetColor(ID_SilhouetteMidColor, _settings.silhouetteMidColor.value);
            _material.SetColor(ID_SilhouetteHighColor, _settings.silhouetteHighColor.value);
            _material.SetFloat(ID_SilhouetteThreshold1, _settings.silhouetteThreshold1.value);
            _material.SetFloat(ID_SilhouetteThreshold2, _settings.silhouetteThreshold2.value);
            _material.SetFloat(ID_EnablePixelate, _settings.enablePixelate.value ? 1f : 0f);
            _material.SetFloat(ID_PixelSize, _settings.pixelSize.value);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            _rtDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            _rtDescriptor.msaaSamples = 1;
            _rtDescriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(ref _tempRT0, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookTemp0");
            RenderingUtils.ReAllocateIfNeeded(ref _tempRT1, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookTemp1");
            RenderingUtils.ReAllocateIfNeeded(ref _tempRT2, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookTemp2");
            RenderingUtils.ReAllocateIfNeeded(ref _originalRT, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookOriginal");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _settings == null) return;

            var cmd = CommandBufferPool.Get("CelLookPostProcess");
            UpdateMaterialProperties();

            var cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // 1. 保存原始相机画面，用于最后根据强度混合
            Blitter.BlitCameraTexture(cmd, cameraTarget, _originalRT);
            cmd.SetGlobalTexture(ID_OriginalCameraTex, _originalRT);

            // 2. 渲染流水线
            Blitter.BlitCameraTexture(cmd, cameraTarget, _tempRT0, _material, PASS_FLATTEN);
            Blitter.BlitCameraTexture(cmd, _tempRT0, _tempRT1, _material, PASS_COLOR_BIN);
            Blitter.BlitCameraTexture(cmd, _tempRT1, _tempRT2, _material, PASS_LINE_ART);
            Blitter.BlitCameraTexture(cmd, _tempRT2, cameraTarget, _material, PASS_COLOR_GRADE);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public void Dispose()
        {
            _tempRT0?.Release();
            _tempRT1?.Release();
            _tempRT2?.Release();
            _originalRT?.Release();
            CoreUtils.Destroy(_material);
        }
    }
}