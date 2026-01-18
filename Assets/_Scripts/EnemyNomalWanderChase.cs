using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyNormalWanderChase : EnemyBase
{
    [Header("Wander")]
    public float wanderRange = 2.5f;
    public float switchDirTime = 1.2f;

    [Header("Chase Pulse")]
    public float chaseDuration = 0.8f;
    public float pauseDuration = 0.6f;
    public float stopDistance = 0.9f;

    [Header("Custom Gravity")]
    public float gravity = 35f;
    public float maxFallSpeed = 18f;
    public float groundSnap = 0.02f;

    [Header("Checks")]
    public LayerMask groundMask;
    public Transform groundCheck;
    public Transform wallCheck;
    public Transform edgeCheck;
    public float groundRadius = 0.08f;
    public float wallRadius = 0.10f;
    public float edgeRadius = 0.08f;

    [Header("Flip")]
    public SpriteRenderer sr;

    Vector3 origin;
    float wanderTimer;
    int dir = 1;

    enum Mode { Wander, Chase, Pause }
    Mode mode = Mode.Wander;
    float modeTimer = 0f;

    Rigidbody2D rb;
    Collider2D col;

    float vy = 0f;
    bool grounded;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (!sr)
            sr = GetComponentInChildren<SpriteRenderer>();

        if (!rb)
            Debug.LogError($"{name} : Rigidbody2D 없음");

        if (!col)
            Debug.LogError($"{name} : Collider2D 없음");

        if (!sr)
            Debug.LogError($"{name} : SpriteRenderer 없음");
    }

    public override void Init(Room room, int playerLayer)
    {
        base.Init(room, playerLayer);


        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        origin = transform.position;
        wanderTimer = switchDirTime;
        dir = (Random.value < 0.5f) ? -1 : 1;

        mode = Mode.Wander;
        modeTimer = 0f;
        vy = 0f;
    }

    protected override void Update()
    {
        base.Update();

        // target은 EnemyBase에서 감지해주는 걸 그대로 사용한다고 가정
        if (target && mode == Mode.Wander)
        {
            mode = Mode.Chase;
            modeTimer = chaseDuration;
        }

        if (!target && mode != Mode.Wander)
        {
            mode = Mode.Wander;
        }

        switch (mode)
        {
            case Mode.Wander: UpdateDir_Wander(); break;
            case Mode.Chase: UpdateDir_ChasePulse(); break;
            case Mode.Pause: UpdateDir_Pause(); break;
        }
    }

    void FixedUpdate()
    {
        grounded = IsGrounded();

        bool hitWall = HitWall();
        bool noEdge = !HasEdgeGround();

        if (grounded && (hitWall || noEdge))
            dir *= -1;

        // 커스텀 중력
        if (grounded && vy < 0f)
        {
            vy = 0f;
        }
        else
        {
            vy -= gravity * Time.fixedDeltaTime;
            if (vy < -maxFallSpeed) vy = -maxFallSpeed;
        }

        // 여기부터 "이동 적용" 블록만 교체
        Vector2 pos = rb.position;
        float dx = dir * moveSpeed * Time.fixedDeltaTime;
        float dy = vy * Time.fixedDeltaTime;

        Vector2 next = pos;

        // 1) 수평 이동
        next.x += dx;

        // 2) 수직 이동(아래는 레이캐스트로 바닥 잡기)
        if (dy <= 0f)
        {
            Vector2 rayOrigin = groundCheck ? (Vector2)groundCheck.position : (Vector2)transform.position;
            float castDist = Mathf.Abs(dy) + 0.08f;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, castDist, groundMask);

            if (hit.collider != null)
            {
                float bottomToCenter = rb.position.y - col.bounds.min.y;
                next.y = hit.point.y + bottomToCenter + groundSnap;
                vy = 0f;
                grounded = true;
            }
            else
            {
                next.y += dy;
                grounded = false;
            }
        }
        else
        {
            next.y += dy;
        }

        rb.MovePosition(next);
        // 교체 끝

        if (sr) sr.flipX = (dir < 0);
    }


    void UpdateDir_Wander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            wanderTimer = switchDirTime;
            dir = (Random.value < 0.5f) ? -1 : 1;
        }

        // 원점 기준 범위 제한 (벽/절벽은 FixedUpdate에서 처리)
        float nextX = transform.position.x + dir * moveSpeed * Time.deltaTime;
        if (Mathf.Abs(nextX - origin.x) > wanderRange)
        {
            dir *= -1;
        }
    }

    void UpdateDir_ChasePulse()
    {
        if (!target)
        {
            mode = Mode.Wander;
            return;
        }

        modeTimer -= Time.deltaTime;

        float dist = Mathf.Abs(target.position.x - transform.position.x);
        if (dist > stopDistance)
        {
            // 플레이어의 x 위치 기준으로 dir만 바꿈 (실제 이동은 FixedUpdate)
            dir = (target.position.x >= transform.position.x) ? 1 : -1;
        }

        if (modeTimer <= 0f)
        {
            mode = Mode.Pause;
            modeTimer = pauseDuration;
        }
    }

    void UpdateDir_Pause()
    {
        modeTimer -= Time.deltaTime;
        if (modeTimer <= 0f)
        {
            mode = target ? Mode.Chase : Mode.Wander;
            modeTimer = chaseDuration;
        }
    }

    // ------------------------
    // Ground / Wall / Edge 체크
    // ------------------------

    bool IsGrounded()
    {
        if (!groundCheck) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundMask);
    }

    bool HitWall()
    {
        if (!wallCheck) return false;
        return Physics2D.OverlapCircle(wallCheck.position, wallRadius, groundMask);
    }

    bool HasEdgeGround()
    {
        if (!edgeCheck) return true;
        return Physics2D.OverlapCircle(edgeCheck.position, edgeRadius, groundMask);
    }

    void SnapToGround()
    {
        if (!groundCheck) return;

        Vector2 originRay = groundCheck.position;
        RaycastHit2D hit = Physics2D.Raycast(originRay, Vector2.down, groundSnap + 0.2f, groundMask);
        if (!hit.collider) return;

        Bounds b = col.bounds;
        float bottom = b.min.y;
        float offset = hit.point.y - bottom;

        if (offset > -0.001f && offset < 0.2f)
            rb.MovePosition(rb.position + new Vector2(0f, offset));
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (groundCheck) Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        Gizmos.color = Color.red;
        if (wallCheck) Gizmos.DrawWireSphere(wallCheck.position, wallRadius);
        Gizmos.color = Color.yellow;
        if (edgeCheck) Gizmos.DrawWireSphere(edgeCheck.position, edgeRadius);
    }
    void OnEnable()
    {
        // Awake가 혹시 스킵된 상황(도메인 리로드/풀) 방어
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!col) col = GetComponent<Collider2D>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();

        if (!rb) { Debug.LogError($"{name} : Rigidbody2D 없음"); return; }
        if (!col) { Debug.LogError($"{name} : Collider2D 없음"); return; }

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // Init이 안 와도 최소 동작하도록 기본값 세팅
        origin = transform.position;
        wanderTimer = switchDirTime;
        if (dir != 1 && dir != -1) dir = (Random.value < 0.5f) ? -1 : 1;

        mode = Mode.Wander;
        modeTimer = 0f;
        vy = 0f;

        Debug.Log($"{name} OnEnable OK / pos={transform.position}");
    }

}
