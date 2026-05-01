using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "ScriptableObjects/InputReader")]
public class InputReader : ScriptableObject, PlayerInputActions.IPlayerActions, PlayerInputActions.IUIActions
{
    private PlayerInputActions _inputActions;

    #region Player Events

    //State properties for continuous input
    public Vector2 MoveDirection { get; private set; }
    public Vector2 MousePosition { get; private set; }

    //Events for discrete actions
    public event Action OnSlot0Started;
    public event Action OnSlot0Canceled;
    public event Action OnSlot1Started;
    public event Action OnSlot1Canceled;
    public event Action OnSlot2Started;
    public event Action OnSlot2Canceled;
    public event Action OnCraftingMenuPressed;
    public event Action OnPausePressed;

    #endregion
    
    #region UI Events

    public event Action OnCloseCrafting;
    public event Action OnCarouselLeft;
    public event Action OnCarouselRight;
    public event Action OnToggleTooltip;
    
    #endregion

    private void OnEnable()
    {
        if (_inputActions == null)
        {
            _inputActions = new PlayerInputActions();
            //Tell the generated class to send its callbacks to this ScriptableObject
            _inputActions.Player.SetCallbacks(this);
            _inputActions.UI.SetCallbacks(this);
        }

        //Default state
        EnablePlayerInput();
    }

    private void OnDisable()
    {
        _inputActions.Player.Disable();
        _inputActions.UI.Disable();
    }

    #region Context Switching Methods

    public void EnablePlayerInput()
    {
        _inputActions.UI.Disable();
        _inputActions.Player.Enable();
    }

    public void EnableUIInput()
    {
        _inputActions.Player.Disable();
        MoveDirection = Vector2.zero; //Safety: kill momentum so the player doesn't move while in UI.
        _inputActions.UI.Enable();
    }

    #endregion

    #region Player Map Implementation
    //Player Map Implementation---------------------------
    public void OnMovement(InputAction.CallbackContext context)
    {
        MoveDirection = context.ReadValue<Vector2>();
    }

    public void OnMouseAim(InputAction.CallbackContext context)
    {
        MousePosition = context.ReadValue<Vector2>();
    }

    public void OnSlot0(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
            OnSlot0Started?.Invoke();

        if (context.phase == InputActionPhase.Canceled)
            OnSlot0Canceled?.Invoke();
    }

    public void OnSlot1(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
            OnSlot1Started?.Invoke();

        if (context.phase == InputActionPhase.Canceled)
            OnSlot1Canceled?.Invoke();
    }

    public void OnSlot2(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
            OnSlot2Started?.Invoke();

        if (context.phase == InputActionPhase.Canceled)
            OnSlot2Canceled?.Invoke();
    }

    void PlayerInputActions.IPlayerActions.OnToggleCrafting(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            OnCraftingMenuPressed?.Invoke();
    }

    public void OnPauseGame(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            OnPausePressed?.Invoke();
    }
    #endregion
    
    #region UI Map Implementations

    void PlayerInputActions.IUIActions.OnToggleCrafting(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            OnCloseCrafting?.Invoke();
    }

    void PlayerInputActions.IUIActions.OnCarouselLeft(InputAction.CallbackContext context)
    {
        
        if (context.phase == InputActionPhase.Performed)
            OnCarouselLeft?.Invoke();
    }

    void PlayerInputActions.IUIActions.OnCarouselRight(InputAction.CallbackContext context)
    {
        
        if (context.phase == InputActionPhase.Performed)
            OnCarouselRight?.Invoke();
    }

    void PlayerInputActions.IUIActions.OnToggleTooltip(InputAction.CallbackContext context)
    {
        
        if (context.phase == InputActionPhase.Performed)
            OnToggleTooltip?.Invoke();
    }

    #endregion
}