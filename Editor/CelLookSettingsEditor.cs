// CelLookSettingsEditor.cs

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace CelLookPostProcess
{
    [CustomEditor(typeof(CelLookSettings))]
    sealed class CelLookSettingsEditor : VolumeComponentEditor
    {
        private CelLookPresetAsset[] _presets;
        private int _selectedPresetIndex = -1;
        private string _newPresetName = "My Preset";
        private const string PRESETS_FOLDER = "Assets/CelLookPostProcess/Presets";

        private void OnEnable()
        {
            LoadPresets();
        }

        private void LoadPresets()
        {
            if (!AssetDatabase.IsValidFolder(PRESETS_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/CelLookPostProcess", "Presets");
            }

            string[] guids = AssetDatabase.FindAssets("t:CelLookPresetAsset", new[] { PRESETS_FOLDER });
            _presets = new CelLookPresetAsset[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                _presets[i] = AssetDatabase.LoadAssetAtPath<CelLookPresetAsset>(path);
            }
        }

        private CelLookPresetAsset GetSelectedPreset()
        {
            if (_selectedPresetIndex > 0 && _selectedPresetIndex <= _presets.Length)
                return _presets[_selectedPresetIndex - 1];
            return null;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Cel Look セルルック风格后处理\n" +
                "默认强度为0(无效果)。调整全局强度(Effect Intensity)开启。\n" +
                "Pass 0: Kuwahara 抹除噪点暗斑\n" +
                "Pass 1: 严格二分法色块化\n" +
                "Pass 2: 漫画线条 (剔除天空)\n" +
                "Pass 3: Pop Grading 与原图混合",
                MessageType.Info);

            EditorGUILayout.Space(4);

            DrawPresetSection();

            EditorGUILayout.Space(6);
            base.OnInspectorGUI();
        }

        private void DrawPresetSection()
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

            int popupCount = _presets.Length + 1;
            string[] presetNames = new string[popupCount];
            presetNames[0] = "-- Select --";
            for (int i = 0; i < _presets.Length; i++)
            {
                presetNames[i + 1] = _presets[i] != null ? _presets[i].preset.name : "(Invalid)";
            }

            EditorGUI.BeginChangeCheck();
            _selectedPresetIndex = EditorGUILayout.Popup(_selectedPresetIndex, presetNames, GUILayout.Height(25));
            if (EditorGUI.EndChangeCheck())
            {
                if (_selectedPresetIndex > 0)
                {
                    var preset = GetSelectedPreset();
                    if (preset != null)
                    {
                        var s = target as CelLookSettings;
                        Undo.RecordObject(s, "Apply CelLook Preset");
                        preset.preset.ApplyTo(s);
                        EditorUtility.SetDirty(s);
                    }
                }
                _selectedPresetIndex = -1;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Delete Selected", GUILayout.Height(25)))
            {
                var preset = GetSelectedPreset();
                if (preset != null)
                {
                    string path = AssetDatabase.GetAssetPath(preset);
                    AssetDatabase.DeleteAsset(path);
                    LoadPresets();
                    _selectedPresetIndex = -1;
                }
            }
            if (GUILayout.Button("Refresh", GUILayout.Height(25)))
            {
                LoadPresets();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Save Current as New Preset", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            _newPresetName = EditorGUILayout.TextField(_newPresetName, GUILayout.Height(20));
            if (GUILayout.Button("Save", GUILayout.Width(60), GUILayout.Height(20)))
            {
                SaveNewPreset();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void SaveNewPreset()
        {
            if (!AssetDatabase.IsValidFolder(PRESETS_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/CelLookPostProcess", "Presets");
            }

            var newPreset = ScriptableObject.CreateInstance<CelLookPresetAsset>();
            newPreset.preset = new CelLookPreset();
            string presetName = string.IsNullOrEmpty(_newPresetName) ? "New Preset" : _newPresetName;
            newPreset.preset.name = presetName;

            var s = target as CelLookSettings;
            newPreset.preset.LoadFrom(s);

            string sanitizedName = SanitizeFileName(presetName);
            string path = AssetDatabase.GenerateUniqueAssetPath(PRESETS_FOLDER + "/" + sanitizedName + ".asset");
            AssetDatabase.CreateAsset(newPreset, path);
            AssetDatabase.SaveAssets();

            LoadPresets();
            _newPresetName = "My Preset";
        }

        private string SanitizeFileName(string name)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }
            return sb.ToString().Trim();
        }
    }
}
#endif