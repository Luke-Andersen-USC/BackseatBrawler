using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActionController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Combat")]
    [SerializeField] private float punchRange = 1.2f;
    [SerializeField] private float attackFlashDuration = 0.2f;
    [SerializeField] private float visualSmoothing = 12f;

    private Vector3 _baseScale;
    private float _facingDirection = 1f;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Camera _camera;
    private PlayerController _owner;

    public bool IsBlocking { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _camera = Camera.main;
        _owner = GetComponent<PlayerController>();
        _baseScale = transform.localScale;
        _facingDirection = Mathf.Sign(_baseScale.x);
        if (Mathf.Approximately(_facingDirection, 0f))
        {
            _facingDirection = 1f;
        }
    }

    private void Start()
    {
        AssignFighterSprite();
    }


    private void Update()
    {
        UpdateVisualFeedback();
        FaceOpponent();
    }

    public void ExecuteActions(FrameData frameData)
    {
        if (_owner == null || !_owner.CanExecuteActions())
        {
            HandleBlocking(false);
            return;
        }

        HandleBlocking(frameData.BlockHeld);
        HandleMovement(frameData.Movement);

        if (frameData.PunchPressed && !IsBlocking)
        {
            Attack();
        }
    }

    #region Movement

    private void HandleMovement(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f)
            return;

        Vector2 move = Vector2.right * input.x;

        _rb.MovePosition(transform.position + Time.fixedDeltaTime * moveSpeed * new Vector3(move.x, 0f, 0f));
    }

    #endregion

    #region Blocking

    private void HandleBlocking(bool blockHeld)
    {
        bool canBlock = blockHeld && IsTargetInRange();
        IsBlocking = canBlock;

        if (canBlock)
        {
            _sr.color = Color.blue;
        }
        else
        {
            _sr.color = Color.white;
        }
    }

    #endregion

    #region Attack

    private void Attack()
    {
        StartCoroutine(PlayAttackFlash());

        PlayerController target = _owner.GetOpponent();
        if (target == null || target.CurrentState == PlayerController.PlayerState.Dead)
        {
            _owner.AddExhaustion(1f);
            return;
        }

        if (!IsTargetInRange())
        {
            _owner.AddExhaustion(1f);
            return;
        }

        PlayerActionController targetActionController = target.GetComponent<PlayerActionController>();

        if (targetActionController != null && targetActionController.IsBlocking)
        {
            _owner.AddExhaustion(2f);
            return;
        }

        target.RegisterHitTaken();
    }

    #endregion

    private bool IsTargetInRange()
    {
        PlayerController target = _owner.GetOpponent();
        if (target == null)
            return false;

        return Vector2.Distance(transform.position, target.transform.position) <= punchRange;
    }

    private IEnumerator PlayAttackFlash()
    {
        _sr.color = Color.red;
        yield return new WaitForSeconds(attackFlashDuration);

        if (!IsBlocking)
        {
            _sr.color = Color.white;
        }
    }


    private Texture2D BuildPixelFighterTexture(bool isPlayerOne)
    {
        Texture2D texture = new Texture2D(32, 32);
        texture.filterMode = FilterMode.Point;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color body = isPlayerOne ? new Color(0.93f, 0.74f, 0.51f) : new Color(0.75f, 0.86f, 0.55f);
        Color accent = isPlayerOne ? new Color(0.84f, 0.24f, 0.28f) : new Color(0.28f, 0.45f, 0.88f);
        Color face = new Color(0.1f, 0.1f, 0.1f);

        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        FillRect(texture, 10, 20, 12, 8, body);
        FillRect(texture, 8, 8, 16, 12, body);
        FillRect(texture, 5, 10, 3, 7, accent);
        FillRect(texture, 24, 10, 3, 7, accent);
        FillRect(texture, 10, 2, 4, 6, accent);
        FillRect(texture, 18, 2, 4, 6, accent);
        FillRect(texture, 12, 22, 2, 2, face);
        FillRect(texture, 18, 22, 2, 2, face);

        texture.Apply();
        return texture;
    }

    private void FillRect(Texture2D texture, int startX, int startY, int width, int height, Color color)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }


    private void UpdateVisualFeedback()
    {
        if (_owner == null || _sr == null)
            return;

        Vector3 targetScale = _baseScale;

        if (_owner.CurrentState == PlayerController.PlayerState.Moving)
        {
            targetScale = new Vector3(_baseScale.x * 1.05f, _baseScale.y * 0.95f, _baseScale.z);
        }
        else if (_owner.CurrentState == PlayerController.PlayerState.Blocking)
        {
            targetScale = new Vector3(_baseScale.x * 0.9f, _baseScale.y * 1.05f, _baseScale.z);
        }
        else if (_owner.CurrentState == PlayerController.PlayerState.Punching)
        {
            targetScale = new Vector3(_baseScale.x * 1.1f, _baseScale.y * 0.9f, _baseScale.z);
        }
        else if (_owner.CurrentState == PlayerController.PlayerState.Dazed)
        {
            targetScale = new Vector3(_baseScale.x, _baseScale.y * 0.8f, _baseScale.z);
            _sr.color = new Color(1f, 0.85f, 0.35f);
        }

        float targetWidth = Mathf.Abs(targetScale.x) * _facingDirection;
        targetScale.x = targetWidth;

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * visualSmoothing);

        if (_owner.CurrentState != PlayerController.PlayerState.Blocking && _owner.CurrentState != PlayerController.PlayerState.Punching && _owner.CurrentState != PlayerController.PlayerState.Dazed)
        {
            _sr.color = Color.Lerp(_sr.color, Color.white, Time.deltaTime * visualSmoothing);
        }
    }

    private void FaceOpponent()
    {
        PlayerController target = _owner != null ? _owner.GetOpponent() : null;
        if (target == null)
            return;

        _facingDirection = target.transform.position.x >= transform.position.x ? 1f : -1f;
    }

    private void AssignFighterSprite()
    {
        PlayerInput input = GetComponent<PlayerInput>();
        int playerIndex = input != null ? input.playerIndex : 0;

        Texture2D fighterTexture = BuildPixelFighterTexture(playerIndex == 0);
        Sprite generatedSprite = Sprite.Create(fighterTexture, new Rect(0, 0, fighterTexture.width, fighterTexture.height),
            new Vector2(0.5f, 0.5f), 32f);
        _sr.sprite = generatedSprite;
    }
}
