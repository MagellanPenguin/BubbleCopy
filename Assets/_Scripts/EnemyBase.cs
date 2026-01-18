using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Common")]
    public float maxHp = 10f;
    public float moveSpeed = 2.0f;

    [Header("Detection")]
    public float detectRadius = 6.0f;
    public float detectInterval = 0.2f; // 감지 주기(가벼워짐)

    protected float hp;
    protected int playerLayer;
    int playerLayerMask;

    public Room RoomOwner { get; private set; }
    protected Transform target;

    EnemyDeathReporter deathReporter;

    const int MAX_HITS = 4; // 1P/2P면 4면 충분
    readonly Collider2D[] detectBuffer = new Collider2D[MAX_HITS];
    ContactFilter2D contactFilter;
    float detectTimer = 0f;

    public virtual void Init(Room room, int playerLayer)
    {
        RoomOwner = room;
        this.playerLayer = playerLayer;

        playerLayerMask = 1 << playerLayer;

        hp = maxHp;

        deathReporter = GetComponent<EnemyDeathReporter>();
        if (!deathReporter) deathReporter = gameObject.AddComponent<EnemyDeathReporter>();
        deathReporter.BindToRoom(room);

        // ContactFilter 세팅 (레이어 + 트리거 포함 여부)
        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(playerLayerMask);
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = true; // 플레이어 콜라이더가 트리거여도 잡히게(원치 않으면 false)

        detectTimer = 0f; // 시작하자마자 1회 감지
    }

    protected virtual void Update()
    {
        detectTimer -= Time.deltaTime;
        if (detectTimer <= 0f)
        {
            detectTimer = detectInterval;
            target = FindNearestPlayerNonAlloc();
        }
    }

    protected Transform FindNearestPlayerNonAlloc()
    {
        int hitCount = Physics2D.OverlapCircle(
            (Vector2)transform.position,
            detectRadius,
            contactFilter,
            detectBuffer
        ); // 결과를 배열에 채움 (배열 크기만큼만)

        if (hitCount <= 0) return null;

        float best = float.MaxValue;
        Transform bestT = null;

        for (int i = 0; i < hitCount; i++)
        {
            var c = detectBuffer[i];
            if (!c) continue;

            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = c.transform;
            }
        }

        return bestT;
    }

    public virtual void TakeDamage(float dmg)
    {
        if (dmg <= 0f) return;
        hp -= dmg;
        if (hp <= 0f) Die();
    }

    protected virtual void Die()
    {
        deathReporter?.ReportDead();
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
