// CelLookRenderFeature.cs

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CelLookPostProcess
{
    public class CelLookRenderFeature : ScriptableRendererFeature
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader shader;
        private CelLookRenderPass _pass;

        public override void Create()
        {
            _pass = new CelLookRenderPass(renderPassEvent, shader);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (shader == null)
            {
                shader = Shader.Find("Hidden/CelLookPostProcess");
            }
        }
#endif

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
        private const int PASS_KUWAHARA = 0;
        private const int PASS_BILATERAL = 1;
        private const int PASS_UBER = 2;

        private Material _material;
        private Shader _shader;
        private CelLookSettings _settings;
        private RenderTextureDescriptor _rtDescriptor;

        private RTHandle _tempRT;
        private RTHandle _originalRT;

        private static readonly int ID_EffectIntensity = Shader.PropertyToID("_EffectIntensity");
        private static readonly int ID_KuwaharaRadius = Shader.PropertyToID("_KuwaharaRadius");
        private static readonly int ID_BilateralColorSigma = Shader.PropertyToID("_BilateralColorSigma");
        private static readonly int ID_BilateralSpatialSigma = Shader.PropertyToID("_BilateralSpatialSigma");

        private static readonly int ID_RampMap = Shader.PropertyToID("_RampMap");
        private static readonly int ID_CelSteps = Shader.PropertyToID("_CelSteps");
        private static readonly int ID_CelStepSmoothness = Shader.PropertyToID("_CelStepSmoothness");

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
        private static readonly int ID_ColorThreshold = Shader.PropertyToID("_ColorThreshold");
        private static readonly int ID_LineColor = Shader.PropertyToID("_LineColor");
        private static readonly int ID_LineIntensity = Shader.PropertyToID("_LineIntensity");
        private static readonly int ID_DepthFalloff = Shader.PropertyToID("_DepthFalloff");
        
        private static readonly int ID_FinalSaturation = Shader.PropertyToID("_FinalSaturation");
        private static readonly int ID_FinalContrast = Shader.PropertyToID("_FinalContrast");
        private static readonly int ID_ShadowTint = Shader.PropertyToID("_ShadowTint");
        private static readonly int ID_ShadowInfluence = Shader.PropertyToID("_ShadowInfluence");

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

        private static readonly int ID_PixelSize = Shader.PropertyToID("_PixelSize");
        private static readonly int ID_CRTCurve = Shader.PropertyToID("_CRTCurve");
        private static readonly int ID_ChromaticAberration = Shader.PropertyToID("_ChromaticAberration");
        private static readonly int ID_ScanlineCount = Shader.PropertyToID("_ScanlineCount");
        private static readonly int ID_ScanlineIntensity = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int ID_VignetteIntensity = Shader.PropertyToID("_VignetteIntensity");
        
        private static readonly int ID_NoiseTex = Shader.PropertyToID("_NoiseTex");
        private static readonly int ID_GlitchFrequency = Shader.PropertyToID("_GlitchFrequency");
        private static readonly int ID_GlitchSpeed = Shader.PropertyToID("_GlitchSpeed");
        private static readonly int ID_GlitchIntensity = Shader.PropertyToID("_GlitchIntensity");
        private static readonly int ID_FilmGrainIntensity = Shader.PropertyToID("_FilmGrainIntensity");

        private static readonly int ID_OriginalCameraTex = Shader.PropertyToID("_OriginalCameraTex");
        private static readonly int ID_StencilRef = Shader.PropertyToID("_StencilRef");
        private static readonly int ID_StencilComp = Shader.PropertyToID("_StencilComp");

        public CelLookRenderPass(RenderPassEvent evt, Shader shader)
        {
            renderPassEvent = evt;
            _shader = shader;
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        public void Setup(CelLookSettings settings)
        {
            _settings = settings;

            if (_material == null && _shader != null)
            {
                _material = CoreUtils.CreateEngineMaterial(_shader);
            }
        }

        private void UpdateMaterialProperties()
        {
            _material.SetFloat(ID_EffectIntensity, _settings.effectIntensity.value);

            int stencilComp = _settings.enableStencil.value ? (int)CompareFunction.NotEqual : (int)CompareFunction.Always;
            _material.SetInteger(ID_StencilRef, _settings.stencilRef.value);
            _material.SetInteger(ID_StencilComp, stencilComp);

            _material.SetInteger(ID_KuwaharaRadius, _settings.kuwaharaRadius.value);
            _material.SetFloat(ID_BilateralColorSigma, _settings.bilateralColorSigma.value);
            _material.SetFloat(ID_BilateralSpatialSigma, _settings.bilateralSpatialSigma.value);

            if (_settings.rampMap.value != null)
            {
                _material.SetTexture(ID_RampMap, _settings.rampMap.value);
            }
            _material.SetInteger(ID_CelSteps, _settings.celSteps.value);
            _material.SetFloat(ID_CelStepSmoothness, _settings.celStepSmoothness.value);

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
            _material.SetFloat(ID_ColorThreshold, _settings.colorThreshold.value);
            _material.SetColor(ID_LineColor, _settings.lineColor.value);
            _material.SetFloat(ID_LineIntensity, _settings.lineIntensity.value);
            _material.SetFloat(ID_DepthFalloff, _settings.depthFalloff.value);

            _material.SetFloat(ID_FinalSaturation, _settings.finalSaturation.value);
            _material.SetFloat(ID_FinalContrast, _settings.finalContrast.value);
            _material.SetColor(ID_ShadowTint, _settings.shadowTint.value);
            _material.SetFloat(ID_ShadowInfluence, _settings.shadowInfluence.value);

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

            _material.SetFloat(ID_PixelSize, _settings.pixelSize.value);
            _material.SetFloat(ID_CRTCurve, _settings.crtCurve.value);
            _material.SetFloat(ID_ChromaticAberration, _settings.chromaticAberration.value);
            _material.SetFloat(ID_ScanlineCount, _settings.scanlineCount.value);
            _material.SetFloat(ID_ScanlineIntensity, _settings.scanlineIntensity.value);
            _material.SetFloat(ID_VignetteIntensity, _settings.vignetteIntensity.value);

            if (_settings.noiseTex.value != null)
            {
                _material.SetTexture(ID_NoiseTex, _settings.noiseTex.value);
            }
            _material.SetFloat(ID_GlitchFrequency, _settings.glitchFrequency.value);
            _material.SetFloat(ID_GlitchSpeed, _settings.glitchSpeed.value);
            _material.SetFloat(ID_GlitchIntensity, _settings.glitchIntensity.value);
            _material.SetFloat(ID_FilmGrainIntensity, _settings.filmGrainIntensity.value);

            // Set Keywords for UberPass efficiency and explicit toggles
            CoreUtils.SetKeyword(_material, "_ENABLE_COLOR_MAPPING", _settings.enableColorMapping.value);
            CoreUtils.SetKeyword(_material, "_ENABLE_MANGA_LINES", _settings.enableMangaLines.value);
            CoreUtils.SetKeyword(_material, "_ENABLE_COLOR_GRADING", _settings.enableColorGrading.value);
            
            CoreUtils.SetKeyword(_material, "_ENABLE_VAPORWAVE", _settings.enableVaporwave.value);
            CoreUtils.SetKeyword(_material, "_ENABLE_RETRO_CRT", _settings.enableRetroCRT.value);
            CoreUtils.SetKeyword(_material, "_ENABLE_PIXELATE", _settings.enablePixelate.value);
            CoreUtils.SetKeyword(_material, "_ENABLE_SILHOUETTE", _settings.enableSilhouette.value);
            CoreUtils.SetKeyword(_material, "_USE_RAMP_MAP", _settings.useRampMap.value);
            CoreUtils.SetKeyword(_material, "_ENABLE_COMIC_PATTERN", _settings.patternType.value > 0);
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

            if (_settings.preFilterMode.value == PreFilterMode.Kuwahara && _settings.kuwaharaRadius.value > 0)
            {
                Blitter.BlitCameraTexture(cmd, _originalRT, _tempRT, _material, PASS_KUWAHARA);
                CoreUtils.SetRenderTarget(cmd, cameraTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, depthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                Blitter.BlitCameraTexture(cmd, _tempRT, cameraTarget, _material, PASS_UBER);
            }
            else if (_settings.preFilterMode.value == PreFilterMode.Bilateral)
            {
                Blitter.BlitCameraTexture(cmd, _originalRT, _tempRT, _material, PASS_BILATERAL);
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
