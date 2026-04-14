// CelLookRenderFeature.cs

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CelLookPostProcess
{
    public class CelLookRenderFeature : ScriptableRendererFeature
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        private CelLookRenderPass _pass;

        public override void Create()
        {
            _pass = new CelLookRenderPass(renderPassEvent);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview) return;

            var stack = VolumeManager.instance.stack;
            var settings = stack.GetComponent<CelLookSettings>();

            if (settings == null || !settings.IsActive()) return;

            _pass.Setup(settings);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
        }
    }

    public class CelLookRenderPass : ScriptableRenderPass
    {
        private const int PASS_FLATTEN = 0;
        private const int PASS_UBER = 1;

        private Material _material;
        private CelLookSettings _settings;
        private RenderTextureDescriptor _rtDescriptor;

        private RTHandle _tempRT;
        private RTHandle _originalRT;

        private static readonly int ID_EffectIntensity = Shader.PropertyToID("_EffectIntensity");
        private static readonly int ID_KuwaharaRadius = Shader.PropertyToID("_KuwaharaRadius");
        private static readonly int ID_Saturation = Shader.PropertyToID("_Saturation");
        private static readonly int ID_Contrast = Shader.PropertyToID("_Contrast");
        private static readonly int ID_Brightness = Shader.PropertyToID("_Brightness");
        private static readonly int ID_ShadowHueShift = Shader.PropertyToID("_ShadowHueShift");
        private static readonly int ID_ShadowSatBoost = Shader.PropertyToID("_ShadowSatBoost");
        private static readonly int ID_ShadowThreshold = Shader.PropertyToID("_ShadowThreshold");
        private static readonly int ID_ShadowSmoothness = Shader.PropertyToID("_ShadowSmoothness");
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

        private static readonly int ID_EnableSilhouette = Shader.PropertyToID("_EnableSilhouette");
        private static readonly int ID_SilhouetteShadowColor = Shader.PropertyToID("_SilhouetteShadowColor");
        private static readonly int ID_SilhouetteMidColor = Shader.PropertyToID("_SilhouetteMidColor");
        private static readonly int ID_SilhouetteHighColor = Shader.PropertyToID("_SilhouetteHighColor");
        private static readonly int ID_SilhouetteThreshold1 = Shader.PropertyToID("_SilhouetteThreshold1");
        private static readonly int ID_SilhouetteThreshold2 = Shader.PropertyToID("_SilhouetteThreshold2");

        private static readonly int ID_PatternType = Shader.PropertyToID("_PatternType");
        private static readonly int ID_PatternScale = Shader.PropertyToID("_PatternScale");
        private static readonly int ID_PatternAngle = Shader.PropertyToID("_PatternAngle");
        private static readonly int ID_PatternIntensity = Shader.PropertyToID("_PatternIntensity");
        private static readonly int ID_PatternColor = Shader.PropertyToID("_PatternColor");
        private static readonly int ID_PatternLumaThreshold = Shader.PropertyToID("_PatternLumaThreshold");

        private static readonly int ID_EnablePixelate = Shader.PropertyToID("_EnablePixelate");
        private static readonly int ID_PixelSize = Shader.PropertyToID("_PixelSize");
        private static readonly int ID_EnableRetroCRT = Shader.PropertyToID("_EnableRetroCRT");
        private static readonly int ID_CRTCurve = Shader.PropertyToID("_CRTCurve");
        private static readonly int ID_ChromaticAberration = Shader.PropertyToID("_ChromaticAberration");
        private static readonly int ID_ScanlineCount = Shader.PropertyToID("_ScanlineCount");
        private static readonly int ID_ScanlineIntensity = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int ID_VignetteIntensity = Shader.PropertyToID("_VignetteIntensity");

        private static readonly int ID_OriginalCameraTex = Shader.PropertyToID("_OriginalCameraTex");
        private static readonly int ID_UseRampMap = Shader.PropertyToID("_UseRampMap");
        private static readonly int ID_RampMap = Shader.PropertyToID("_RampMap");
        private static readonly int ID_StencilRef = Shader.PropertyToID("_StencilRef");
        private static readonly int ID_StencilComp = Shader.PropertyToID("_StencilComp");

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
                if (shader != null)
                {
                    _material = CoreUtils.CreateEngineMaterial(shader);
                }
            }
        }

        private void UpdateMaterialProperties()
        {
            _material.SetFloat(ID_EffectIntensity, _settings.effectIntensity.value);

            int stencilComp = _settings.enableStencil.value ? (int)CompareFunction.NotEqual : (int)CompareFunction.Always;
            _material.SetInteger(ID_StencilRef, _settings.stencilRef.value);
            _material.SetInteger(ID_StencilComp, stencilComp);

            _material.SetInteger(ID_KuwaharaRadius, _settings.kuwaharaRadius.value);

            _material.SetFloat(ID_UseRampMap, _settings.useRampMap.value ? 1f : 0f);
            if (_settings.rampMap.value != null)
            {
                _material.SetTexture(ID_RampMap, _settings.rampMap.value);
            }

            _material.SetFloat(ID_Saturation, _settings.saturation.value);
            _material.SetFloat(ID_Contrast, _settings.contrast.value);
            _material.SetFloat(ID_Brightness, _settings.brightness.value);
            _material.SetFloat(ID_ShadowHueShift, _settings.shadowHueShift.value);
            _material.SetFloat(ID_ShadowSatBoost, _settings.shadowSatBoost.value);
            _material.SetFloat(ID_ShadowThreshold, _settings.shadowThreshold.value);
            _material.SetFloat(ID_ShadowSmoothness, _settings.shadowSmoothness.value);
            _material.SetFloat(ID_ShadowDarken, _settings.shadowDarken.value);

            _material.SetFloat(ID_LineThickness, _settings.lineThickness.value);
            _material.SetFloat(ID_DepthThreshold, _settings.depthThreshold.value);
            _material.SetFloat(ID_NormalThreshold, _settings.normalThreshold.value);
            _material.SetColor(ID_LineColor, _settings.lineColor.value);
            _material.SetFloat(ID_LineIntensity, _settings.lineIntensity.value);
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

            _material.SetInteger(ID_PatternType, _settings.patternType.value);
            _material.SetFloat(ID_PatternScale, _settings.patternScale.value);
            _material.SetFloat(ID_PatternAngle, _settings.patternAngle.value);
            _material.SetFloat(ID_PatternIntensity, _settings.patternIntensity.value);
            _material.SetColor(ID_PatternColor, _settings.patternColor.value);
            _material.SetFloat(ID_PatternLumaThreshold, _settings.patternLumaThreshold.value);

            _material.SetFloat(ID_EnablePixelate, _settings.enablePixelate.value ? 1f : 0f);
            _material.SetFloat(ID_PixelSize, _settings.pixelSize.value);
            _material.SetFloat(ID_EnableRetroCRT, _settings.enableRetroCRT.value ? 1f : 0f);
            _material.SetFloat(ID_CRTCurve, _settings.crtCurve.value);
            _material.SetFloat(ID_ChromaticAberration, _settings.chromaticAberration.value);
            _material.SetFloat(ID_ScanlineCount, _settings.scanlineCount.value);
            _material.SetFloat(ID_ScanlineIntensity, _settings.scanlineIntensity.value);
            _material.SetFloat(ID_VignetteIntensity, _settings.vignetteIntensity.value);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            _rtDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            _rtDescriptor.msaaSamples = 1;
            _rtDescriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(ref _tempRT, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookTemp");
            RenderingUtils.ReAllocateIfNeeded(ref _originalRT, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookOriginal");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _settings == null) return;

            var cmd = CommandBufferPool.Get("CelLookPostProcess");
            UpdateMaterialProperties();

            var cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            var depthTarget = renderingData.cameraData.renderer.cameraDepthTargetHandle;

            Blitter.BlitCameraTexture(cmd, cameraTarget, _originalRT);
            cmd.SetGlobalTexture(ID_OriginalCameraTex, _originalRT);

            // 为确保模板测试生效，需要绑定相机的Depth/Stencil Buffer
            CoreUtils.SetRenderTarget(cmd, _tempRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);

            if (_settings.kuwaharaRadius.value > 0)
            {
                Blitter.BlitCameraTexture(cmd, _originalRT, _tempRT, _material, PASS_FLATTEN);
                CoreUtils.SetRenderTarget(cmd, cameraTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, depthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                Blitter.BlitCameraTexture(cmd, _tempRT, cameraTarget, _material, PASS_UBER);
            }
            else
            {
                CoreUtils.SetRenderTarget(cmd, cameraTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, depthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                Blitter.BlitCameraTexture(cmd, _originalRT, cameraTarget, _material, PASS_UBER);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public void Dispose()
        {
            _tempRT?.Release();
            _originalRT?.Release();
            CoreUtils.Destroy(_material);
        }
    }
}