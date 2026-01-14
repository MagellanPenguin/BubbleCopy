using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player1Controller : MonoBehaviour, IPlayerRevive
{
    public enum State { Idle, Walk, Jump, Attack, JumpAttack, Sleep, Victory, Die }

    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player1";
    [SerializeField] private string moveActionName = "Player1_Move";
    [SerializeField] private string jumpActionName = "Player1_Jump";
    [SerializeField] private string attackActionName = "Player1_Attack";

    InputAction moveAction;
    InputAction jumpAction;
    InputAction attackAction;

    [Header("Kinematic Move")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Kinematic Jump/Gravity")]
    [SerializeField] private float jumpVelocity = 15f;
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float maxFallSpeed = -25f;
    [SerializeField] private float maxRiseSpeed = 18f;

    [Header("Ground Check (Collider Cast)")]
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private LayerMask groundMask;  // Ground + Wall
    [SerializeField] private LayerMask oneWayMask;  // OneWay
    [SerializeField] private float groundCastDistance = 0.06f;

    [Header("Ceiling Check")]
    [SerializeField] private LayerMask ceilingMask;        // Wall 추천(OneWay X)
    [SerializeField] private float ceilingCastDistance = 0.06f;

    [Header("Idle/Sleep")]
    [SerializeField] private float sleepAfterSeconds = 8f;

    [Header("Invincible")]
    [SerializeField] private float invincibleSeconds = 1.0f;
    [SerializeField] private float blinkInterval = 0.12f;

    [Header("Combat - Bubble Attack")]
    [SerializeField] private AttackProfile currentAttack;
    [SerializeField] private float attackCooldown = 0.25f;

    [Header("Attack Animation Lock")]
    [SerializeField] private float attackAnimLock = 0.12f;
    float attackLockUntil = 0f;

    [Header("Fire Points (Right/Left)")]
    [SerializeField] private Transform firePointR;
    [SerializeField] private Transform firePointL;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb; // Kinematic
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sr;

    [SerializeField] private ObjectPool objectPool;
    [SerializeField] private string bubblePoolKey = "Bubble";

    [Header("Death")]
    [SerializeField] private float dieDisableDelay = 0.6f;

    [Header("GroundCheck Only")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.08f); // 발바닥 박스
    [SerializeField] private float groundSnapSkin = 0.01f;                       // 바닥에 살짝 띄우기
    [SerializeField] private float maxSnapUp = 0.25f;                             // 파고들었을 때 올리는 최대치
    [SerializeField] private LayerMask bubbleMask; // BubbleGround 레이어
    [SerializeField] private float snapMaxDistance = 0.25f;

    const string ANIM_IDLE = "1P_Idle";
    const string ANIM_WALK = "1P_Walk";
    const string ANIM_JUMP = "1P_Jump";
    const string ANIM_JUMPATTACK = "1P_JumpAttack";
    const string ANIM_ATTACK = "1P_Attack";
    const string ANIM_DIE = "1P_Die";
    const string ANIM_VICTORY = "1P_Victory";
    const string ANIM_SLEEP = "1P_Sleep";

    State state = State.Idle;
    bool grounded;
    bool facingRight = true;

    bool inputBound = false;
    float lastInputTime;
    float lastAttackTime;
    bool invincible;

    Vector2 moveInput;

    // ✅ 수동 중력용 y속도
    float vy = 0f;

    RaycastHit2D[] hits = new RaycastHit2D[6];

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        if (!bodyCollider) bodyCollider = GetComponent<Collider2D>();
        if (!objectPool) objectPool = ObjectPool.Instance;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.useFullKinematicContacts = true;

        // ✅ 충돌 안정
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BindInputActions();
    }

    void OnEnable()
    {
        EnableInputs(true);
        lastInputTime = Time.time;

        StartInvincible(invincibleSeconds);
        SetState(State.Idle);
    }

    void OnDisable()
    {
        EnableInputs(false);
    }

    void BindInputActions()
    {
        if (!inputActions) return;
        var map = inputActions.FindActionMap(actionMapName, true);
        moveAction = map.FindAction(moveActionName, true);
        jumpAction = map.FindAction(jumpActionName, true);
        attackAction = map.FindAction(attackActionName, true);
    }

    void EnableInputs(bool enable)
    {
        if (moveAction == null || jumpAction == null || attackAction == null) return;

        if (enable)
        {
            if (inputBound) return;
            moveAction.Enable(); jumpAction.Enable(); attackAction.Enable();

            jumpAction.started += OnJump;
            attackAction.performed += OnAttack;

            inputBound = true;
        }
        else
        {
            if (!inputBound) return;
            jumpAction.started -= OnJump;
            attackAction.performed -= OnAttack;

            moveAction.Disable(); jumpAction.Disable(); attackAction.Disable();
            inputBound = false;
        }
    }

    void Update()
    {
        if (state == State.Die) return;

        moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        if (moveInput.sqrMagnitude > 0.0001f) lastInputTime = Time.time;

        if (state != State.Victory && Time.time - lastInputTime > sleepAfterSeconds)
            SetState(State.Sleep);

        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (state == State.Die) return;

        // 1) 입력
        float x = moveInput.x;

        if (Mathf.Abs(x) > 0.01f)
        {
            if (x > 0 && !facingRight) Flip(true);
            else if (x < 0 && facingRight) Flip(false);
        }

        // 2) 바닥판정
        grounded = IsGrounded();

        // 3) 중력
        if (grounded && vy <= 0f) vy = 0f;
        else
        {
            vy += gravity * Time.fixedDeltaTime;
            vy = Mathf.Clamp(vy, maxFallSpeed, maxRiseSpeed);
        }

        // 4) 이동 적용 (일단 기존대로 속도)
        rb.linearVelocity = new Vector2(x * moveSpeed, vy);

        // 5) 착지 스냅 (상승 중엔 안 함 => 원웨이 머리 걸림 방지)
        SnapToGround();

        // 6) 스냅 후 grounded 갱신
        grounded = IsGrounded();

        // 7) 상태
        if (Time.time >= attackLockUntil)
        {
            if (!grounded)
            {
                if (state != State.JumpAttack) SetState(State.Jump);
            }
            else
            {
                if (Mathf.Abs(x) > 0.01f) SetState(State.Walk);
                else SetState(State.Idle);
            }
        }
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        if (state == State.Die || state == State.Victory) return;

        if (state == State.Sleep)
        {
            lastInputTime = Time.time;
            SetState(State.Idle);
        }

        // 입력 순간 grounded 재검사
        if (!IsGrounded()) return;

        vy = jumpVelocity;
        SetState(State.Jump);
    }

    void OnAttack(InputAction.CallbackContext ctx)
    {
        if (state == State.Die || state == State.Victory) return;

        if (state == State.Sleep)
        {
            lastInputTime = Time.time;
            SetState(State.Idle);
        }

        if (Time.time < lastAttackTime + attackCooldown) return;
        lastAttackTime = Time.time;

        if (!grounded) SetState(State.JumpAttack);
        else SetState(State.Attack);

        attackLockUntil = Time.time + attackAnimLock;

        FireBubble();
    }

    void FireBubble()
    {
        if (!currentAttack || !objectPool) return;

        Transform fp = facingRight ? firePointR : firePointL;
        if (!fp) return;

        var bubbleGO = objectPool.Get(bubblePoolKey, fp.position, Quaternion.identity);
        var proj = bubbleGO.GetComponent<BubbleProjectile>();
        if (proj)
        {
            float d = facingRight ? 1f : -1f;
            proj.Fire(currentAttack, d, owner: gameObject, bubblePoolKey, objectPool);
        }
    }

    bool IsGrounded()
    {
        if (!groundCheck) return false;

        LayerMask mask = groundMask | bubbleMask;
        if (vy <= 0f) mask |= oneWayMask;

        Collider2D col = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, mask);
        return col != null;
    }



    void UpdateAnimation()
    {
        switch (state)
        {
            case State.Idle: PlayAnim(ANIM_IDLE); break;
            case State.Walk: PlayAnim(ANIM_WALK); break;
            case State.Jump: PlayAnim(ANIM_JUMP); break;
            case State.Attack: PlayAnim(ANIM_ATTACK); break;
            case State.JumpAttack: PlayAnim(ANIM_JUMPATTACK); break;
            case State.Sleep: PlayAnim(ANIM_SLEEP); break;
            case State.Victory: PlayAnim(ANIM_VICTORY); break;
            case State.Die: PlayAnim(ANIM_DIE); break;
        }
    }

    void PlayAnim(string name)
    {
        if (!anim) return;
        var st = anim.GetCurrentAnimatorStateInfo(0);
        if (st.IsName(name)) return;
        anim.Play(name, 0, 0f);
    }

    void SetState(State s)
    {
        if (state == s) return;
        state = s;
    }

    void Flip(bool toRight)
    {
        facingRight = toRight;
        if (sr) sr.flipX = !toRight;
    }

    public void Die()
    {
        if (state == State.Die) return;

        SetState(State.Die);
        EnableInputs(false);

        if (bodyCollider) bodyCollider.enabled = false;

        if (InGameUIManager.Instance != null)
            InGameUIManager.Instance.NotifyPlayerDied(1);

        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        yield return new WaitForSeconds(dieDisableDelay);
        gameObject.SetActive(false);
    }

    public void SetVictory(bool on)
    {
        if (state == State.Die) return;
        if (on) SetState(State.Victory);
        else SetState(State.Idle);
    }

    void StartInvincible(float seconds)
    {
        StopCoroutine(nameof(InvincibleRoutine));
        StartCoroutine(InvincibleRoutine(seconds));
    }

    IEnumerator InvincibleRoutine(float seconds)
    {
        invincible = true;
        float t = 0f;

        while (t < seconds)
        {
            t += blinkInterval;
            if (sr) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }

        if (sr) sr.enabled = true;
        invincible = false;
    }

    public void Revive()
    {
        if (bodyCollider) bodyCollider.enabled = true;

        SetState(State.Idle);
        EnableInputs(true);

        lastInputTime = Time.time;
        StartInvincible(invincibleSeconds);

        vy = 0f;
    }

    public void SetAttackProfile(AttackProfile newProfile)
    {
        if (newProfile) currentAttack = newProfile;
    }

    void SnapToGround()
    {
        if (!groundCheck) return;
        if (vy > 0f) return; // 상승 중엔 스냅 금지

        LayerMask mask = groundMask | bubbleMask;
        if (vy <= 0f) mask |= oneWayMask;

        RaycastHit2D hit = Physics2D.BoxCast(
            groundCheck.position,
            groundCheckSize,
            0f,
            Vector2.down,
            snapMaxDistance,
            mask
        );

        if (!hit.collider) return;

        float desiredY = hit.point.y + groundSnapSkin;
        float diff = desiredY - groundCheck.position.y;

        if (diff > 0f && diff <= snapMaxDistance)
        {
            transform.position += new Vector3(0f, diff, 0f);
            vy = 0f;

            var v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
        }
    }
    public void SetControlEnabled(bool enable)
    {
        EnableInputs(enable);

        if (!enable && rb)
        {
            rb.linearVelocity = Vector2.zero;
            // vy 쓰면 같이 0으로(공중/원웨이 꼬임 방지)
            // vy = 0f;
        }
    }


}
