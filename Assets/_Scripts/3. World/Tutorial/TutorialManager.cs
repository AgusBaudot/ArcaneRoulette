using System.Collections;
using UnityEngine;
using Core;
using Foundation;
using TMPro;
using DG.Tweening;

namespace World
{
    public class TutorialManager : MonoBehaviour
    {
        [Header("UI Feedback Panel")]
        [SerializeField] private CanvasGroup _feedbackPanel;
        [SerializeField] private TextMeshProUGUI _feedbackText;
        [SerializeField] private float _fadeDuration = 0.5f;

        [Header("Tutorial Timings")]
        [SerializeField] private float _stepWaitTimer = 6f; 

        [Header("Room Triggers & Spawns")]
        [SerializeField] private Transform _room1Spawn;
        [SerializeField] private Transform _room3Spawn;
        [SerializeField] private Transform _room4Spawn;
        [SerializeField] private Transform _room3Enemy1Spawn;
        [SerializeField] private Transform _room3Enemy2Spawn;
        
        [Header("Prefabs & Logic")]
        [SerializeField] private TutorialRewardChest _chestPrefab;
        [SerializeField] private Transform _room2ChestSpawn;
        [SerializeField] private Transform _room3ChestSpawn;

        [Header("Room 1 Doors")]
        [SerializeField] private TutorialDoor _room1ExitTo2;

        [Header("Room 2 Doors")]
        [SerializeField] private TutorialDoor _room2EntranceFrom1;
        [SerializeField] private TutorialDoor _room2ExitTo3;

        [Header("Room 3 Doors")]
        [SerializeField] private TutorialDoor _room3EntranceFrom2;
        [SerializeField] private TutorialDoor _room3ExitTo4;

        [Header("Room 4 Doors")]
        [SerializeField] private TutorialDoor _room4EntranceFrom3;
        [SerializeField] private TutorialDoor _room4ExitTo5;

        [Header("Room 5 Doors")]
        [SerializeField] private TutorialDoor _room5EntranceFrom4;

        private int _currentStep = 1;
        private int _currentRoom = 1;
        
        private bool _dummyDefeated = false;
        private int _room3EnemiesDefeated = 0;
        private bool _room2ChestSpawned = false;
        private bool _room3ChestSpawned = false;
        
        private GameObject _player;

        private void Awake()
        {
            _feedbackPanel.alpha = 0f;
            _feedbackPanel.blocksRaycasts = false;
        }

        private void Start()
        {
            _player = FindObjectOfType<PlayerController>().gameObject;
            _player.transform.position = _room1Spawn.position;

            _room1ExitTo2.Close();
            _room2EntranceFrom1.Open(false); _room2ExitTo3.Close(false);
            _room3EntranceFrom2.Open(false); _room3ExitTo4.Close(false);
            _room4EntranceFrom3.Open(false); _room4ExitTo5.Close(false);
            _room5EntranceFrom4.Open(false);

            var proj = Resources.Load<AbilityRuneSO>("Runes/Normal Projectile");
            if (proj != null) GameStateManager.RunState.AddRune(proj, 1);

            UpdateGuidanceText("Welcome to Arcane Roulette!");
            StartCoroutine(Step1Timer());
        }

        private void OnEnable()
        {
            EventBus.Subscribe<SpellCraftingOpenedEvent>(OnCraftingOpened);
            EventBus.Subscribe<SpellCraftingClosedEvent>(OnCraftingClosed);
            EventBus.Subscribe<SpellEquippedEvent>(OnSpellEquipped);
            EventBus.Subscribe<TutorialChestOpenedEvent>(OnChestOpened);
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            GameStateManager.RunState.OnKill += OnEnemyKilled;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SpellCraftingOpenedEvent>(OnCraftingOpened);
            EventBus.Unsubscribe<SpellCraftingClosedEvent>(OnCraftingClosed);
            EventBus.Unsubscribe<SpellEquippedEvent>(OnSpellEquipped);
            EventBus.Unsubscribe<TutorialChestOpenedEvent>(OnChestOpened);
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            if (GameStateManager.RunState != null)
            {
                GameStateManager.RunState.OnKill -= OnEnemyKilled;
            }
        }

        private bool HasProjectileEquipped()
        {
            for (int i = 0; i < 3; i++)
            {
                var slot = GameStateManager.RunState.GetSlot((SlotIndex)i) as SpellInstance;
                if (slot != null && slot.Recipe.Ability != null) return true; 
            }
            return false;
        }

