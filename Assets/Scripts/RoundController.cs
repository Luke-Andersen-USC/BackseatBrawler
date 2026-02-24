using System.Collections;
using UnityEngine;

public class RoundController : MonoBehaviour
{
    public enum RoundState
    {
        Idle,
        InputCollection,
        Action
    }
    
    [Header("References")]
    [SerializeField] private RoundTimeDisplay p1RoundTimeDisplay;
    [SerializeField] private RoundTimeDisplay p2RoundTimeDisplay;
    
    [Header("Timing")] 
    [SerializeField] private float _idleTime;
    [SerializeField] private float _roundTime;
    
    public RoundState CurrentState { private set; get; } = RoundState.Idle;
    private float _timeInState;

    private bool _isRoundActive = false;
    
    void Update()
    {
        _timeInState += Time.deltaTime;
        
        if (!_isRoundActive)
        {
            StartCoroutine(RoundSequence());
        }
    }

    private IEnumerator RoundSequence()
    {
        foreach (var playerController in GameManager.Instance.Players)
        {
            playerController.RoundTimeDisplay.DisplayHolder.gameObject.SetActive(false);
        }
        
        _isRoundActive = true;
        
        SwitchState(RoundState.Idle);
        yield return GameManager.Instance.UIController.ShowRoundBanner("Input Phase!", _idleTime);
        
        SwitchState(RoundState.InputCollection);
        yield return StartCoroutine(RoundPhase());
        
        SwitchState(RoundState.Idle);
        yield return GameManager.Instance.UIController.ShowRoundBanner("Action Phase!", _idleTime);
        
        SwitchState(RoundState.Action);
        foreach (var playerController in GameManager.Instance.Players)
        {
            playerController.RoundTimeDisplay.DisplayHolder.gameObject.SetActive(true);
        }
        yield return StartCoroutine(RoundPhase());

        foreach (var playerController in GameManager.Instance.Players)
        {
            while (playerController.IsStillExecuting())
            {
                yield return null;
            }
            
            playerController.RoundTimeDisplay.ClearDisplayHolder();
            playerController.RoundTimeDisplay.DisplayHolder.gameObject.SetActive(false);
        }
        
        SwitchState(RoundState.Idle);
        foreach (PlayerController playerController in GameManager.Instance.Players)
        {
            playerController.Reset();
        }
        
        yield return GameManager.Instance.UIController.ShowRoundBanner("Round Finished!", _idleTime);
        
        _isRoundActive = false;
    }

    private IEnumerator RoundPhase()
    {
        float timeElapsed = 0f;
        
        p1RoundTimeDisplay.gameObject.SetActive(true);
        p2RoundTimeDisplay.gameObject.SetActive(true);
        
        while (timeElapsed <= _roundTime)
        {
            float t = _timeInState / _roundTime;
            p1RoundTimeDisplay.UpdateDisplay(t);
            p2RoundTimeDisplay.UpdateDisplay(t);
            
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        
        p1RoundTimeDisplay.gameObject.SetActive(false);
        p2RoundTimeDisplay.gameObject.SetActive(false);

    }
    
    private void SwitchState(RoundState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        _timeInState = 0f;
        
        //Debug.Log($"Switched to {CurrentState}");
    }
    
    
}
