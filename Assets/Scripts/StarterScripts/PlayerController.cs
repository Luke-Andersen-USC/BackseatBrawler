using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public struct FrameData
{
    public Vector2 Movement;
    public bool PunchPressed;
    public bool BlockHeld;
}

[RequireComponent(typeof(PlayerActionController))]
public class PlayerController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Moving,
        Blocking,
        Punching,
        Dazed,
        Dead
    }

    [Header("References")]
    [SerializeField] private Transform spawnPoint;

    [Header("Combat Stats")]
    [SerializeField] private float maxExhaustion = 5f;
    [SerializeField] private int maxHitsTaken = 10;

    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _punchAction;
    private InputAction _blockAction;

    private readonly Queue<FrameData> _inputFrameQueue = new();

    private PlayerState _currentState = PlayerState.Idle;
    private float _timeInState;

    private PlayerActionController _actionController;

    private bool _punchPressedThisFrame;
    private bool _stunnedForRound;
    private float _exhaustion;
    private int _hitsTaken;

    private int _playerIndex;

    private void Awake()
    {
        _actionController = GetComponent<PlayerActionController>();
        SetupInput();
    }

    private void Update()
    {
        _timeInState += Time.deltaTime;

        RoundController rc = GameManager.Instance.RoundController;

        if (rc.CurrentState == RoundController.RoundState.InputCollection)
        {
            _punchPressedThisFrame = IsUsingKeyboardFallback() ? GetKeyboardPunchPressed() : _punchAction.WasPressedThisFrame();
            ReadInput();
        }

        if (rc.CurrentState == RoundController.RoundState.Action)
        {
            ProcessInputQueue();
            UpdateStateLogic();
        }
    }

    #region Input

    private void ReadInput()
    {
        if (GameManager.Instance.RoundController.CurrentState != RoundController.RoundState.InputCollection)
            return;

        Vector2 movement = IsUsingKeyboardFallback() ? GetKeyboardMovement() : _moveAction.ReadValue<Vector2>();
        bool blockHeld = IsUsingKeyboardFallback() ? GetKeyboardBlockHeld() : _blockAction.IsPressed();

        FrameData frameData = new()
        {
            Movement = movement,
            PunchPressed = _punchPressedThisFrame,
            BlockHeld = blockHeld
        };

        _inputFrameQueue.Enqueue(frameData);
    }

    private void ProcessInputQueue()
    {
        if (_inputFrameQueue.Count <= 0)
            return;

        FrameData frame = _inputFrameQueue.Dequeue();

        DetermineState(frame);

        if (CanExecuteActions())
        {
            _actionController.ExecuteActions(frame);
        }
    }

    private bool IsUsingKeyboardFallback()
    {
        if (GameManager.KeyboardFallbackEnabled)
            return true;

        return _playerInput != null && _playerInput.currentControlScheme == "Keyboard&Mouse";
    }

    private int GetKeyboardControlSlot()
    {
        PlayerController opponent = GetOpponent();
        if (opponent != null)
        {
            return transform.position.x <= opponent.transform.position.x ? 0 : 1;
        }

        return _playerIndex;
    }

    private Vector2 GetKeyboardMovement()
    {
        if (Keyboard.current == null)
            return Vector2.zero;

        float horizontal = 0f;

        if (GetKeyboardControlSlot() == 0)
        {
            if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed) horizontal += 1f;
        }
        else
        {
            if (Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
        }

        return new Vector2(Mathf.Clamp(horizontal, -1f, 1f), 0f);
    }

    private bool GetKeyboardPunchPressed()
    {
        if (Keyboard.current == null)
            return false;

        if (GetKeyboardControlSlot() == 0)
        {
            return Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        return Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame;
    }

    private bool GetKeyboardBlockHeld()
    {
        if (Keyboard.current == null)
            return false;

        if (GetKeyboardControlSlot() == 0)
        {
            return Keyboard.current.sKey.isPressed || Keyboard.current.leftShiftKey.isPressed;
        }

        return Keyboard.current.downArrowKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
    }

    #endregion

    #region State Logic

    private void DetermineState(FrameData frame)
    {
        if (_currentState == PlayerState.Dead)
            return;

        if (_stunnedForRound)
        {
            SwitchState(PlayerState.Dazed);
            return;
        }

        if (frame.PunchPressed)
            SwitchState(PlayerState.Punching);
        else if (frame.BlockHeld)
            SwitchState(PlayerState.Blocking);
        else if (frame.Movement.sqrMagnitude > 0.01f)
            SwitchState(PlayerState.Moving);
        else
            SwitchState(PlayerState.Idle);
    }

    private void UpdateStateLogic()
    {
        if (_currentState == PlayerState.Punching && _timeInState > 0.25f)
        {
            SwitchState(PlayerState.Idle);
        }
    }

    private void SwitchState(PlayerState newState)
    {
        if (_currentState == newState)
            return;

        _currentState = newState;
        _timeInState = 0f;
    }

    public bool IsStillExecuting()
    {
        return GameManager.Instance.RoundController.CurrentState == RoundController.RoundState.Action
               && _inputFrameQueue.Count > 0;
    }

    #endregion

    public void Reset()
    {
        transform.position = spawnPoint.position;
        _inputFrameQueue.Clear();
        _exhaustion = 0f;
        _stunnedForRound = false;

        if (_currentState != PlayerState.Dead)
        {
            SwitchState(PlayerState.Idle);
        }
    }

    public void AddExhaustion(float amount)
    {
        if (_currentState == PlayerState.Dead)
            return;

        _exhaustion = Mathf.Clamp(_exhaustion + amount, 0f, maxExhaustion);
        if (_exhaustion >= maxExhaustion)
        {
            _stunnedForRound = true;
            SwitchState(PlayerState.Dazed);
        }
    }

    public void RegisterHitTaken()
    {
        if (_currentState == PlayerState.Dead)
            return;

        _hitsTaken++;
        if (_hitsTaken >= maxHitsTaken)
        {
            SwitchState(PlayerState.Dead);
        }
    }

    public bool CanExecuteActions()
    {
        return _currentState != PlayerState.Dead && !_stunnedForRound;
    }

    public PlayerController GetOpponent()
    {
        foreach (PlayerController player in GameManager.Instance.Players)
        {
            if (player != this)
            {
                return player;
            }
        }

        return null;
    }

    #region Setup

    private void SetupInput()
    {
        _playerInput = GetComponent<PlayerInput>();
        _playerIndex = _playerInput != null ? _playerInput.playerIndex : 0;

        var map = _playerInput.actions.FindActionMap("Player", true);

        _moveAction = map.FindAction("Move", true);
        _punchAction = map.FindAction("Punch", true);
        _blockAction = map.FindAction("Block", true);

        _moveAction.Enable();
        _punchAction.Enable();
        _blockAction.Enable();
    }

    #endregion

    public PlayerState CurrentState => _currentState;
    public float Exhaustion => _exhaustion;
    public float MaxExhaustion => maxExhaustion;
    public int HitsTaken => _hitsTaken;
    public int MaxHitsTaken => maxHitsTaken;
}
