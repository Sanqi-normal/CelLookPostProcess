using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CelLookPostProcess;

public class CelLookPresetSwitcher : MonoBehaviour
{
    [SerializeField] private CelLookPresetAsset[] _presets;
    [SerializeField] private int _currentIndex = -1;
    
    [Header("Transition Settings (Transition Core)")]
    [SerializeField] private float _transitionDuration = 1.5f;
    [SerializeField] private float _maxScanDepth = 200f;
    [SerializeField, Range(1f, 5f)] private float _scanCurvePower = 3f;

    [Header("Time Dilation (Hitstop)")]
    [SerializeField] private bool _enableTimeDilation = true;
    [SerializeField, Range(0.01f, 1f)] private float _hitstopTimeScale = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _hitstopDurationRatio = 0.2f;

    [Header("Camera Shake")]
    [SerializeField] private bool _enableShake = true;
    [SerializeField] private float _shakeMaxDistance = 15f;
    [SerializeField] private float _shakeMaxIntensity = 0.03f;

    [Header("Transition Visuals")]
    [SerializeField] private float _organicWaveAmplitude = 2.0f;
    [SerializeField] private float _voidBandWidth = 4.0f;
    [SerializeField] private float _jitterBandWidth = 1.5f;

    private Volume _volume;
    private CelLookSettings _celLookSettings;
    private CelLookSettings _oldSettings;
    
    private bool _isTransitioning;
    private float _currentScanDepth;
    private bool _isReverseScan;
    private float _transitionProgress;

    private void Awake()
    {
        _oldSettings = ScriptableObject.CreateInstance<CelLookSettings>();
    }

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
                ApplyCurrentPreset(false);
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            CyclePreset(1);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            CyclePreset(-1);
        }

        if (_isTransitioning)
        {
            _transitionProgress += Time.unscaledDeltaTime / _transitionDuration;
            
            if (_enableTimeDilation)
            {
                if (_transitionProgress < _hitstopDurationRatio) {
                    Time.timeScale = Mathf.Lerp(_hitstopTimeScale, 1f, _transitionProgress / _hitstopDurationRatio);
                } else {
                    Time.timeScale = 1f;
                }
            }

            if (_transitionProgress >= 1f)
            {
                _transitionProgress = 1f;
                _isTransitioning = false;
                if (_enableTimeDilation) Time.timeScale = 1f;
                CelLookRenderFeature.IsTransitioning = false;
                CelLookRenderFeature.OldSettingsForTransition = null;
            }
            else
            {
                float t = _transitionProgress;
                float depthT = _isReverseScan ? Mathf.Pow(t, _scanCurvePower) : Mathf.Pow(1f - t, _scanCurvePower);
                _currentScanDepth = depthT * _maxScanDepth;

                float shake = 0f;
                if (_enableShake && _currentScanDepth < _shakeMaxDistance && _currentScanDepth > 0f)
                {
                    shake = (_shakeMaxDistance - _currentScanDepth) / _shakeMaxDistance;
                    shake *= shake; 
                }

                CelLookRenderFeature.IsTransitioning = true;
                CelLookRenderFeature.TransitionDepth = _currentScanDepth;
                CelLookRenderFeature.ScanDirection = _isReverseScan ? -1f : 1f;
                CelLookRenderFeature.ShakeIntensity = shake * _shakeMaxIntensity;
                CelLookRenderFeature.OrganicWaveAmplitude = _organicWaveAmplitude;
                CelLookRenderFeature.VoidBandWidth = _voidBandWidth;
                CelLookRenderFeature.JitterBandWidth = _jitterBandWidth;
                CelLookRenderFeature.OldSettingsForTransition = _oldSettings;
            }
        }
    }
    
    private void OnDisable()
    {
        Time.timeScale = 1f;
        CelLookRenderFeature.IsTransitioning = false;
    }

    public void CyclePreset(int direction = 1)
    {
        if (_presets == null || _presets.Length == 0) return;

        // Save current state before switching
        if (_currentIndex >= 0 && _currentIndex < _presets.Length)
        {
            _presets[_currentIndex].preset.ApplyTo(_oldSettings);
        }

        _currentIndex = (_currentIndex + direction + _presets.Length) % _presets.Length;
        _isReverseScan = direction < 0;
        ApplyCurrentPreset(true);
    }

    private void ApplyCurrentPreset(bool withTransition)
    {
        if (_currentIndex < 0 || _currentIndex >= _presets.Length) return;
        if (_presets[_currentIndex] == null || _celLookSettings == null) return;

        _presets[_currentIndex].preset.ApplyTo(_celLookSettings);

        if (_volume != null)
        {
            _volume.weight = 0.999f;
            _volume.weight = 1f;
        }

        if (withTransition)
        {
            _isTransitioning = true;
            _transitionProgress = 0f;
            if (_enableTimeDilation) Time.timeScale = _hitstopTimeScale;
        }
    }
}