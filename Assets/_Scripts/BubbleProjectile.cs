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
    GameObject owner;

    bool rising;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private LayerMask playerMask;

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

        // ✅ Trigger로 통일해서 판정만 사용
        if (col) col.isTrigger = true;
    }

    public void Fire(AttackProfile p, float direction, GameObject owner, string poolKey, ObjectPool pool)
    {
        this.profile = p;
        this.dir = Mathf.Sign(direction);
        this.owner = owner;
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
        int otherLayer = other.gameObject.layer;

        //  Wall이면 즉시 Pop
        if (((1 << otherLayer) & wallMask) != 0)
        {
            Pop();
            return;
        }

        //  Player면 상호작용 (owner만)
        if (owner && other.gameObject == owner && ((1 << otherLayer) & playerMask) != 0)
        {
            HandlePlayerInteraction(owner);
        }
    }

    void HandlePlayerInteraction(GameObject player)
    {
        var prb = player.GetComponent<Rigidbody2D>();
        if (!prb) return;

        float py = player.transform.position.y;
        float by = transform.position.y;
        float pvy = prb.linearVelocity.y;

        // 아래에서 위로 치면 터짐
        if (py < by && pvy > 0f)
        {
            Pop();
            return;
        }

        // 위에서 밟으면 콩콩
        if (py > by && pvy <= 0f)
        {
            prb.linearVelocity = new Vector2(prb.linearVelocity.x, 0f);
            prb.AddForce(Vector2.up * 12f, ForceMode2D.Impulse);
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