        private bool HasModifiersEquipped()
        {
            for (int i = 0; i < 3; i++)
            {
                var slot = GameStateManager.RunState.GetSlot((SlotIndex)i) as SpellInstance;
                if (slot != null && slot.Recipe.Modifiers != null)
                {
                    int modCount = 0;
                    foreach (var mod in slot.Recipe.Modifiers)
                    {
                        if (mod != null) modCount++;
                    }
                    if (modCount >= 2) return true;
                }
            }
            return false;
        }

        private bool HasUtilityEquipped()
        {
            for (int i = 0; i < 3; i++)
            {
                var slot = GameStateManager.RunState.GetSlot((SlotIndex)i) as SpellInstance;
                if (slot != null && slot.Recipe.Ability != null && 
                   (slot.Recipe.Ability.name.Contains("Dash") || slot.Recipe.Ability.name.Contains("Shield")))
                {
                    return true;
                }
            }
            return false;
        }

        private IEnumerator Step1Timer()
        {
            yield return new WaitForSeconds(_stepWaitTimer);
            SetStep(2, "Use WASD to move. Head through the door to the next room.");
            _room1ExitTo2.Open();
        }

        public void OnRoomEntered(int roomNumber)
        {
            _currentRoom = roomNumber;
            
            if (roomNumber == 2 && _currentStep == 2)
            {
                _room2EntranceFrom1.Close(); 
                
                if (_dummyDefeated) TriggerRoom2Chest();
                else if (HasProjectileEquipped()) SetStep(6, "Use your spell to defeat the Enemy Dummy.");
                else SetStep(3, "Press TAB to open the Spell Crafting Interface.");
            }
            else if (roomNumber == 3 && (_currentStep == 8 || _currentStep == 7))
            {
                _room3EntranceFrom2.Close(); 

                PoolEnemy.Instance.Get(EnemyType.Melee, _room3Enemy1Spawn.position);
                PoolEnemy.Instance.Get(EnemyType.Melee, _room3Enemy2Spawn.position);
                
                if (_room3EnemiesDefeated >= 2 && HasModifiersEquipped()) TriggerRoom3Chest();
                else if (HasModifiersEquipped()) SetStep(10, "Now put your modified spell to the test. Defeat both enemies!");
                else SetStep(9, "Equip the Piercing and DoT Runes alongside your Projectile Rune to modify your spell.");
            }
            else if (roomNumber == 4 && (_currentStep == 12 || _currentStep == 11))
            {
                _room4EntranceFrom3.Close(); 
                
                if (HasUtilityEquipped()) SetStep(15, "Watch out for the Thorn Traps! Use your abilities to get past them safely.");
                else SetStep(13, "Press TAB to open the Spell Crafting Interface.");
            }
            else if (roomNumber == 5 && (_currentStep == 16 || _currentStep == 15))
            {
                _room5EntranceFrom4.Close(); 
                SetStep(17, "Well done! You've completed your training!");
                StartCoroutine(Step17Timer());
            }
        }

        private void OnCraftingOpened(SpellCraftingOpenedEvent _)
        {
            if (_currentStep == 3) 
            {
                if (HasProjectileEquipped()) SetStep(5, "Close the Spell Crafting Interface to return to the game.");
                else SetStep(4, "Click the Projectile Rune in your inventory to equip it.");
            }
            else if (_currentStep == 13)
            {
                SetStep(14, "Equip a Dash or Shield Rune to one of your spell slots.");
            }
        }

        private void OnSpellEquipped(SpellEquippedEvent evt)
        {
            if (_currentStep == 4 && HasProjectileEquipped()) 
                SetStep(5, "Close the Spell Crafting Interface to return to the game.");
            
            if (_currentStep == 14 && HasUtilityEquipped()) 
                SetStep(15, "Watch out for the Thorn Traps! Use your abilities to get past them safely.");
        }

        private void OnCraftingClosed(SpellCraftingClosedEvent _)
        {
            if (_currentStep == 3 || _currentStep == 4 || _currentStep == 5) 
            {
                if (HasProjectileEquipped())
                {
                    if (_dummyDefeated) TriggerRoom2Chest();
                    else SetStep(6, "Use your spell to defeat the Enemy Dummy.");
                }
            }
            else if (_currentStep == 9)
            {
                if (HasModifiersEquipped())
                {
                    if (_room3EnemiesDefeated >= 2) TriggerRoom3Chest();
                    else SetStep(10, "Now put your modified spell to the test. Defeat both enemies!");
                }
            }
            else if (_currentStep == 13 || _currentStep == 14)
            {
                if (HasUtilityEquipped())
                {
                    SetStep(15, "Watch out for the Thorn Traps! Use your abilities to get past them safely.");
                }
            }
        }

