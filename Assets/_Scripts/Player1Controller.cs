using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player1Controller : MonoBehaviour, IPlayerRevive
{
    public enum State { Idle, Walk, Jump, JumpAttack, Sleep, Victory, Die }

    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player1";
    [SerializeField] private string moveActionName = "Player1_Move";
    [SerializeField] private string jumpActionName = "Player1_Jump";
    [SerializeField] private string attackActionName = "Player1_Attack";

    InputAction moveAction;
    InputAction jumpAction;
    InputAction attackAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] LayerMask groundMask; 
    [SerializeField] LayerMask oneWayMask;


    [Header("Idle/Sleep")]
    [SerializeField] private float sleepAfterSeconds = 8f;

    [Header("Invincible")]
    [SerializeField] private float invincibleSeconds = 1.0f;
    [SerializeField] private float blinkInterval = 0.12f;

    [Header("Combat - Bubble Attack")]
    [SerializeField] private AttackProfile currentAttack; // 기본 공격(버블)
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackCooldown = 0.25f;

    [Header("Death")]
    [SerializeField] private float dieDisableDelay = 0.6f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D bodyCollider;

    [SerializeField] private ObjectPool objectPool;
    [SerializeField] private string bubblePoolKey = "Bubble";

    [SerializeField] float gravity = -30f;
    [SerializeField] float maxFallSpeed = -20f;

    const string ANIM_IDLE = "1P_Idle";
    const string ANIM_WALK = "1P_Walk";
    const string ANIM_JUMP = "1P_Jump";
    const string ANIM_JUMPATTACK = "1P_JumpAttack";
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

        BindInputActions();
    }

    void OnEnable()
    {
        EnableInputs(true);
        lastInputTime = Time.time;

        // 코인 시작/스테이지 시작 시 깜빡 무적
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

            moveAction.Enable();
            jumpAction.Enable();
            attackAction.Enable();

            jumpAction.performed += OnJump;
            attackAction.performed += OnAttack;

            inputBound = true;
        }
        else
        {
            if (!inputBound) return;

            jumpAction.performed -= OnJump;
            attackAction.performed -= OnAttack;

            moveAction.Disable();
            jumpAction.Disable();
            attackAction.Disable();

            inputBound = false;
        }
    }


    void Update()
    {
        if (state == State.Die) return;

        grounded = IsGrounded();

        Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        // 입력 감지(슬립용)
        if (move.sqrMagnitude > 0.0001f) lastInputTime = Time.time;

        // 슬립
        if (state != State.Victory && Time.time - lastInputTime > sleepAfterSeconds)
        {
            SetState(State.Sleep);
        }

        // 이동 처리 (슬립/빅토리면 이동 X)
        if (state != State.Sleep && state != State.Victory)
        {
            MoveHorizontal(move.x);
        }
        else
        {
            // 슬립/빅토리 중에는 멈춤
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // 상태별 애니
        UpdateAnimation();
        ApplyGravity();

    }

    void MoveHorizontal(float x)
    {
        // 방향전환
        if (Mathf.Abs(x) > 0.01f)
        {
            if (x > 0 && !facingRight) Flip(true);
            else if (x < 0 && facingRight) Flip(false);
        }

        rb.linearVelocity = new Vector2(x * moveSpeed, rb.linearVelocity.y);

        // 상태 전환
        if (!grounded)
        {
            // 점프 중
            if (state != State.JumpAttack) SetState(State.Jump);
        }
        else
        {
            if (Mathf.Abs(x) > 0.01f) SetState(State.Walk);
            else SetState(State.Idle);
        }
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        if (state == State.Die || state == State.Victory) return;

        // 슬립 중 입력오면 깨기
        if (state == State.Sleep) { lastInputTime = Time.time; SetState(State.Idle); }

        if (!grounded) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        SetState(State.Jump);
    }

    void OnAttack(InputAction.CallbackContext ctx)
    {
        if (state == State.Die || state == State.Victory) return;

        // 슬립 중 입력오면 깨기
        if (state == State.Sleep) { lastInputTime = Time.time; SetState(State.Idle); }

        if (Time.time < lastAttackTime + attackCooldown) return;
        lastAttackTime = Time.time;

        // 점프 공격 애니(선택)
        if (!grounded) SetState(State.JumpAttack);

        FireBubble();
    }

    void FireBubble()
    {
        if (!currentAttack || !firePoint || !objectPool) return;

        var bubbleGO = objectPool.Get(
            bubblePoolKey,
            firePoint.position,
            Quaternion.identity
        );

        var proj = bubbleGO.GetComponent<BubbleProjectile>();
        if (proj)
        {
            float dir = facingRight ? 1f : -1f;
            proj.Fire(currentAttack, dir, owner: gameObject, bubblePoolKey, objectPool);
        }
    }



    bool IsGrounded()
    {
        if (!groundCheck) return false;

        // 1) 완전 바닥
        if (Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundMask))
            return true;

        // 2) OneWay는 "떨어지고 있을 때만" 바닥 취급
        if (rb.linearVelocity.y <= 0f)
        {
            if (Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                oneWayMask))
                return true;
        }

        return false;
    }


    void UpdateAnimation()
    {
        switch (state)
        {
            case State.Idle: PlayAnim(ANIM_IDLE); break;
            case State.Walk: PlayAnim(ANIM_WALK); break;
            case State.Jump: PlayAnim(ANIM_JUMP); break;
            case State.JumpAttack: PlayAnim(ANIM_JUMPATTACK); break;
            case State.Sleep: PlayAnim(ANIM_SLEEP); break;
            case State.Victory: PlayAnim(ANIM_VICTORY); break;
            case State.Die: PlayAnim(ANIM_DIE); break;
        }
    }

    void PlayAnim(string name)
    {
        if (!anim) return;
        // 같은 애니 재생 중이면 재시작 안 하게
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
        if (sr)
        {
            sr.flipX = !toRight; // 스프라이트 방향에 따라 반대로면 바꾸기
        }
    }

    // ====== 몬스터 충돌 시 사망 ======
    void OnCollisionEnter2D(Collision2D col)
    {
        if (state == State.Die) return;
        if (invincible) return;

        // 몬스터 판정: 태그 "Monster" 추천
        if (col.collider.CompareTag("Monster"))
        {
            Die();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (state == State.Die) return;
        if (invincible) return;

        if (other.CompareTag("Monster"))
        {
            Die();
        }
    }

    public void Die()
    {
        if (state == State.Die) return;

        SetState(State.Die);
        EnableInputs(false);

        // 충돌 꺼서 죽은 후 이상한 상호작용 방지(선택)
        if (bodyCollider) bodyCollider.enabled = false;

        // UI 라이프 감소
        if (InGameUIManager.Instance != null)
            InGameUIManager.Instance.NotifyPlayerDied(1);

        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        // 죽는 애니 조금 보여주고 비활성화
        yield return new WaitForSeconds(dieDisableDelay);

        // 다시 컨티뉴 전까지 꺼둠
        gameObject.SetActive(false);
    }

    // ====== 스테이지 클리어 시 ======
    public void SetVictory(bool on)
    {
        if (state == State.Die) return;
        if (on) SetState(State.Victory);
        else SetState(State.Idle);
    }

    // ====== 무적 깜빡 ======
    void StartInvincible(float seconds)
    {
        StopCoroutine(nameof(InvincibleRoutine));
        StartCoroutine(InvincibleRoutine(seconds));
    }

    IEnumerator InvincibleRoutine(float seconds)
    {
        invincible = true;
        float t = 0f;

        // 콜라이더는 켜두고, 맞았을 때만 무시하는 방식(요구에 맞게)
        while (t < seconds)
        {
            t += blinkInterval;
            if (sr) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }

        if (sr) sr.enabled = true;
        invincible = false;

        // 다시 충돌 켜기(혹시 죽을 때 껐다가 revive로 켜는 구조면 여기서 X)
    }

    // ====== IPlayerRevive (코인 시작/컨티뉴 시 호출) ======
    public void Revive()
    {
        // 켜졌다고 가정(JoinController가 SetActive(true) 후 호출)
        if (bodyCollider) bodyCollider.enabled = true;

        SetState(State.Idle);
        EnableInputs(true);

        lastInputTime = Time.time;
        StartInvincible(invincibleSeconds);
    }

    // ====== 아이템으로 공격 교체 ======
    public void SetAttackProfile(AttackProfile newProfile)
    {
        if (newProfile) currentAttack = newProfile;
    }
    void ApplyGravity()
    {
        if (grounded && rb.linearVelocity.y <= 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            return;
        }

        float vy = rb.linearVelocity.y + gravity * Time.deltaTime;
        vy = Mathf.Max(vy, maxFallSpeed);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, vy);
    }

}
