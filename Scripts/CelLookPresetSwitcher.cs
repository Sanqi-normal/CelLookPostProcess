using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CelLookPostProcess;

public class CelLookPresetSwitcher : MonoBehaviour
{
    [SerializeField] private CelLookPresetAsset[] _presets;
    [SerializeField] private int _currentIndex = -1;

    private Volume _volume;
    private CelLookSettings _celLookSettings;

        private void Start()
        {
            _volume = GetComponent<Volume>();
            if (_volume != null)
            {
                if (_volume.profile == null)
                {
                    return;
                }

                if (!_volume.profile.TryGet(out _celLookSettings))
                {
                    return;
                }

                if (_presets != null && _presets.Length > 0 && _currentIndex < 0)
                {
                    _currentIndex = 0;
                    ApplyCurrentPreset();
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                CyclePreset();
            }
        }

        public void CyclePreset()
        {
            if (_presets == null || _presets.Length == 0) return;

            _currentIndex = (_currentIndex + 1) % _presets.Length;
            ApplyCurrentPreset();
        }

    private void ApplyCurrentPreset()
    {
        if (_currentIndex < 0 || _currentIndex >= _presets.Length) return;
        if (_presets[_currentIndex] == null || _celLookSettings == null) return;

        _presets[_currentIndex].preset.ApplyTo(_celLookSettings);

        if (_volume != null)
        {
            _volume.weight = 0.999f;
            _volume.weight = 1f;
        }
    }
}