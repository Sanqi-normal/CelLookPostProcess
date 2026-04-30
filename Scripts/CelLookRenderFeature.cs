// CelLookRenderFeature.cs

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CelLookPostProcess
{
    public class CelLookRenderFeature : ScriptableRendererFeature
    {
        public static bool IsTransitioning;
        public static float TransitionDepth;
        public static float ScanDirection = 1f;
        public static float ShakeIntensity = 0f;
        public static float OrganicWaveAmplitude = 4f;
        public static float VoidBandWidth = 8f;
        public static float JitterBandWidth = 2f;
        public static CelLookSettings OldSettingsForTransition;

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
        private const int PASS_BLEND = 3;

        private Material _material;
        private Material _oldMaterial;
        private Shader _shader;
        private CelLookSettings _settings;
        private RenderTextureDescriptor _rtDescriptor;

        private RTHandle _tempRT;
        private RTHandle _originalRT;
        private RTHandle _oldStyleRT;
        private RTHandle _prefilterRT;

        private static readonly int ID_OldStyleTex = Shader.PropertyToID("_OldStyleTex");
        private static readonly int ID_TransitionDepth = Shader.PropertyToID("_TransitionDepth");
        private static readonly int ID_ScanDirection = Shader.PropertyToID("_ScanDirection");
        private static readonly int ID_ShakeIntensity = Shader.PropertyToID("_ShakeIntensity");
        private static readonly int ID_OrganicWaveAmplitude = Shader.PropertyToID("_OrganicWaveAmplitude");
        private static readonly int ID_VoidBandWidth = Shader.PropertyToID("_VoidBandWidth");
        private static readonly int ID_JitterBandWidth = Shader.PropertyToID("_JitterBandWidth");
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
        private static readonly int ID_LineFadeStart = Shader.PropertyToID("_LineFadeStart");
        private static readonly int ID_LineFadeEnd = Shader.PropertyToID("_LineFadeEnd");
        private static readonly int ID_LineWiggleIntensity = Shader.PropertyToID("_LineWiggleIntensity");
        private static readonly int ID_LineWiggleSpeed = Shader.PropertyToID("_LineWiggleSpeed");

        private static readonly int ID_SpeedLineMode = Shader.PropertyToID("_SpeedLineMode");
        private static readonly int ID_SpeedLineIntensity = Shader.PropertyToID("_SpeedLineIntensity");
        private static readonly int ID_SpeedLineDensity = Shader.PropertyToID("_SpeedLineDensity");
        private static readonly int ID_SpeedLineSpeed = Shader.PropertyToID("_SpeedLineSpeed");
        private static readonly int ID_SpeedLineWidth = Shader.PropertyToID("_SpeedLineWidth");
        private static readonly int ID_SpeedLineColor = Shader.PropertyToID("_SpeedLineColor");
        
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
        private static readonly int ID_PixelColorCount = Shader.PropertyToID("_PixelColorCount");
        private static readonly int ID_PixelDitherIntensity = Shader.PropertyToID("_PixelDitherIntensity");
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
            if (_oldMaterial == null && _shader != null)
            {
                _oldMaterial = CoreUtils.CreateEngineMaterial(_shader);
            }
        }

        private void UpdateMaterialProperties(CelLookSettings settings, Material material)
        {
            material.SetFloat(ID_EffectIntensity, settings.effectIntensity.value);

            int stencilComp = settings.enableStencil.value ? (int)CompareFunction.NotEqual : (int)CompareFunction.Always;
            material.SetFloat(ID_StencilRef, settings.stencilRef.value);
            material.SetFloat(ID_StencilComp, stencilComp);

            material.SetInteger(ID_KuwaharaRadius, settings.kuwaharaRadius.value);
            material.SetFloat(ID_BilateralColorSigma, settings.bilateralColorSigma.value);
            material.SetFloat(ID_BilateralSpatialSigma, settings.bilateralSpatialSigma.value);

            if (settings.rampMap.value != null)
            {
                material.SetTexture(ID_RampMap, settings.rampMap.value);
            }
            material.SetInteger(ID_CelSteps, settings.celSteps.value);
            material.SetFloat(ID_CelStepSmoothness, settings.celStepSmoothness.value);

            material.SetFloat(ID_Saturation, settings.saturation.value);
            material.SetFloat(ID_Contrast, settings.contrast.value);
            material.SetFloat(ID_Brightness, settings.brightness.value);
            material.SetFloat(ID_ShadowHueShift, settings.shadowHueShift.value);
            material.SetFloat(ID_ShadowSatBoost, settings.shadowSatBoost.value);
            material.SetFloat(ID_ShadowThreshold, settings.shadowThreshold.value);
            material.SetFloat(ID_ShadowSmoothness, settings.shadowSmoothness.value);
            material.SetFloat(ID_ShadowDarken, settings.shadowDarken.value);

            material.SetFloat(ID_LineThickness, settings.lineThickness.value);
            material.SetFloat(ID_DepthThreshold, settings.depthThreshold.value);
            material.SetFloat(ID_NormalThreshold, settings.normalThreshold.value);
            material.SetFloat(ID_ColorThreshold, settings.colorThreshold.value);
            material.SetColor(ID_LineColor, settings.lineColor.value);
            material.SetFloat(ID_LineIntensity, settings.lineIntensity.value);
            material.SetFloat(ID_LineFadeStart, settings.lineFadeStart.value);
            material.SetFloat(ID_LineFadeEnd, settings.lineFadeEnd.value);
            material.SetFloat(ID_LineWiggleIntensity, settings.lineWiggleIntensity.value);
            material.SetFloat(ID_LineWiggleSpeed, settings.lineWiggleSpeed.value);

            material.SetInteger(ID_SpeedLineMode, (int)settings.speedLineMode.value);
            material.SetFloat(ID_SpeedLineIntensity, settings.speedLineIntensity.value);
            material.SetFloat(ID_SpeedLineDensity, settings.speedLineDensity.value);
            material.SetFloat(ID_SpeedLineSpeed, settings.speedLineSpeed.value);
            material.SetFloat(ID_SpeedLineWidth, settings.speedLineWidth.value);
            material.SetColor(ID_SpeedLineColor, settings.speedLineColor.value);

            material.SetFloat(ID_FinalSaturation, settings.finalSaturation.value);
            material.SetFloat(ID_FinalContrast, settings.finalContrast.value);
            material.SetColor(ID_ShadowTint, settings.shadowTint.value);
            material.SetFloat(ID_ShadowInfluence, settings.shadowInfluence.value);

            material.SetColor(ID_SilhouetteShadowColor, settings.silhouetteShadowColor.value);
            material.SetColor(ID_SilhouetteMidColor, settings.silhouetteMidColor.value);
            material.SetColor(ID_SilhouetteHighColor, settings.silhouetteHighColor.value);
            material.SetFloat(ID_SilhouetteThreshold1, settings.silhouetteThreshold1.value);
            material.SetFloat(ID_SilhouetteThreshold2, settings.silhouetteThreshold2.value);

            material.SetInteger(ID_PatternType, settings.patternType.value);
            material.SetFloat(ID_PatternScale, settings.patternScale.value);
            material.SetFloat(ID_PatternAngle, settings.patternAngle.value);
            material.SetFloat(ID_PatternIntensity, settings.patternIntensity.value);
            material.SetColor(ID_PatternColor, settings.patternColor.value);
            material.SetFloat(ID_PatternLumaThreshold, settings.patternLumaThreshold.value);

            material.SetFloat(ID_PixelSize, settings.pixelSize.value);
            material.SetInteger(ID_PixelColorCount, settings.pixelColorCount.value);
            material.SetFloat(ID_PixelDitherIntensity, settings.pixelDitherIntensity.value);
            material.SetFloat(ID_CRTCurve, settings.crtCurve.value);
            material.SetFloat(ID_ChromaticAberration, settings.chromaticAberration.value);
            material.SetFloat(ID_ScanlineCount, settings.scanlineCount.value);
            material.SetFloat(ID_ScanlineIntensity, settings.scanlineIntensity.value);
            material.SetFloat(ID_VignetteIntensity, settings.vignetteIntensity.value);

            if (settings.noiseTex.value != null)
            {
                material.SetTexture(ID_NoiseTex, settings.noiseTex.value);
            }
            material.SetFloat(ID_GlitchFrequency, settings.glitchFrequency.value);
            material.SetFloat(ID_GlitchSpeed, settings.glitchSpeed.value);
            material.SetFloat(ID_GlitchIntensity, settings.glitchIntensity.value);
            material.SetFloat(ID_FilmGrainIntensity, settings.filmGrainIntensity.value);

            // Set Keywords for UberPass efficiency and explicit toggles
            CoreUtils.SetKeyword(material, "_ENABLE_COLOR_MAPPING", settings.enableColorMapping.value);
            CoreUtils.SetKeyword(material, "_ENABLE_MANGA_LINES", settings.enableMangaLines.value);
            CoreUtils.SetKeyword(material, "_ENABLE_COLOR_GRADING", settings.enableColorGrading.value);
            
            CoreUtils.SetKeyword(material, "_ENABLE_VAPORWAVE", settings.enableVaporwave.value);
            CoreUtils.SetKeyword(material, "_ENABLE_RETRO_CRT", settings.enableRetroCRT.value);
            CoreUtils.SetKeyword(material, "_ENABLE_PIXELATE", settings.enablePixelate.value);
            CoreUtils.SetKeyword(material, "_ENABLE_SILHOUETTE", settings.enableSilhouette.value);
            CoreUtils.SetKeyword(material, "_ENABLE_SPEED_LINES", settings.enableSpeedLines.value);
            CoreUtils.SetKeyword(material, "_USE_RAMP_MAP", settings.useRampMap.value);
            CoreUtils.SetKeyword(material, "_ENABLE_COMIC_PATTERN", settings.patternType.value > 0);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            _rtDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            _rtDescriptor.msaaSamples = 1;
            _rtDescriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(ref _tempRT, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookTemp");
            RenderingUtils.ReAllocateIfNeeded(ref _originalRT, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookOriginal");
            RenderingUtils.ReAllocateIfNeeded(ref _oldStyleRT, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookOldStyle");
            RenderingUtils.ReAllocateIfNeeded(ref _prefilterRT, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookPrefilter");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _settings == null) return;

            var cmd = CommandBufferPool.Get("CelLookPostProcess");

            var cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            var depthTarget = renderingData.cameraData.renderer.cameraDepthTargetHandle;

            Blitter.BlitCameraTexture(cmd, cameraTarget, _originalRT);
            cmd.SetGlobalTexture(ID_OriginalCameraTex, _originalRT);

            if (CelLookRenderFeature.IsTransitioning && CelLookRenderFeature.OldSettingsForTransition != null)
            {
                RenderStyle(cmd, CelLookRenderFeature.OldSettingsForTransition, _oldMaterial, _originalRT, _oldStyleRT, depthTarget);
                RenderStyle(cmd, _settings, _material, _originalRT, _tempRT, depthTarget);

                _material.SetTexture(ID_OldStyleTex, _oldStyleRT);
                _material.SetFloat(ID_TransitionDepth, CelLookRenderFeature.TransitionDepth);
                _material.SetFloat(ID_ScanDirection, CelLookRenderFeature.ScanDirection);
                _material.SetFloat(ID_ShakeIntensity, CelLookRenderFeature.ShakeIntensity);
                _material.SetFloat(ID_OrganicWaveAmplitude, CelLookRenderFeature.OrganicWaveAmplitude);
                _material.SetFloat(ID_VoidBandWidth, CelLookRenderFeature.VoidBandWidth);
                _material.SetFloat(ID_JitterBandWidth, CelLookRenderFeature.JitterBandWidth);
                Blitter.BlitCameraTexture(cmd, _tempRT, cameraTarget, _material, PASS_BLEND);
            }
            else
            {
                RenderStyle(cmd, _settings, _material, _originalRT, cameraTarget, depthTarget);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }


        private void BlitWithStencil(CommandBuffer cmd, RTHandle source, RTHandle destination,
                                     RTHandle depthStencil, Material material, int pass)
        {
            // Bind colour + depth/stencil together so the GPU Stencil test has a buffer to read.
            cmd.SetRenderTarget(
                destination,   RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                depthStencil,  RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);

            // Upload the source as _BlitTexture (the name Blit.hlsl / Vert expects).
            cmd.SetGlobalTexture(Shader.PropertyToID("_BlitTexture"), source);
            // Match the scale/bias that BlitCameraTexture normally sets.
            cmd.SetGlobalVector(Shader.PropertyToID("_BlitScaleBias"), new Vector4(1f, 1f, 0f, 0f));

            cmd.DrawProcedural(Matrix4x4.identity, material, pass,
                               MeshTopology.Triangles, 3, 1);
        }

        private void RenderStyle(CommandBuffer cmd, CelLookSettings settings, Material material,
                                 RTHandle source, RTHandle destination, RTHandle depthStencil)
        {
            UpdateMaterialProperties(settings, material);

            bool useStencil = settings.enableStencil.value;

            if (settings.preFilterMode.value == PreFilterMode.Kuwahara && settings.kuwaharaRadius.value > 0)
            {
                // Pre-filter passes: no stencil needed, plain blit into intermediate RT.
                Blitter.BlitCameraTexture(cmd, source, _prefilterRT, material, PASS_KUWAHARA);
                // Final Uber pass: bind depth+stencil when stencil masking is active.
                if (useStencil)
                    BlitWithStencil(cmd, _prefilterRT, destination, depthStencil, material, PASS_UBER);
                else
                    Blitter.BlitCameraTexture(cmd, _prefilterRT, destination, material, PASS_UBER);
            }
            else if (settings.preFilterMode.value == PreFilterMode.Bilateral)
            {
                Blitter.BlitCameraTexture(cmd, source, _prefilterRT, material, PASS_BILATERAL);
                if (useStencil)
                    BlitWithStencil(cmd, _prefilterRT, destination, depthStencil, material, PASS_UBER);
                else
                    Blitter.BlitCameraTexture(cmd, _prefilterRT, destination, material, PASS_UBER);
            }
            else
            {
                if (useStencil)
                    BlitWithStencil(cmd, source, destination, depthStencil, material, PASS_UBER);
                else
                    Blitter.BlitCameraTexture(cmd, source, destination, material, PASS_UBER);
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public void Dispose()
        {
            _tempRT?.Release();
            _oldStyleRT?.Release();
            _prefilterRT?.Release();
            _originalRT?.Release();
            CoreUtils.Destroy(_material);
            CoreUtils.Destroy(_oldMaterial);
        }
    }
}