        private void OnEnemyKilled(KillContext ctx)
        {
            if (_currentRoom == 2)
            {
                _dummyDefeated = true;
                if ((_currentStep == 6 || _currentStep == 3 || _currentStep == 4 || _currentStep == 5) && HasProjectileEquipped())
                {
                    TriggerRoom2Chest();
                }
            }
            else if (_currentRoom == 3)
            {
                _room3EnemiesDefeated++;
                if (_room3EnemiesDefeated >= 2 && (_currentStep == 10 || _currentStep == 9) && HasModifiersEquipped())
                {
                    TriggerRoom3Chest();
                }
            }
        }

        private void TriggerRoom2Chest()
        {
            if (_room2ChestSpawned) return;
            _room2ChestSpawned = true;
            Instantiate(_chestPrefab, _room2ChestSpawn.position, Quaternion.identity);
            SetStep(7, "A reward chest has appeared! Open it to claim new Runes.");
        }

        private void TriggerRoom3Chest()
        {
            if (_room3ChestSpawned) return;
            _room3ChestSpawned = true;
            Instantiate(_chestPrefab, _room3ChestSpawn.position, Quaternion.identity);
            SetStep(11, "Well done! Open the chest to claim your reward.");
        }

        private void OnChestOpened(TutorialChestOpenedEvent evt)
        {
            if (_currentStep == 7) 
            {
                var piercing = Resources.Load<ModifierRuneSO>("Runes/Piercing");
                var dot = Resources.Load<ModifierRuneSO>("Runes/DoT");
                if (piercing != null) GameStateManager.RunState.AddRune(piercing, 1);
                if (dot != null) GameStateManager.RunState.AddRune(dot, 1);
                
                SetStep(8, "Great! Head through the door to the next room.");
                _room2ExitTo3.Open();
                
                EventBus.Publish<RuneClaimedEvent>(new (piercing.Name));
                EventBus.Publish<RuneClaimedEvent>(new (dot.Name));
            }
            else if (_currentStep == 11) 
            {
                SetStep(12, "You're getting the hang of it! Head to the next room.");
                
                var dash = Resources.Load<AbilityRuneSO>("Runes/Normal Dash");
                var shield = Resources.Load<AbilityRuneSO>("Runes/Normal Shield");
                
                if (dash != null) GameStateManager.RunState.AddRune(dash, 1);
                if (shield != null) GameStateManager.RunState.AddRune(shield, 1);
                
                EventBus.Publish<RuneClaimedEvent>(new (dash.Name));
                EventBus.Publish<RuneClaimedEvent>(new (shield.Name));
                
                _room3ExitTo4.Open();
            }
        }

        public void OnThornTrapPassed() 
        {
            if (_currentStep == 15) 
            {
                SetStep(16, "Nice work! Head through the door to the final room.");
                _room4ExitTo5.Open();
            }
        }

        private IEnumerator Step17Timer()
        {
            var saveSystem = new Meta.SaveLoadSystem();
            var settings = saveSystem.LoadSettings();
            settings.HasCompletedTutorial = true;
            saveSystem.SaveSettings(settings);
            
            yield return new WaitForSeconds(_stepWaitTimer);
            SetStep(18, "Interact with the portal to enter the Lobby. Your adventure awaits!");
        }

        private void SetStep(int step, string text)
        {
            _currentStep = step;
            UpdateGuidanceText(text);
        }

        private void UpdateGuidanceText(string text)
        {
            _feedbackPanel.DOKill();
            _feedbackPanel.DOFade(0f, _fadeDuration).SetUpdate(true).OnComplete(() =>
            {
                _feedbackText.text = text;
                _feedbackPanel.DOFade(1f, _fadeDuration).SetUpdate(true);
            });
        }

        private void OnPlayerDied(PlayerDiedEvent _)
        {
            StartCoroutine(RespawnSequence());
        }

        private IEnumerator RespawnSequence()
        {
            yield return new WaitForSecondsRealtime(2f);

            GameStateManager.RunState.SetHp(GameStateManager.RunState.MaxHp); 

            if (_currentRoom == 3)
            {
                EventBus.Publish(new PlayerTeleportRequestEvent(_room3Spawn.position));
                SetStep(9, "Equip the Piercing and DoT Runes alongside your Projectile Rune to modify your spell.");
            }
            else if (_currentRoom == 4)
            {
                EventBus.Publish(new PlayerTeleportRequestEvent(_room4Spawn.position));
                SetStep(13, "Press TAB to open the Spell Crafting Interface.");
            }
            
            _player.GetComponent<PlayerHealth>().Initialize(Helpers.PlayerStats); 
        }
    }
}