using UnityEngine;
using UnityEditor;
using System.IO;

public class RampMapGenerator : EditorWindow
{
    private Gradient rampGradient = new Gradient();
    private int textureWidth = 256;
    private string fileName = "RampMap";
    private const string SAVE_DIRECTORY = "Assets/CelLookPostProcess/Textures/";

    [MenuItem("Tools/Ramp Map Generator")]
    public static void ShowWindow()
    {
        GetWindow<RampMapGenerator>("Ramp Map Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("NPR Ramp Map 生成器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        rampGradient = EditorGUILayout.GradientField("颜色分布 (渐变)", rampGradient);

        GUILayout.Space(10);
        fileName = EditorGUILayout.TextField("保存名称", fileName);

        GUILayout.Space(20);
        if (GUILayout.Button("生成并保存纹理", GUILayout.Height(30)))
        {
            GenerateAndSaveRampMap();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("提示：\n1. 生成的纹理将保存到指定路径的指定名称\n2. 如需边缘有抗锯齿(软边缘过渡)，将渐变色的任意一端 Mode 改为 Fixed", MessageType.Info);
    }

    private void GenerateAndSaveRampMap()
    {
        Texture2D tex = new Texture2D(textureWidth, 4, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;

        for (int x = 0; x < textureWidth; x++)
        {
            float t = (float)x / (textureWidth - 1);
            Color col = rampGradient.Evaluate(t);

            for (int y = 0; y < 4; y++)
            {
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string fullPath = Path.Combine(Application.dataPath, SAVE_DIRECTORY.Replace("Assets/", ""));
        string fullFilePath = fullPath + fileName + ".png";

        string directory = Path.GetDirectoryName(fullFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(fullFilePath, bytes);
        AssetDatabase.Refresh();

        string assetPath = SAVE_DIRECTORY + fileName + ".png";
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Debug.Log($"Ramp Map 生成并保存到: {assetPath}");
    }
}