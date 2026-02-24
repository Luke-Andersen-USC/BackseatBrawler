using System.Collections;
using UnityEngine;

public class PlayerActionController : MonoBehaviour
{
    private enum VisualPose
    {
        Idle,
        Blocking,
        Punching,
        Dazed
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Combat")]
    [SerializeField] private float punchRange = 1.2f;
    [SerializeField] private float visualSmoothing = 12f;
    [SerializeField] private float animationFps = 3.33f;

    [Header("Hit Reaction")]
    [SerializeField] private float hitReactionDuration = 0.5f;
    [SerializeField] private Color hitReactionColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float hitKnockbackDistance = 0.4f;
    [SerializeField] private int hitFlashCount = 3;
    [SerializeField] private float hitShakeIntensity = 0.06f;

    private Vector3 _baseScale;
    private float _facingDirection = 1f;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private PlayerController _owner;

    private Sprite[] _idleSprites;
    private Sprite[] _blockSprites;
    private Sprite[] _punchRightSprites;
    private Sprite[] _punchLeftSprites; 
    private Sprite[] _activePunchSprites;
    private Sprite[] _dazedSprites;

    private bool _lastPunchWasRight;

    private Coroutine _hitReactionCoroutine;
    private bool _isHitReacting;
    private Vector3 _hitShakeOffset;

    public bool IsBlocking { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
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
        AssignFighterSprites();
    }

    private void Update()
    {
        FaceOpponent();
        UpdateVisualFeedback();
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

    public void PlayHitReaction()
    {
        if (!isActiveAndEnabled)
            return;

        if (_hitReactionCoroutine != null)
        {
            StopCoroutine(_hitReactionCoroutine);
            _hitReactionCoroutine = null;
        }

        _hitReactionCoroutine = StartCoroutine(HitReactionRoutine());
    }

    public void ResetPunchPose()
    {
        _lastPunchWasRight = !_lastPunchWasRight;
        _activePunchSprites = _lastPunchWasRight ? _punchRightSprites : _punchLeftSprites;

        if (_sr != null && _idleSprites != null && _idleSprites.Length > 0)
        {
            _sr.sprite = _idleSprites[0];
        }

        Vector3 resetScale = _baseScale;
        resetScale.x = Mathf.Abs(resetScale.x) * _facingDirection;
        transform.localScale = resetScale;
    }

    public void StartNewPunch()
    {
        _lastPunchWasRight = true;
        _activePunchSprites = _punchRightSprites;
    }

    private IEnumerator HitReactionRoutine()
    {
        _isHitReacting = true;

        PlayerController opponent = _owner != null ? _owner.GetOpponent() : null;
        if (opponent != null)
        {
            float knockDir = Mathf.Sign(transform.position.x - opponent.transform.position.x);
            if (Mathf.Approximately(knockDir, 0f)) knockDir = 1f;
            transform.position += new Vector3(knockDir * hitKnockbackDistance, 0f, 0f);
        }

        float elapsed = 0f;
        float flashInterval = hitReactionDuration / (hitFlashCount * 2f);
        int flashStep = 0;
        int totalFlashSteps = hitFlashCount * 2;

        while (elapsed < hitReactionDuration)
        {
            float shakeDecay = 1f - (elapsed / hitReactionDuration);
            _hitShakeOffset = new Vector3(
                Random.Range(-hitShakeIntensity, hitShakeIntensity) * shakeDecay,
                Random.Range(-hitShakeIntensity, hitShakeIntensity) * shakeDecay,
                0f
            );

            elapsed += Time.deltaTime;

            int currentStep = Mathf.FloorToInt(elapsed / flashInterval);
            if (currentStep != flashStep && currentStep < totalFlashSteps)
            {
                flashStep = currentStep;
                if (_sr != null) _sr.enabled = (flashStep % 2 == 0);
            }

            yield return null;
        }

        _hitShakeOffset = Vector3.zero;
        if (_sr != null) _sr.enabled = true;
        _isHitReacting = false;
        _hitReactionCoroutine = null;
    }

    private void HandleMovement(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f)
            return;

        float horizontal = input.x;
        transform.position += Time.deltaTime * moveSpeed * new Vector3(horizontal, 0f, 0f);
    }

    private void HandleBlocking(bool blockHeld)
    {
        IsBlocking = blockHeld && IsTargetInRange();
    }

