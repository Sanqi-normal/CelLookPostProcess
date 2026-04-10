using UnityEngine;

namespace CelLookPostProcess
{
    [System.Serializable]
    public class CelLookPreset
    {
        public string name = "New Preset";

        public float effectIntensity = 1f;
        public int kuwaharaRadius = 2;
        public float saturation = 1.4f;
        public float contrast = 1.3f;
        public float brightness = 0f;

        public float shadowThreshold = 0.5f;
        public float shadowHueShift = 0.04f;
        public float shadowSatBoost = 0.3f;
        public float shadowDarken = 0.6f;

        public bool enableLines = true;
        public float lineThickness = 1.5f;
        public float depthThreshold = 0.01f;
        public float normalThreshold = 0.2f;
        public Color lineColor = new Color(0.05f, 0.05f, 0.08f);
        public float lineIntensity = 1f;
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

        public void ApplyTo(CelLookSettings target)
        {
            target.effectIntensity.Override(effectIntensity);
            target.kuwaharaRadius.Override(kuwaharaRadius);
            target.saturation.Override(saturation);
            target.contrast.Override(contrast);
            target.brightness.Override(brightness);
            target.shadowThreshold.Override(shadowThreshold);
            target.shadowHueShift.Override(shadowHueShift);
            target.shadowSatBoost.Override(shadowSatBoost);
            target.shadowDarken.Override(shadowDarken);
            target.enableLines.Override(enableLines);
            target.lineThickness.Override(lineThickness);
            target.depthThreshold.Override(depthThreshold);
            target.normalThreshold.Override(normalThreshold);
            target.lineColor.Override(lineColor);
            target.lineIntensity.Override(lineIntensity);
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
        }

        public void LoadFrom(CelLookSettings source)
        {
            effectIntensity = source.effectIntensity.value;
            kuwaharaRadius = source.kuwaharaRadius.value;
            saturation = source.saturation.value;
            contrast = source.contrast.value;
            brightness = source.brightness.value;
            shadowThreshold = source.shadowThreshold.value;
            shadowHueShift = source.shadowHueShift.value;
            shadowSatBoost = source.shadowSatBoost.value;
            shadowDarken = source.shadowDarken.value;
            enableLines = source.enableLines.value;
            lineThickness = source.lineThickness.value;
            depthThreshold = source.depthThreshold.value;
            normalThreshold = source.normalThreshold.value;
            lineColor = source.lineColor.value;
            lineIntensity = source.lineIntensity.value;
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
        }
    }

}