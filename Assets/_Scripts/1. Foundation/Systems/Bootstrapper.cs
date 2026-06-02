using Core;
using Foundation;
using Meta;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class Bootstrapper : MonoBehaviour
{
    private void Awake()
    {
        InitializeGameManager();
        InitializeUpdateManager();
        InitializeProjFactory();
    }

    private void OnEnable()
    {
        InitializeCheats();
        InitializeCraftingSystem();
        InitializeAudioManager();
    }

    private void InitializeGameManager()
    {
        if (GameStateManager.RunState != null)
            return;

        gameObject.AddComponent<GameStateManager>();
    }

    private void InitializeUpdateManager()
    {
        if (UpdateManager.Instance != null)
            return;

        gameObject.AddComponent<UpdateManager>();
    }

    private void InitializeCheats()
    {
        if (FindObjectOfType<Cheats>() != null)
            return;

        gameObject.AddComponent<Cheats>();
    }

    private void InitializeProjFactory()
    {
        if (ProjectilePrefabFactory.Instance != null)
            return;

        var factoryComponent = gameObject.AddComponent<ProjectilePrefabFactory>();
        var projParent = new GameObject("Projectile Factory");
        factoryComponent.SetContainer(projParent.transform);
    }

    private void InitializeCraftingSystem()
    {
        if (FindObjectOfType<SpellCrafter>() != null)
            return;

        gameObject.AddComponent<AttunementSystem>();
        gameObject.AddComponent<SpellCrafter>();
        Debug.LogError($"{nameof(gameObject)}: Spell crafter not found! Rune seeder is probably also missing.");
    }

    private void InitializeAudioManager()
    {
        if (AudioManager.Instance != null)
            return;

        var audioPrefab = Resources.Load<GameObject>("AudioManager");
        if (audioPrefab != null)
        {
            Instantiate(audioPrefab).name = "AudioManager";
        }
        else
        {
            Debug.LogWarning(
                "[Bootstrapper] AudioManager prefab not found at Resources/AudioManager. Audio will be silent in this scene. Create the prefab and wire mixer groups in its inspector.");
        }
    }
}