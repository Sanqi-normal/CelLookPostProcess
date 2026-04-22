// CelLookSettingsEditor.cs

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

namespace CelLookPostProcess
{
    [CustomEditor(typeof(CelLookSettings))]
    sealed class CelLookSettingsEditor : VolumeComponentEditor
    {
        private CelLookPresetAsset[] _presets;
        private int _selectedPresetIndex = 0; // 0 = "-- Select --"
        private string _newPresetName = "My Preset";
        private string _presetsFolder;

        private void OnEnable()
        {
            _presetsFolder = FindPresetsFolder();
            LoadPresets();
        }

        private string FindPresetsFolder()
        {
            string[] guids = AssetDatabase.FindAssets("t:CelLookPresetAsset", new[] { "Assets" });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return Path.GetDirectoryName(path);
            }

            guids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("CelLookSettings.cs"))
                {
                    string folder = Path.GetDirectoryName(path);
                    string presetsPath = Path.Combine(folder, "Presets");
                    if (!AssetDatabase.IsValidFolder(presetsPath))
                    {
                        AssetDatabase.CreateFolder(folder, "Presets");
                    }
                    return presetsPath;
                }
            }

            string fallbackPath = "Assets/CelLookPostProcess/Presets";
            if (!AssetDatabase.IsValidFolder(fallbackPath))
            {
                string parent = "Assets/CelLookPostProcess";
                if (!AssetDatabase.IsValidFolder(parent))
                    AssetDatabase.CreateFolder("Assets", "CelLookPostProcess");
                AssetDatabase.CreateFolder(parent, "Presets");
            }
            return fallbackPath;
        }

        private void LoadPresets()
        {
            if (!AssetDatabase.IsValidFolder(_presetsFolder))
            {
                _presetsFolder = FindPresetsFolder();
            }

            string[] guids = AssetDatabase.FindAssets("t:CelLookPresetAsset", new[] { _presetsFolder });
            _presets = new CelLookPresetAsset[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                _presets[i] = AssetDatabase.LoadAssetAtPath<CelLookPresetAsset>(path);
            }

            // 校正 _selectedPresetIndex 防止越界
            int maxIndex = _presets.Length; // 有效范围 0 ~ _presets.Length
            if (_selectedPresetIndex > maxIndex)
                _selectedPresetIndex = 0;
        }

        // 返回当前选中的预设（index=0 为 "-- Select --"，index>=1 为真实预设）
        private CelLookPresetAsset GetSelectedPreset()
        {
            if (_selectedPresetIndex >= 1 && _selectedPresetIndex <= _presets.Length)
                return _presets[_selectedPresetIndex - 1];
            return null;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Cel Look NPR 风格后处理渲染器\n" +
                "调整全局强度(Effect Intensity)以开启效果。\n" +
                "有关参数配置详见README.md",
                MessageType.Info);

            EditorGUILayout.Space(4);

            DrawPresetSection();

            EditorGUILayout.Space(6);
            base.OnInspectorGUI();
        }

        private void DrawPresetSection()
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

            // 构建下拉列表名称，使用 Asset 文件名（name 属性）作为显示名
            int popupCount = _presets.Length + 1;
            string[] presetNames = new string[popupCount];
            presetNames[0] = "-- Select --";
            for (int i = 0; i < _presets.Length; i++)
            {
                presetNames[i + 1] = _presets[i] != null ? _presets[i].name : "(Invalid)";
            }

            // 绘制下拉框，选择后保持显示选中项名称（不重置 index）
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(_selectedPresetIndex, presetNames, GUILayout.Height(25));
            if (EditorGUI.EndChangeCheck())
            {
                _selectedPresetIndex = newIndex;
                if (_selectedPresetIndex >= 1)
                {
                    var preset = GetSelectedPreset();
                    if (preset != null)
                    {
                        var s = target as CelLookSettings;
                        Undo.RecordObject(s, "Apply CelLook Preset");
                        preset.preset.ApplyTo(s);
                        EditorUtility.SetDirty(s);

                        // 将保存名称同步为当前选中预设名，方便直接覆盖保存
                        _newPresetName = preset.name;
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();

            // 删除按钮：删除当前选中预设
            GUI.enabled = GetSelectedPreset() != null;
            if (GUILayout.Button("Delete Selected", GUILayout.Height(25)))
            {
                var preset = GetSelectedPreset();
                if (preset != null)
                {
                    bool confirmed = EditorUtility.DisplayDialog(
                        "Delete Preset",
                        $"确定要删除预设 \"{preset.name}\" 吗？此操作不可撤销。",
                        "删除", "取消");

                    if (confirmed)
                    {
                        string path = AssetDatabase.GetAssetPath(preset);
                        AssetDatabase.DeleteAsset(path);
                        AssetDatabase.SaveAssets();
                        _selectedPresetIndex = 0;
                        LoadPresets();
                        GUIUtility.ExitGUI();
                        return;
                    }
                }
            }
            GUI.enabled = true;

            if (GUILayout.Button("Refresh", GUILayout.Height(25)))
            {
                LoadPresets();
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Save Current Settings as Preset", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            _newPresetName = EditorGUILayout.TextField(_newPresetName, GUILayout.Height(20));
            if (GUILayout.Button("Save", GUILayout.Width(60), GUILayout.Height(20)))
            {
                SavePreset();
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.EndHorizontal();

            // 提示：若与已有预设同名，则会覆盖
            var existingPreset = FindPresetByName(_newPresetName);
            if (existingPreset != null)
            {
                EditorGUILayout.HelpBox($"将覆盖已有预设: {existingPreset.name}", MessageType.Warning);
            }
        }

        // 按 Asset 文件名查找预设（忽略大小写）
        private CelLookPresetAsset FindPresetByName(string presetName)
        {
            string sanitized = SanitizeFileName(presetName);
            foreach (var preset in _presets)
            {
                if (preset != null && string.Equals(preset.name, sanitized, System.StringComparison.OrdinalIgnoreCase))
                    return preset;
            }
            return null;
        }

        private void SavePreset()
        {
            if (!AssetDatabase.IsValidFolder(_presetsFolder))
            {
                _presetsFolder = FindPresetsFolder();
            }

            string presetName = string.IsNullOrEmpty(_newPresetName) ? "New Preset" : _newPresetName;
            string sanitizedName = SanitizeFileName(presetName);
            string assetPath = _presetsFolder + "/" + sanitizedName + ".asset";

            var s = target as CelLookSettings;

            // 检查是否已存在同名预设，存在则覆盖
            var existingAsset = AssetDatabase.LoadAssetAtPath<CelLookPresetAsset>(assetPath);
            if (existingAsset != null)
            {
                Undo.RecordObject(existingAsset, "Overwrite CelLook Preset");
                existingAsset.preset.name = presetName;
                existingAsset.preset.LoadFrom(s);
                EditorUtility.SetDirty(existingAsset);
                AssetDatabase.SaveAssets();
            }
            else
            {
                // 创建新预设
                var newPreset = ScriptableObject.CreateInstance<CelLookPresetAsset>();
                newPreset.preset = new CelLookPreset();
                newPreset.preset.name = presetName;
                newPreset.preset.LoadFrom(s);

                AssetDatabase.CreateAsset(newPreset, assetPath);
                AssetDatabase.SaveAssets();
            }

            LoadPresets();

            // 保存后自动选中该预设
            for (int i = 0; i < _presets.Length; i++)
            {
                if (_presets[i] != null && string.Equals(_presets[i].name, sanitizedName, System.StringComparison.OrdinalIgnoreCase))
                {
                    _selectedPresetIndex = i + 1;
                    break;
                }
            }
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