#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class KoreanFontAssetCreator
{
    private const string SourceFontPath = "Assets/Fonts/NotoSansKR-VariableFont_wght.ttf";
    private const string OutputFolder = "Assets/Fonts";
    private const string OutputAssetPath = "Assets/Fonts/NotoSansKR_Dynamic_TMP.asset";

    [MenuItem("Tools/What is Justice/Create Korean TMP Font Asset")]
    public static void CreateKoreanTmpFontAsset()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError($"Could not find source font at {SourceFontPath}");
            return;
        }

        if (!Directory.Exists(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.name = "NotoSansKR_Dynamic_TMP";

        AssetDatabase.CreateAsset(fontAsset, OutputAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = fontAsset;
        EditorGUIUtility.PingObject(fontAsset);
        Debug.Log($"Created Korean TMP font asset: {OutputAssetPath}");
    }
}
#endif