    private void Attack()
    {
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
        if (targetActionController != null)
        {
            targetActionController.PlayHitReaction();
        }
    }

    private bool IsTargetInRange()
    {
        PlayerController target = _owner.GetOpponent();
        return target != null && Vector2.Distance(transform.position, target.transform.position) <= punchRange;
    }

    private void UpdateVisualFeedback()
    {
        if (_owner == null || _sr == null)
            return;

        Vector3 targetScale = _baseScale;
        VisualPose pose = VisualPose.Idle;

        switch (_owner.CurrentState)
        {
            case PlayerController.PlayerState.Moving:
                targetScale = new Vector3(_baseScale.x * 1.05f, _baseScale.y * 0.95f, _baseScale.z);
                break;
            case PlayerController.PlayerState.Blocking:
                targetScale = new Vector3(_baseScale.x * 0.85f, _baseScale.y * 1.08f, _baseScale.z);
                pose = VisualPose.Blocking;
                break;
            case PlayerController.PlayerState.Punching:
                targetScale = new Vector3(_baseScale.x * 1.2f, _baseScale.y * 0.92f, _baseScale.z);
                pose = VisualPose.Punching;
                break;
            case PlayerController.PlayerState.Dazed:
                targetScale = new Vector3(_baseScale.x, _baseScale.y * 0.8f, _baseScale.z);
                pose = VisualPose.Dazed;
                break;
        }

        targetScale.x = Mathf.Abs(targetScale.x) * _facingDirection;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * visualSmoothing);

        _sr.sprite = GetAnimatedPoseSprite(pose);

        if (_isHitReacting)
        {
            _sr.color = hitReactionColor;
        }
        else if (pose == VisualPose.Dazed)
        {
            _sr.color = new Color(1f, 0.85f, 0.35f);
        }
        else
        {
            _sr.color = Color.white;
        }

        if (_isHitReacting)
        {
            _sr.transform.localPosition = _hitShakeOffset;
        }
        else
        {
            _sr.transform.localPosition = Vector3.zero;
        }
    }

    private Sprite GetAnimatedPoseSprite(VisualPose pose)
    {
        Sprite[] frames = pose switch
        {
            VisualPose.Blocking => _blockSprites,
            VisualPose.Punching => _activePunchSprites,
            VisualPose.Dazed => _dazedSprites,
            _ => _idleSprites
        };

        if (frames == null || frames.Length == 0)
            return null;

        int frameIndex = Mathf.FloorToInt(Time.time * animationFps) % frames.Length;
        return frames[frameIndex];
    }

    private void FaceOpponent()
    {
        PlayerController target = _owner != null ? _owner.GetOpponent() : null;
        if (target == null)
            return;

        _facingDirection = target.transform.position.x >= transform.position.x ? 1f : -1f;
    }

    private void AssignFighterSprites()
    {
        bool isP1 = _owner != null && _owner.PlayerIndex == 0;

        _idleSprites = new[]
        {
            BuildSprite(isP1, PoseData.Idle1()),
            BuildSprite(isP1, PoseData.Idle2())
        };
        _blockSprites = new[]
        {
            BuildSprite(isP1, PoseData.Block1()),
            BuildSprite(isP1, PoseData.Block2())
        };
        _punchRightSprites = new[]
        {
            BuildSprite(isP1, PoseData.PunchRight1()),
            BuildSprite(isP1, PoseData.PunchRight2())
        };
        _punchLeftSprites = new[]
        {
            BuildSprite(isP1, PoseData.PunchLeft1()),
            BuildSprite(isP1, PoseData.PunchLeft2())
        };
        _dazedSprites = new[]
        {
            BuildSprite(isP1, PoseData.Dazed1()),
            BuildSprite(isP1, PoseData.Dazed2())
        };

        _lastPunchWasRight = false; 
        _activePunchSprites = _punchRightSprites;

        _sr.sprite = (_idleSprites != null && _idleSprites.Length > 0) ? _idleSprites[0] : null;
    }

    #region Sprite Construction

    private struct BodyPartRect
    {
        public int X, Y, W, H;
        public BodyPartRect(int x, int y, int w, int h) { X = x; Y = y; W = w; H = h; }
    }

    private struct PoseLayout
    {
        public BodyPartRect Head;
        public BodyPartRect Body;
        public BodyPartRect LeadArm;   
        public BodyPartRect RearArm;   
        public BodyPartRect LeftLeg;
        public BodyPartRect RightLeg;
        public BodyPartRect LeftEye;
        public BodyPartRect RightEye;
    }

