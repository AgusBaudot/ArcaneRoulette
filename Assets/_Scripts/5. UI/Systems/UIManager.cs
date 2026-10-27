using System.Collections.Generic;
using Foundation;
using World;
using UnityEngine;

namespace UI
{
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Master Canvases (Scene Bound containers)")]
        [Tooltip("Assign the Canvas_Static transform from the scene here.")]
        [SerializeField] private Transform _staticCanvasRoot;

        [Header("Panel Prefabs (Strictly Isolated Assets)")]
        [SerializeField] private SpellCraftingUI _spellCraftingPrefab;
        [SerializeField] private ShopUI _shopPrefab;
        [SerializeField] private PauseMenu _pauseMenuPrefab;
        [SerializeField] private LootSelectionUI _lootSelectionPrefab;
        [SerializeField] private ConsoleUI _consolePrefab;
        [SerializeField] private SettingsUI _settingsPrefab;
        
        [Header("Universal Audio")]
        [SerializeField] private AudioEventSO _menuOpenSound;
        [SerializeField] private AudioEventSO _menuCloseSound;

        // ── Runtime Instances ────────────────────────────────────────────────

        private SpellCraftingUI _spellCraftingInstance;
        private ShopUI _shopInstance;
        private PauseMenu _pauseMenuInstance;
        private LootSelectionUI _lootSelectionInstance;
        private ConsoleUI _consoleInstance;
        private SettingsUI _settingsInstance;

        private BaseUIPanel _currentActivePanel;

        private Stack<BaseUIPanel> _panelHistory = new();

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            _spellCraftingInstance = Instantiate(_spellCraftingPrefab, _staticCanvasRoot);
            _shopInstance = Instantiate(_shopPrefab, _staticCanvasRoot);
            _pauseMenuInstance = Instantiate(_pauseMenuPrefab, _staticCanvasRoot);
            _lootSelectionInstance = Instantiate(_lootSelectionPrefab, _staticCanvasRoot);
            _consoleInstance = Instantiate(_consolePrefab, _staticCanvasRoot);
            _settingsInstance = Instantiate(_settingsPrefab, _staticCanvasRoot);
        }   

        private void OnEnable()
        {
            EventBus.Subscribe<ShopOpenRequestEvent>(HandleShopOpenRequest);
            EventBus.Subscribe<RoomClearEvent>(HandleRoomClear);
            EventBus.Subscribe<SettingsOpenRequestEvent>(HandleSettingsOpenRequest);

            Helpers.Input.OnCraftingMenuPressed += HandleCraftingToggle;
            Helpers.Input.OnPausePressed += HandlePauseToggle;
            Helpers.Input.OnCloseMenu += HandleCancelInput;
            Helpers.Input.OnConsolePressed += HandleConsoleToggle;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ShopOpenRequestEvent>(HandleShopOpenRequest);
            EventBus.Unsubscribe<RoomClearEvent>(HandleRoomClear);
            EventBus.Unsubscribe<SettingsOpenRequestEvent>(HandleSettingsOpenRequest);

            Helpers.Input.OnCraftingMenuPressed -= HandleCraftingToggle;
            Helpers.Input.OnPausePressed -= HandlePauseToggle;
            Helpers.Input.OnCloseMenu -= HandleCancelInput;
            Helpers.Input.OnConsolePressed -= HandleConsoleToggle;
        }

        // ── Triggers ─────────────────────────────────────────────────────────

        private void HandleShopOpenRequest(ShopOpenRequestEvent evt)
        {
            if (_currentActivePanel != null) return;
            
            _shopInstance.Bind(evt.ShopInstance);
            OpenPanel(_shopInstance);
        }

        private void HandleRoomClear(RoomClearEvent evt)
        {
            if (_currentActivePanel != null)
            {
                if (_currentActivePanel == _consoleInstance)
                {
                    CloseCurrentPanel();
                }
                else
                {
                    return;
                }
            }
    
            OpenPanel(_lootSelectionInstance);
        }

        private void HandleCraftingToggle()
        {
            if (_currentActivePanel == null)
                OpenPanel(_spellCraftingInstance);
            else if (_currentActivePanel == _spellCraftingInstance)
                CloseCurrentPanel();
        }

        private void HandlePauseToggle()
        {
            if (_currentActivePanel == null)
                OpenPanel(_pauseMenuInstance);
            else if (_currentActivePanel == _pauseMenuInstance)
                CloseCurrentPanel();
        }

        private void HandleConsoleToggle()
        {
            if (_currentActivePanel == null)
                OpenPanel(_consoleInstance);
            else if (_currentActivePanel == _consoleInstance)
                CloseCurrentPanel();
        }

        private void HandleCancelInput()
        {
            if (_currentActivePanel != null)
            {
                CloseCurrentPanel();
            }
        }

        private void HandleSettingsOpenRequest(SettingsOpenRequestEvent evt)
        {
            OpenPanel(_settingsInstance, true);
        }

        // ── Core Orchestration ───────────────────────────────────────────────

        private void OpenPanel(BaseUIPanel targetPanel, bool keepHistory = false)
        {
            if (_currentActivePanel == targetPanel) return;

            if (_currentActivePanel != null)
            {
                _currentActivePanel.OnCloseRequested -= CloseCurrentPanel;
                _currentActivePanel.Hide();

                if (keepHistory)
                {
                    _panelHistory.Push(_currentActivePanel);
                }
                else
                {
                    _panelHistory.Clear();
                }
            }

            _currentActivePanel = targetPanel;
            _currentActivePanel.OnCloseRequested += CloseCurrentPanel;
            _currentActivePanel.Show();

            if (_panelHistory.Count == 0)
            {
                Time.timeScale = 0f;
                AudioListener.pause = true;

                Helpers.Input.EnableUIInput();
            }

            if (_menuOpenSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = _menuOpenSound });
        }

        private void CloseCurrentPanel()
        {
            if (_currentActivePanel == null) return;

            _currentActivePanel.OnCloseRequested -= CloseCurrentPanel;
            _currentActivePanel.Hide();
            _currentActivePanel = null;

            if (_panelHistory.Count > 0)
            {
                _currentActivePanel = _panelHistory.Pop();
                _currentActivePanel.OnCloseRequested += CloseCurrentPanel;
                _currentActivePanel.Show();
                
                if (_menuCloseSound != null)
                    EventBus.Publish(new AudioPlayRequest { Event = _menuCloseSound });

                return;
            }

            AudioListener.pause = false;
            Time.timeScale = 1f;
            Helpers.Input.EnablePlayerInput();

            if (_menuCloseSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = _menuCloseSound });
        }
    }
}