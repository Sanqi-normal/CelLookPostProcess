import re

file_path = r'E:\Unity\fpsbuiltin\Assets\CelLookPostProcess\Scripts\CelLookRenderFeature.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

# 1. Add PASS_BLEND
text = text.replace('private const int PASS_UBER = 2;', 'private const int PASS_UBER = 2;\n        private const int PASS_BLEND = 3;')

# 2. Add RTHandles
text = text.replace('private RTHandle _originalRT;', 'private RTHandle _originalRT;\n        private RTHandle _oldStyleRT;\n        private RTHandle _prefilterRT;')

# 3. Add ID_OldStyleTex and ID_TransitionDepth
search_str = 'private static readonly int ID_EffectIntensity = Shader.PropertyToID("_EffectIntensity");'
replace_str = 'private static readonly int ID_OldStyleTex = Shader.PropertyToID("_OldStyleTex");\n        private static readonly int ID_TransitionDepth = Shader.PropertyToID("_TransitionDepth");\n        private static readonly int ID_EffectIntensity = Shader.PropertyToID("_EffectIntensity");'
text = text.replace(search_str, replace_str)

# 4. Modify UpdateMaterialProperties
method_start = text.find('private void UpdateMaterialProperties()')
method_end = text.find('public override void OnCameraSetup')
if method_start != -1 and method_end != -1:
    method_body = text[method_start:method_end]
    new_method_body = method_body.replace('private void UpdateMaterialProperties()', 'private void UpdateMaterialProperties(CelLookSettings settings)')
    new_method_body = new_method_body.replace('_settings.', 'settings.')
    text = text[:method_start] + new_method_body + text[method_end:]

# 5. Modify OnCameraSetup
setup_start = text.find('public override void OnCameraSetup')
setup_end = text.find('public override void Execute')
if setup_start != -1 and setup_end != -1:
    setup_body = text[setup_start:setup_end]
    old_alloc = 'RenderingUtils.ReAllocateIfNeeded(ref _originalRT, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookOriginal");'
    new_alloc = old_alloc + '\n            RenderingUtils.ReAllocateIfNeeded(ref _oldStyleRT, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookOldStyle");\n            RenderingUtils.ReAllocateIfNeeded(ref _prefilterRT, _rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CelLookPrefilter");'
    new_setup_body = setup_body.replace(old_alloc, new_alloc)
    text = text[:setup_start] + new_setup_body + text[setup_end:]

# 6. Rewrite Execute and add RenderStyle, modify Dispose
execute_start = text.find('public override void Execute')
cleanup_start = text.find('public override void OnCameraCleanup')

new_execute = '''public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _settings == null) return;

            var cmd = CommandBufferPool.Get("CelLookPostProcess");

            var cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            var depthTarget = renderingData.cameraData.renderer.cameraDepthTargetHandle;

            Blitter.BlitCameraTexture(cmd, cameraTarget, _originalRT);
            cmd.SetGlobalTexture(ID_OriginalCameraTex, _originalRT);

            if (CelLookRenderFeature.IsTransitioning && CelLookRenderFeature.OldSettingsForTransition != null)
            {
                RenderStyle(cmd, CelLookRenderFeature.OldSettingsForTransition, _originalRT, _oldStyleRT, depthTarget);
                RenderStyle(cmd, _settings, _originalRT, _tempRT, depthTarget);

                _material.SetTexture(ID_OldStyleTex, _oldStyleRT);
                _material.SetFloat(ID_TransitionDepth, CelLookRenderFeature.TransitionDepth);
                Blitter.BlitCameraTexture(cmd, _tempRT, cameraTarget, _material, PASS_BLEND);
            }
            else
            {
                RenderStyle(cmd, _settings, _originalRT, cameraTarget, depthTarget);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void RenderStyle(CommandBuffer cmd, CelLookSettings settings, RTHandle source, RTHandle destination, RenderTargetIdentifier depthTarget)
        {
            UpdateMaterialProperties(settings);

            if (settings.preFilterMode.value == PreFilterMode.Kuwahara && settings.kuwaharaRadius.value > 0)
            {
                CoreUtils.SetRenderTarget(cmd, _prefilterRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                Blitter.BlitCameraTexture(cmd, source, _prefilterRT, _material, PASS_KUWAHARA);

                CoreUtils.SetRenderTarget(cmd, destination, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, depthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                Blitter.BlitCameraTexture(cmd, _prefilterRT, destination, _material, PASS_UBER);
            }
            else if (settings.preFilterMode.value == PreFilterMode.Bilateral)
            {
                CoreUtils.SetRenderTarget(cmd, _prefilterRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                Blitter.BlitCameraTexture(cmd, source, _prefilterRT, _material, PASS_BILATERAL);

                CoreUtils.SetRenderTarget(cmd, destination, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, depthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                Blitter.BlitCameraTexture(cmd, _prefilterRT, destination, _material, PASS_UBER);
            }
            else
            {
                CoreUtils.SetRenderTarget(cmd, destination, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, depthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                Blitter.BlitCameraTexture(cmd, source, destination, _material, PASS_UBER);
            }
        }

        '''

if execute_start != -1 and cleanup_start != -1:
    text = text[:execute_start] + new_execute + text[cleanup_start:]

text = text.replace('_tempRT?.Release();', '_tempRT?.Release();\n            _oldStyleRT?.Release();\n            _prefilterRT?.Release();')

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(text)
print('Success!')
