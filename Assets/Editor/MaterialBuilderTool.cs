using System.IO;
using UnityEditor;
using UnityEngine;

public class MaterialBuilderTool : EditorWindow
{
    private DefaultAsset _targetFolder;
    
    private const string SHADER_PATH = "Assets/Shaders/CameraTransparency/DitherLit.shadergraph";

    [MenuItem("Tools/ArcaneRoulette/Material Builder")]
    public static void ShowWindow()
    {
        //Create and dock the window
        GetWindow<MaterialBuilderTool>("Material Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Arcane Roulette - Material Auto-Builder", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "1. Drop the folder containing your FBX and Textures below.\n" +
            "2. Ensure textures end in _BaseColor, _Normal, _Metallic, _Roughness, _Height, or Emissive.",
            MessageType.Info);

        GUILayout.Space(10);
        
        _targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Asset Folder", _targetFolder, typeof(DefaultAsset), false);

        GUILayout.Space(20);

        GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
        if (GUILayout.Button("Build Material", GUILayout.Height(40)))
        {
            if (_targetFolder == null)
            {
                Debug.LogError("[Material Builder] Please assign a folder first!");
                return;
            }

            BuildMaterialLogic();
        }
        GUI.backgroundColor = Color.white;
    }

    private void BuildMaterialLogic()
    {
        string folderPath = AssetDatabase.GetAssetPath(_targetFolder);

        //Find all textures in the selected folder
        string[] guidList = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        if (guidList.Length == 0)
        {
            Debug.LogError($"[Material Builder] No textures found in folder: {folderPath}");
            return;
        }

        Texture2D baseColor = null, normal = null, metallic = null, roughness = null, height = null, emissive = null;
        string assetName = "UnknownAsset";

        //Sort textures and fix import settings
        foreach (string guid in guidList)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            // Grab the ASSETNAME from the first texture we find (everything before the first '_')
            if (assetName == "UnknownAsset")
                assetName = tex.name.Split('_')[0];

            if (tex.name.EndsWith("_BaseColor")) baseColor = tex;
            else if (tex.name.EndsWith("_Normal"))
            {
                normal = tex;
                FixImportSettings(path, true, false); // Force Normal Map type
            }
            else if (tex.name.EndsWith("_Metallic"))
            {
                metallic = tex;
                FixImportSettings(path, false, true); // Force Linear (sRGB = false)
            }
            else if (tex.name.EndsWith("_Roughness"))
            {
                roughness = tex;
                FixImportSettings(path, false, true); // Force Linear (sRGB = false)
            }
            else if (tex.name.EndsWith("_Height"))
            {
                height = tex;
                FixImportSettings(path, false, true); // Force Linear (sRGB = false)
            }
            else if (tex.name.EndsWith("_Emissive")) emissive = tex;
        }

        //Error Checking for missing maps
        CheckForMissingMap(baseColor, "_BaseColor");
        CheckForMissingMap(normal, "_Normal");
        CheckForMissingMap(metallic, "_Metallic");
        CheckForMissingMap(roughness, "_Roughness");
        CheckForMissingMap(height, "_Height");
        CheckForMissingMap(emissive, "_Emissive");

        //Load the Shader
        Shader customShader = AssetDatabase.LoadAssetAtPath<Shader>(SHADER_PATH);
        if (customShader == null)
        {
            Debug.LogError($"[Material Builder] Could not find shader at {SHADER_PATH}. Please verify the path.");
            return;
        }

        //Create the Material
        string materialName = $"mt_{assetName}.mat";
        string materialPath = Path.Combine(folderPath, materialName).Replace("\\", "/");

        Material newMat = new Material(customShader);

        // Assign Textures
        if (baseColor != null) newMat.SetTexture("_BaseMap", baseColor);
        if (normal != null) newMat.SetTexture("_NormalMap", normal);
        if (metallic != null) newMat.SetTexture("_MetallicMap", metallic);
        if (roughness != null) newMat.SetTexture("_RoughnessMap", roughness);
        if (height != null) newMat.SetTexture("_HeightMap", height);
        if (emissive != null) newMat.SetTexture("_EmissiveMap", emissive);

        // Assign Default Floats and Colors based on your Blackboard
        newMat.SetFloat("_Opacity", 1.0f);
        newMat.SetFloat("_HeightStrength", 0.01f); 
        newMat.SetColor("_EmissiveColor", Color.white); // Default to white so the map shows through

        // Save it to the project
        AssetDatabase.CreateAsset(newMat, materialPath);
        AssetDatabase.SaveAssets();

        // Highlight the new material in the Project window
        EditorGUIUtility.PingObject(newMat);
        Debug.Log($"<color=green>[Material Builder] Successfully created {materialName}!</color>");
    }

    private void FixImportSettings(string path, bool isNormalMap, bool forceLinear)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool needsReimport = false;

        if (isNormalMap && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            needsReimport = true;
        }

        if (forceLinear && importer.sRGBTexture)
        {
            importer.sRGBTexture = false;
            needsReimport = true;
        }

        if (needsReimport)
        {
            importer.SaveAndReimport();
        }
    }

    private void CheckForMissingMap(Texture2D tex, string suffix)
    {
        if (tex == null)
        {
            Debug.LogError($"[Material Builder] Missing map ending in '{suffix}'!");
        }
    }
}