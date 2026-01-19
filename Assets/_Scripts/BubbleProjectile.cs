using UnityEngine;

public class BubbleProjectile : MonoBehaviour
{
    Rigidbody2D rb;
    Collider2D col;

    ObjectPool pool;
    string poolKey;

    AttackProfile profile;
    float dir;                 // +1 / -1
    Vector3 spawnPos;
    float lifeTimer;

    bool rising;
    bool popped = false;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask wallMask;

    [Header("Pop Effect")]
    [SerializeField] private GameObject popEffectPrefab;

    [Header("Capture")]
    [SerializeField] private bool stopScaleWhenCaptured = true;
    [SerializeField] private bool stopHorizontalWhenCaptured = true;
    [SerializeField] private bool popOnWallWhenCaptured = false;
    [SerializeField] private float popBlockAfterCapture = 0.15f;
    CapturableMonster captured;
    float capturedTime = -999f;

    [Header("Bubble Visuals")]
    [SerializeField] private GameObject normalBubbleVisual;
    [SerializeField] private GameObject capturedBubbleVisual;

    [Header("Player Pop (Under hit only)")]
    [SerializeField] private string playerLayerName = "Player";
    [SerializeField] private float playerPopMinUpSpeed = 0.1f;
    [SerializeField] private float playerMustBeBelowMargin = 0.05f;
    int playerLayer;

    public float FiredDir => dir;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (rb)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.simulated = true;
        }

        if (col) col.isTrigger = true;

        playerLayer = LayerMask.NameToLayer(playerLayerName);
        SetBubbleVisual(false);
    }

    public void Fire(AttackProfile p, float direction, GameObject owner, string poolKey, ObjectPool pool)
    {
        profile = p;
        dir = Mathf.Sign(direction);
        if (dir == 0f) dir = 1f;

        this.poolKey = poolKey;
        this.pool = pool;

        spawnPos = transform.position;
        lifeTimer = 0f;
        rising = false;
        popped = false;

        captured = null;
        capturedTime = -999f;

        if (col) col.enabled = true;

        transform.localScale = Vector3.one * profile.startScale;

        if (rb)
            rb.linearVelocity = new Vector2(profile.speed * dir, 0f);

        SetBubbleVisual(false);
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (profile == null) return;

        if (!captured)
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= profile.maxLifetime)
            {
                Pop();
                return;
            }
        }

        float dist = Vector3.Distance(spawnPos, transform.position);

        if (!rising && dist >= profile.maxDistance)
            rising = true;

        if (rb)
        {
            if (captured && stopHorizontalWhenCaptured)
                rb.linearVelocity = new Vector2(0f, profile.riseSpeed);
            else
                rb.linearVelocity = rising
                    ? new Vector2(0f, profile.riseSpeed)
                    : new Vector2(profile.speed * dir, 0f);
        }

        if (!(captured && stopScaleWhenCaptured))
        {
            float t = Mathf.Clamp01(dist / profile.maxDistance);
            transform.localScale = Vector3.one * Mathf.Lerp(profile.startScale, profile.endScale, t);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1) Monster capture
        if (!captured)
        {
            var cap = other.GetComponentInParent<CapturableMonster>();
            if (cap != null && !cap.IsCaptured)
            {
                NotifyHit(other, BubbleHitType.MonsterCapture);

                cap.CaptureTo(transform);
                captured = cap;
                capturedTime = Time.time;
                SetBubbleVisual(true);
                return;
            }
        }

        // 2) Player under-hit triggers pop (with capture block time)
        if (other.gameObject.layer == playerLayer)
        {
            if (captured && Time.time < capturedTime + popBlockAfterCapture)
                return;

            if (IsPlayerUnderHit(other))
            {
                NotifyHit(other, BubbleHitType.PlayerUnderHit);
                Pop();
            }
            return;
        }

        // 3) Wall
        if (((1 << other.gameObject.layer) & wallMask) != 0)
        {
            if (captured && !popOnWallWhenCaptured)
                return;

            NotifyHit(other, BubbleHitType.Wall);
            Pop();
        }
    }

    void NotifyHit(Collider2D other, BubbleHitType type)
    {
        var list = GetComponents<MonoBehaviour>();
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] is IBubbleHitHandler h)
                h.OnBubbleHit(this, other, type);
        }
    }

    bool IsPlayerUnderHit(Collider2D playerCol)
    {
        float playerY = playerCol.bounds.center.y;
        float bubbleY = col ? col.bounds.center.y : transform.position.y;

        if (playerY >= bubbleY - playerMustBeBelowMargin)
            return false;

        var prb = playerCol.attachedRigidbody;
        if (!prb) return false;

        return prb.linearVelocity.y > playerPopMinUpSpeed;
    }

    void SetBubbleVisual(bool capturedState)
    {
        if (normalBubbleVisual) normalBubbleVisual.SetActive(!capturedState);
        if (capturedBubbleVisual) capturedBubbleVisual.SetActive(capturedState);
    }

    void Pop()
    {
        if (popped) return;
        popped = true;

        if (col) col.enabled = false;

        // Pop hook (reverse shot, etc.)
        var list = GetComponents<MonoBehaviour>();
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] is IBubblePopHandler p)
                p.OnBubblePop(this, dir);
        }

        // kill captured enemy on pop
        if (captured)
        {
            var enemy = captured.GetComponent<EnemyBase>();
            if (enemy) enemy.TakeDamage(999999f);
            else Destroy(captured.gameObject);

            captured = null;
        }

        if (popEffectPrefab)
            Instantiate(popEffectPrefab, transform.position, Quaternion.identity);

        if (pool != null) pool.Release(poolKey, gameObject);
        else Destroy(gameObject);
    }
}
