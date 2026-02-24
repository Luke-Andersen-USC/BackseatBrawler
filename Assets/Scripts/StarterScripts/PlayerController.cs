using System;
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
    
    
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _punchAction;
    private InputAction _blockAction;

    private Queue<FrameData> _inputFrameQueue = new();

    private PlayerState _currentState = PlayerState.Idle;
    private float _timeInState;

    private PlayerActionController _actionController;

    private bool _punchPressedThisFrame = false;

    public RoundTimeDisplay RoundTimeDisplay { get; private set; }
    
    public void SetRoundTimeDisplay(RoundTimeDisplay timeDisplay)
    {
        RoundTimeDisplay = timeDisplay;
    }
    
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
            _punchPressedThisFrame = _punchAction.WasPressedThisFrame();
            ReadInput();
        }
        if (rc.CurrentState == RoundController.RoundState.Action)
        {
            ProcessInputQueue();
            UpdateStateLogic();
        }
    }
    private void FixedUpdate()
    {
        /*
        RoundController rc = GameManager.Instance.RoundController;
        if (rc.CurrentState == RoundController.RoundState.Action)
        {
            ProcessInputQueue();
            UpdateStateLogic();
        }\
        */
    }
    
    

    #region Input
    
    public event Action<Vector2> OnMoveInput;
    public event Action OnPunchPressed;
    public event Action<bool> OnBlockChanged;

    private void ReadInput()
    {
        if (GameManager.Instance.RoundController.CurrentState != RoundController.RoundState.InputCollection)
            return;

        FrameData frameData = new()
        {
            Movement = _moveAction.ReadValue<Vector2>(),
            PunchPressed = _punchPressedThisFrame,
            BlockHeld = _blockAction.IsPressed()
        };

        _inputFrameQueue.Enqueue(frameData);
        
        OnMoveInput?.Invoke(frameData.Movement);

        if (frameData.PunchPressed)
            OnPunchPressed?.Invoke();

        OnBlockChanged?.Invoke(frameData.BlockHeld);
    }

    private void ProcessInputQueue()
    {
        if (_inputFrameQueue.Count > 0)
        {
            FrameData frame = _inputFrameQueue.Dequeue();

            DetermineState(frame);
            _actionController.ExecuteActions(frame);
        }
    }

    #endregion

    #region State Logic

    private void DetermineState(FrameData frame)
    {
        if (_currentState == PlayerState.Dead)
            return;
        
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
        // Example: auto-return to idle after punch
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

        //Debug.Log($"Switched to {_currentState}");
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
    }

    #region Setup

    private void SetupInput()
    {
        _playerInput = GetComponent<PlayerInput>();

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
}