using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Foundation;
using Core;

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
}