    private static class PoseData
    {
        public static PoseLayout Idle1() => new()
        {
            Head     = new BodyPartRect(10, 21, 12, 8),
            Body     = new BodyPartRect(9, 9, 14, 12),
            LeadArm  = new BodyPartRect(23, 10, 3, 8),
            RearArm  = new BodyPartRect(6, 10, 3, 8),
            LeftLeg  = new BodyPartRect(10, 2, 5, 7),
            RightLeg = new BodyPartRect(17, 2, 5, 7),
            LeftEye  = new BodyPartRect(13, 23, 2, 2),
            RightEye = new BodyPartRect(17, 23, 2, 2),
        };

        public static PoseLayout Idle2() => new()
        {
            Head     = new BodyPartRect(10, 22, 12, 8),
            Body     = new BodyPartRect(9, 10, 14, 12),
            LeadArm  = new BodyPartRect(23, 11, 3, 8),
            RearArm  = new BodyPartRect(6, 11, 3, 8),
            LeftLeg  = new BodyPartRect(10, 3, 5, 7),
            RightLeg = new BodyPartRect(17, 3, 5, 7),
            LeftEye  = new BodyPartRect(13, 24, 2, 2),
            RightEye = new BodyPartRect(17, 24, 2, 2),
        };

        public static PoseLayout Block1() => new()
        {
            Head     = new BodyPartRect(10, 20, 12, 8),
            Body     = new BodyPartRect(9, 8, 14, 12),
            LeadArm  = new BodyPartRect(22, 15, 4, 7),
            RearArm  = new BodyPartRect(21, 9, 4, 7),
            LeftLeg  = new BodyPartRect(10, 1, 5, 7),
            RightLeg = new BodyPartRect(17, 1, 5, 7),
            LeftEye  = new BodyPartRect(13, 22, 2, 2),
            RightEye = new BodyPartRect(17, 22, 2, 2),
        };

        public static PoseLayout Block2() => new()
        {
            Head     = new BodyPartRect(10, 20, 12, 8),
            Body     = new BodyPartRect(9, 8, 14, 12),
            LeadArm  = new BodyPartRect(22, 14, 4, 7),
            RearArm  = new BodyPartRect(21, 10, 4, 7),
            LeftLeg  = new BodyPartRect(10, 1, 5, 7),
            RightLeg = new BodyPartRect(17, 1, 5, 7),
            LeftEye  = new BodyPartRect(13, 22, 2, 2),
            RightEye = new BodyPartRect(17, 22, 2, 2),
        };

        public static PoseLayout PunchRight1() => new()
        {
            Head     = new BodyPartRect(9, 21, 12, 8),
            Body     = new BodyPartRect(8, 9, 14, 12), 
            LeadArm  = new BodyPartRect(22, 13, 9, 4), 
            RearArm  = new BodyPartRect(5, 8, 3, 6),   
            LeftLeg  = new BodyPartRect(9, 2, 5, 7),
            RightLeg = new BodyPartRect(16, 2, 5, 7),
            LeftEye  = new BodyPartRect(12, 23, 2, 2),
            RightEye = new BodyPartRect(16, 23, 2, 2),
        };

        public static PoseLayout PunchRight2() => new()
        {
            Head     = new BodyPartRect(8, 21, 12, 8),
            Body     = new BodyPartRect(7, 9, 14, 12),  
            LeadArm  = new BodyPartRect(21, 14, 10, 4), 
            RearArm  = new BodyPartRect(4, 7, 3, 6),    
            LeftLeg  = new BodyPartRect(8, 2, 5, 7),
            RightLeg = new BodyPartRect(15, 2, 5, 7),
            LeftEye  = new BodyPartRect(11, 23, 2, 2),
            RightEye = new BodyPartRect(15, 23, 2, 2),
        };

        public static PoseLayout PunchLeft1() => new()
        {
            Head     = new BodyPartRect(11, 21, 12, 8),
            Body     = new BodyPartRect(10, 9, 11, 12),    // narrower body = turned sideways
            LeadArm  = new BodyPartRect(21, 15, 9, 4),     // LEFT arm: crosses over, extends right
            RearArm  = new BodyPartRect(21, 8, 3, 6),      // RIGHT arm: dropped low at side
            LeftLeg  = new BodyPartRect(11, 2, 5, 7),
            RightLeg = new BodyPartRect(16, 2, 5, 7),
            LeftEye  = new BodyPartRect(14, 23, 2, 2),
            RightEye = new BodyPartRect(18, 23, 2, 2),
        };

