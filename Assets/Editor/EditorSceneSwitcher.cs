using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EditorSceneSwitcher
{
    // & = Alt/Option modifier. 
    // Shift is #, Ctrl/Cmd is % for changing them.
    
    [MenuItem("Tools/Scenes/Main Menu &1")]
    public static void OpenMainMenu() => LoadBuildScene("Main Menu");

    [MenuItem("Tools/Scenes/Lobby &2")]
    public static void OpenLobby() => LoadBuildScene("Lobby");

    [MenuItem("Tools/Scenes/Core Loop &3")]
    public static void OpenCoreLoop() => LoadBuildScene("Core loop");

    private static void LoadBuildScene(string sceneName)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Exit Play mode to switch scenes via shortcuts.");
            return;
        }

        // Prompts to save any unsaved work before switching
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) 
        {
            return;
        }

        // Look up the exact path from Build Settings to avoid hitting stale backup scenes
        foreach (var buildScene in EditorBuildSettings.scenes)
        {
            if (buildScene.path.EndsWith($"/{sceneName}.unity"))
            {
                EditorSceneManager.OpenScene(buildScene.path);
                return;
            }
        }

        Debug.LogError($"Scene '{sceneName}' not found in Build Settings. Please add it via File > Build Settings.");
    }
}