using UnityEngine;

namespace CelLookPostProcess
{
    [CreateAssetMenu(fileName = "New Preset", menuName = "CelLook/Preset Asset")]
    public class CelLookPresetAsset : ScriptableObject
    {
        public CelLookPreset preset = new CelLookPreset();
    }
}