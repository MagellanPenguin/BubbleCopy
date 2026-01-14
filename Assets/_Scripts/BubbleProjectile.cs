using UnityEngine;

public class BubbleProjectile : MonoBehaviour
{
    Rigidbody2D rb;
    Collider2D col;

    ObjectPool pool;
    string poolKey;

    AttackProfile profile;
    float dir;
    Vector3 spawnPos;
    float lifeTimer;

    bool rising;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask wallMask;

    [Header("Pop Effect")]
    [SerializeField] private GameObject popEffectPrefab;

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

        // ✅ groundCheck로만 밟을 거면 Trigger 권장(물리 충돌 불필요)
        if (col) col.isTrigger = true;
    }

    public void Fire(AttackProfile p, float direction, GameObject owner, string poolKey, ObjectPool pool)
    {
        profile = p;
        dir = Mathf.Sign(direction);

        this.poolKey = poolKey;
        this.pool = pool;

        spawnPos = transform.position;
        lifeTimer = 0f;
        rising = false;

        if (col) col.enabled = true;

        transform.localScale = Vector3.one * profile.startScale;

        if (rb)
            rb.linearVelocity = new Vector2(profile.speed * dir, 0f);

        gameObject.SetActive(true);
    }

    void Update()
    {
        if (profile == null) return;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= profile.maxLifetime)
        {
            Pop();
            return;
        }

        float dist = Vector3.Distance(spawnPos, transform.position);

        if (!rising && dist >= profile.maxDistance)
            rising = true;

        if (rb)
        {
            rb.linearVelocity = rising
                ? new Vector2(0f, profile.riseSpeed)
                : new Vector2(profile.speed * dir, 0f);
        }

        float t = Mathf.Clamp01(dist / profile.maxDistance);
        transform.localScale = Vector3.one * Mathf.Lerp(profile.startScale, profile.endScale, t);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 벽 닿으면 Pop만
        if (((1 << other.gameObject.layer) & wallMask) != 0)
        {
            Pop();
        }
    }

    void Pop()
    {
        if (col) col.enabled = false;

        if (popEffectPrefab)
            Instantiate(popEffectPrefab, transform.position, Quaternion.identity);

        if (pool != null) pool.Release(poolKey, gameObject);
        else Destroy(gameObject);
    }
}
