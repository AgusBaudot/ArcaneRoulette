using Foundation;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class GameOverUI : MonoBehaviour
    {
        // Not wired to PlayerDiedEvent right now — SceneController.HandlePlayerDeath
        // already owns death → delay → fade → EndRunRequestEvent. This class used to
        // subscribe too and fired instantly, winning the race and killing
        // SceneController's fade before it could play. Build against the sketch below
        // once this becomes the real results screen.
        
    //     [SerializeField] private float _delayBeforeUI = 2.0f; // Let the death animation play
    //     [SerializeField] private AudioEventSO _gameOverStinger;
    //     
    //     private CanvasGroup _canvasGroup;
    //
    //     private void Awake()
    //     {
    //         _canvasGroup = GetComponent<CanvasGroup>();
    //         SetVisibility(false);
    //     }
    //
    //     private void OnEnable()
    //     {
    //         EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
    //     }
    //
    //     private void OnDisable()
    //     {
    //         EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
    //     }
    //
    //     private void OnPlayerDied(PlayerDiedEvent _)
    //     {
    //         StartCoroutine(GameOverSequence());
    //     }
    //
    //     private IEnumerator GameOverSequence()
    //     {
    //         // 1. Wait while the game is still running so particles/animations finish
    //         yield return CoroutineUtils.GetWait(_delayBeforeUI);
    //
    //         // 2. Lock the game state down
    //         Time.timeScale = 0f;
    //         AudioListener.pause = true; // Pause SFX/Ambience
    //         
    //         Helpers.Input.EnableUIInput(); // Revoke player movement
    //
    //         // 3. Play dramatic stinger (using UI/Music bus so it ignores listener pause)
    //         if (_gameOverStinger != null)
    //             EventBus.Publish(new AudioPlayRequest { Event = _gameOverStinger });
    //
    //         // 4. Show the UI
    //         SetVisibility(true);
    //         
    //         // TODO: Poll GameStateManager.RunState here to display "Rooms Cleared", etc.
    //     }
    //
    //     public void OnClickReturnToMenu() // Wired to your UI Button
    //     {
    //         // Clean up overrides before transitioning out
    //         Time.timeScale = 1f;
    //         AudioListener.pause = false;
    //         Helpers.Input.EnablePlayerInput(); // Ensure next scene's player can move
    //
    //         // Delegate teardown to Foundation
    //         EventBus.Publish(new EndRunRequestEvent("MainMenu"));
    //     }
    //
    //     private void SetVisibility(bool isVisible)
    //     {
    //         _canvasGroup.alpha = isVisible ? 1f : 0f;
    //         _canvasGroup.interactable = isVisible;
    //         _canvasGroup.blocksRaycasts = isVisible;
    //     }
    }
}