        public static PoseLayout PunchLeft2() => new()
        {
            Head     = new BodyPartRect(12, 21, 12, 8),
            Body     = new BodyPartRect(11, 9, 11, 12),    // narrower = more turned
            LeadArm  = new BodyPartRect(22, 16, 9, 4),     // LEFT arm: max cross extension
            RearArm  = new BodyPartRect(22, 7, 3, 6),      // RIGHT arm: fully dropped
            LeftLeg  = new BodyPartRect(12, 2, 5, 7),
            RightLeg = new BodyPartRect(17, 2, 5, 7),
            LeftEye  = new BodyPartRect(15, 23, 2, 2),
            RightEye = new BodyPartRect(19, 23, 2, 2),
        };

        // --- DAZED ---
        public static PoseLayout Dazed1() => new()
        {
            Head     = new BodyPartRect(10, 19, 12, 8),
            Body     = new BodyPartRect(9, 7, 14, 12),
            LeadArm  = new BodyPartRect(23, 6, 3, 7),
            RearArm  = new BodyPartRect(6, 7, 3, 7),
            LeftLeg  = new BodyPartRect(10, 1, 5, 6),
            RightLeg = new BodyPartRect(17, 1, 5, 6),
            LeftEye  = new BodyPartRect(13, 21, 2, 1),
            RightEye = new BodyPartRect(17, 21, 2, 1),
        };

        public static PoseLayout Dazed2() => new()
        {
            Head     = new BodyPartRect(11, 19, 12, 8),
            Body     = new BodyPartRect(10, 7, 14, 12),
            LeadArm  = new BodyPartRect(24, 5, 3, 7),
            RearArm  = new BodyPartRect(7, 6, 3, 7),
            LeftLeg  = new BodyPartRect(11, 1, 5, 6),
            RightLeg = new BodyPartRect(18, 1, 5, 6),
            LeftEye  = new BodyPartRect(14, 21, 2, 1),
            RightEye = new BodyPartRect(18, 21, 2, 1),
        };
    }

    private Sprite BuildSprite(bool isPlayerOne, PoseLayout pose)
    {
        Texture2D tex = new Texture2D(32, 32) { filterMode = FilterMode.Point };

        Color clear  = new Color(0f, 0f, 0f, 0f);
        Color body   = isPlayerOne ? new Color(0.93f, 0.74f, 0.51f) : new Color(0.75f, 0.86f, 0.55f);
        Color accent = isPlayerOne ? new Color(0.84f, 0.24f, 0.28f) : new Color(0.28f, 0.45f, 0.88f);
        Color eye    = new Color(0.1f, 0.1f, 0.1f);
        Color fist   = isPlayerOne ? new Color(0.95f, 0.35f, 0.3f) : new Color(0.35f, 0.55f, 0.95f);

        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
                tex.SetPixel(x, y, clear);

        FillRect(tex, pose.LeftLeg, accent);
        FillRect(tex, pose.RightLeg, accent);
        FillRect(tex, pose.RearArm, accent);
        FillRect(tex, pose.Body, body);
        FillRect(tex, pose.Head, body);
        FillRect(tex, pose.LeadArm, accent);
        FillRect(tex, pose.LeftEye, eye);
        FillRect(tex, pose.RightEye, eye);

        if (pose.LeadArm.W > 5)
        {
            int fistX = pose.LeadArm.X + pose.LeadArm.W - 3;
            int fistY = pose.LeadArm.Y - 1;
            int fistH = Mathf.Min(pose.LeadArm.H + 2, 6);
            FillRect(tex, new BodyPartRect(fistX, fistY, 3, fistH), fist);
        }

        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }

    private static void FillRect(Texture2D texture, BodyPartRect r, Color color)
    {
        for (int x = r.X; x < r.X + r.W && x < texture.width; x++)
        {
            for (int y = r.Y; y < r.Y + r.H && y < texture.height; y++)
            {
                if (x >= 0 && y >= 0)
                    texture.SetPixel(x, y, color);
            }
        }
    }

    private static void FillRect(Texture2D texture, int startX, int startY, int width, int height, Color color)
    {
        FillRect(texture, new BodyPartRect(startX, startY, width, height), color);
    }

    #endregion
}
