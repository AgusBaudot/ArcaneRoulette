using System.IO;
using UnityEditor;
using UnityEngine;

namespace Foundation.Editor
{
    public static class RuneSpriteProcessor
    {
        [MenuItem("Assets/Tools/Trim Sprites", false, 20)]
        public static void ProcessSelectedSprites()
        {
            Texture2D[] selectedTextures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);

            if (selectedTextures.Length == 0)
            {
                Debug.LogWarning("No textures selected. Please select one or more sprites in the Project window.");
                return;
            }

            int processedCount = 0;

            foreach (Texture2D tex in selectedTextures)
            {
                string path = AssetDatabase.GetAssetPath(tex);

                // 1. Ensure the texture is readable
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && !importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }

                // 2. Process the texture
                if (TrimTexture(tex, path))
                {
                    processedCount++;
                }
            }

            // 3. Refresh the Asset Database to show changes
            AssetDatabase.Refresh();
            Debug.Log($"Successfully trimmed {processedCount} sprites to their exact visual bounds.");
        }

        private static bool TrimTexture(Texture2D originalTex, string path)
        {
            Color[] pixels = originalTex.GetPixels();
            int width = originalTex.width;
            int height = originalTex.height;

            int minX = width, maxX = 0, minY = height, maxY = 0;
            bool foundPixel = false;

            // Find the bounding box of non-transparent pixels
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float alpha = pixels[y * width + x].a;
                    if (alpha > 0.01f) // Threshold to ignore near-invisible artifacts
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                        foundPixel = true;
                    }
                }
            }

            if (!foundPixel)
            {
                Debug.LogWarning($"Skipped {originalTex.name} - Texture appears to be completely transparent.");
                return false;
            }

            // Calculate exact dimensions of the actual artwork
            int artWidth = maxX - minX + 1;
            int artHeight = maxY - minY + 1;

            // Create a new pixel array matching ONLY the artwork dimensions
            Color[] newPixels = new Color[artWidth * artHeight];

            // Copy the artwork from the original bounding box directly into the new tight array
            for (int y = 0; y < artHeight; y++)
            {
                for (int x = 0; x < artWidth; x++)
                {
                    Color p = pixels[(minY + y) * width + (minX + x)];
                    newPixels[y * artWidth + x] = p; 
                }
            }

            // Apply to a new texture sized exactly to the artwork and encode to PNG
            Texture2D croppedTex = new Texture2D(artWidth, artHeight, TextureFormat.RGBA32, false);
            croppedTex.SetPixels(newPixels);
            croppedTex.Apply();

            byte[] pngData = croppedTex.EncodeToPNG();
            if (pngData != null)
            {
                File.WriteAllBytes(path, pngData);
                return true;
            }

            return false;
        }

        // Validate that the menu item is only clickable when textures are selected
        [MenuItem("Assets/Tools/Trim Sprites", true)]
        private static bool ProcessSelectedSpritesValidate()
        {
            return Selection.GetFiltered<Texture2D>(SelectionMode.Assets).Length > 0;
        }
    }
}