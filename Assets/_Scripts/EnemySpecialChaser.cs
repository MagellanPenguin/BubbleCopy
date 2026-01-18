using UnityEngine;

public class EnemySpecialChaser : EnemyBase
{
    [Header("Special")]
    public float alwaysChaseSpeed = 3.5f;

    [Tooltip("타겟 갱신 주기 (EnemyBase의 detectInterval보다 크면 여기 값 사용)")]
    public float retargetInterval = 0.15f;

    float retargetTimer;

    Rigidbody2D rb;
    Collider2D col;

    public override void Init(Room room, int playerLayer)
    {
        base.Init(room, playerLayer);

        moveSpeed = alwaysChaseSpeed;
        retargetTimer = 0f;

        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;              // 안전
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    protected override void Update()
    {
        // EnemyBase의 감지(주기적 target 갱신)를 그대로 쓰되,
        // 특수몹은 더 자주 갱신하고 싶으면 아래로 덮어쓴다.
        base.Update();

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f)
        {
            retargetTimer = retargetInterval;
            // EnemyBase에서 이미 target을 갱신하지만, 특수몹은 더 자주 갱신 가능
            // Unity6 최적 감지는 EnemyBase에서 처리하므로 여기선 그냥 재사용
            // (target이 null이면 base.Update에서 채워짐)
        }

        if (!target) return;

        Vector3 p = transform.position;
        Vector3 t = target.position;
        t.z = p.z;

        Vector3 next = Vector3.MoveTowards(p, t, moveSpeed * Time.deltaTime);

        // Rigidbody2D가 있으면 rb로 이동 (트리거/충돌 이벤트 안정)
        if (rb)
            rb.MovePosition(next);
        else
            transform.position = next;
    }
}
