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
    [SerializeField] private float jumpVelocity = 15f;     // ✅ 기본값 상향(너무 낮은 점프 방지)
    [SerializeField] private float gravity = -24f;         // ✅ 너무 강하면 점프가 짧아짐
    [SerializeField] private float maxFallSpeed = -25f;
    [SerializeField] private float maxRiseSpeed = 18f;

    [Header("Ground Check (Collider Cast)")]
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private LayerMask groundMask;  // Wall + Ground
    [SerializeField] private LayerMask oneWayMask;  // OneWay
    [SerializeField] private float groundCastDistance = 0.05f; // ✅ 조금 줄임(붙어있음 방지)

    [Header("Ceiling Check")]
    [SerializeField] private LayerMask ceilingMask;        // ✅ Wall만 추천(OneWay 절대 X)
    [SerializeField] private float ceilingCastDistance = 0.06f;

    [Header("Kinematic Collision Solve")]
    [SerializeField] private float skin = 0.01f; // 벽/바닥에 살짝 띄우기(끼임/통과 방지)

    [Header("Collision Masks")]
    [SerializeField] private LayerMask wallMask; // ✅ Wall만 넣기(옆/천장 막기용)

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

    [Header("Jump Assist")]
    [SerializeField] private float coyoteTime = 0.08f;     // 바닥 떠난 뒤도 잠깐 점프 허용
    [SerializeField] private float jumpBuffer = 0.10f;     // 점프 입력 저장

    [Header("Depenetration")]
    [SerializeField] private float depenetrationDistance = 0.02f;

    float lastGroundedTime = -999f;
    float lastJumpPressedTime = -999f;

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

    // ✅ 키네틱 물리 상태값(우리가 직접 관리)
    float vy = 0f;

    // Cast buffer
    RaycastHit2D[] castHits = new RaycastHit2D[8];

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

        // ✅ Kinematic 고정
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.useFullKinematicContacts = true;

        // ✅ 더 안정적으로(옵션)
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

            // ✅ 점프는 started(1회) 권장
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
        Depenetrate();

        if (state == State.Die) return;

        // 슬립/빅토리면 정지
        if (state == State.Sleep || state == State.Victory)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float x = moveInput.x;

        // 방향
        if (Mathf.Abs(x) > 0.01f)
        {
            if (x > 0 && !facingRight) Flip(true);
            else if (x < 0 && facingRight) Flip(false);
        }

        // ===== 1) 바닥 체크(프레임 시작) =====
        grounded = IsGrounded();

        // ===== 2) 중력 적용(키네틱) =====
        if (grounded && vy <= 0f)
        {
            vy = 0f;
        }
        else
        {
            vy += gravity * Time.fixedDeltaTime;
            vy = Mathf.Clamp(vy, maxFallSpeed, maxRiseSpeed);
        }

        // ===== 3) 이동량 계산 =====
        Vector2 delta = new Vector2(x * moveSpeed, vy) * Time.fixedDeltaTime;

        // ===== 4) Cast 기반 이동(벽 통과 방지) =====
        MoveWithCasts(delta);

        // ===== 5) 이동 후 바닥 재체크(착지 반영) =====
        grounded = IsGrounded();

        // ===== 6) 상태 전환 (Attack 락 동안 덮어쓰기 금지) =====
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

        if (grounded) lastGroundedTime = Time.time;

        // ✅ 버퍼/코요테로 점프 판정
        bool canJump = (Time.time - lastGroundedTime) <= coyoteTime;
        bool hasJumpBuffered = (Time.time - lastJumpPressedTime) <= jumpBuffer;

        if (hasJumpBuffered && canJump)
        {
            vy = jumpVelocity;
            lastJumpPressedTime = -999f; // 버퍼 소모
            grounded = false;
            SetState(State.Jump);
        }

    }
    void MoveWithCasts(Vector2 delta)
    {
        if (!bodyCollider) return;

        Vector2 pos = rb.position;

        // ---------- 수평(벽만 막기) ----------
        if (Mathf.Abs(delta.x) > 0.00001f)
        {
            Vector2 dir = new Vector2(Mathf.Sign(delta.x), 0f);

            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;

            // ✅ wallMask 비었으면(0) groundMask로 대체(인스펙터 실수 방지)
            LayerMask xMask = (wallMask != 0) ? wallMask : groundMask;

            filter.layerMask = xMask;
            filter.useTriggers = false;

            float want = Mathf.Abs(delta.x);
            float dist = want + skin;

            int cnt = bodyCollider.Cast(dir, filter, castHits, dist);

            float move = want;
            if (cnt > 0)
            {
                float min = float.MaxValue;
                for (int i = 0; i < cnt; i++)
                {
                    // ✅ 바닥(위쪽 노말)을 수평벽으로 오판하는 걸 줄이기:
                    // 벽에 가까운 hit만 채택 (normal.x가 큰 것)
                    if (Mathf.Abs(castHits[i].normal.x) < 0.5f) continue;

                    if (castHits[i].distance < min)
                        min = castHits[i].distance;
                }

                if (min != float.MaxValue)
                    move = Mathf.Max(0f, min - skin);
            }

            pos.x += dir.x * move;
        }

        // ---------- 수직 ----------
        if (Mathf.Abs(delta.y) > 0.00001f)
        {
            Vector2 dir = new Vector2(0f, Mathf.Sign(delta.y));

            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.useTriggers = false;

            if (dir.y > 0f)
            {
                // ✅ 위로: 천장은 wallMask로 막기(원웨이 절대 X)
                filter.layerMask = wallMask;
            }
            else
            {
                // ✅ 아래로: groundMask + (떨어질 때만) 원웨이
                LayerMask mask = groundMask;
                if (vy <= 0f) mask |= oneWayMask;
                filter.layerMask = mask;
            }

            float want = Mathf.Abs(delta.y);
            float dist = want + skin;

            int cnt = bodyCollider.Cast(dir, filter, castHits, dist);

            float move = want;
            if (cnt > 0)
            {
                float min = float.MaxValue;
                for (int i = 0; i < cnt; i++)
                {
                    if (castHits[i].distance < min)
                        min = castHits[i].distance;
                }

                move = Mathf.Max(0f, min - skin);

                // ✅ 막히면 vy 끊기
                if (dir.y > 0f) vy = 0f;
                else if (vy < 0f) vy = 0f;
            }

            pos.y += dir.y * move;
        }

        rb.MovePosition(pos);
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        if (state == State.Die || state == State.Victory) return;

        if (state == State.Sleep)
        {
            lastInputTime = Time.time;
            SetState(State.Idle);
        }

        // ✅ 입력을 저장만 해둠
        lastJumpPressedTime = Time.time;
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
        if (!bodyCollider) return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;

        LayerMask mask = groundMask;

        // ✅ 원웨이: 떨어질 때만 바닥 취급
        if (vy <= 0f)
            mask |= oneWayMask;

        filter.layerMask = mask;
        filter.useTriggers = false;

        int hitCount = bodyCollider.Cast(Vector2.down, filter, castHits, groundCastDistance);
        return hitCount > 0;
    }

    bool HitCeiling()
    {
        if (!bodyCollider) return false;
        if (ceilingMask == 0) return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = ceilingMask;
        filter.useTriggers = false;

        int hitCount = bodyCollider.Cast(Vector2.up, filter, castHits, ceilingCastDistance);
        return hitCount > 0;
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

    // ====== 사망/무적은 기존 유지 ======
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

    void Depenetrate()
    {
        if (!bodyCollider) return;

        // 벽/바닥/원웨이 포함 전부에서 겹침 풀기
        LayerMask allMask = groundMask | oneWayMask | wallMask;

        Collider2D[] overlaps = new Collider2D[8];
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = allMask;
        filter.useTriggers = false;

        int count = bodyCollider.Overlap(filter, overlaps);
        if (count <= 0) return;

        // 가장 흔한 케이스: 벽 안에 살짝 파고든 상태 -> X축으로 빼내기
        // facing 방향 반대로 살짝 밀어내기(간단/실용)
        float push = depenetrationDistance;
        Vector2 p = rb.position;

        // 양쪽 다 시도해서 빠지는 쪽 채택
        if (!WouldOverlapAt(p + Vector2.right * push, filter))
            rb.position = p + Vector2.right * push;
        else if (!WouldOverlapAt(p + Vector2.left * push, filter))
            rb.position = p + Vector2.left * push;
    }

    bool WouldOverlapAt(Vector2 testPos, ContactFilter2D filter)
    {
        Vector2 old = rb.position;
        rb.position = testPos;

        Collider2D[] buf = new Collider2D[4];
        int c = bodyCollider.Overlap(filter, buf);

        rb.position = old;
        return c > 0;
    }

}
