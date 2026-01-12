using UnityEngine;

public class BubbleProjectile : MonoBehaviour
{
    Rigidbody2D rb;
    ObjectPool pool;
    string poolKey;

    AttackProfile profile;
    float dir;
    Vector3 spawnPos;
    float lifeTimer;
    GameObject owner;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    public void Fire(
        AttackProfile p,
        float direction,
        GameObject owner,
        string poolKey,
        ObjectPool pool
    )
    {
        this.profile = p;
        this.dir = Mathf.Sign(direction);
        this.owner = owner;
        this.poolKey = poolKey;
        this.pool = pool;

        spawnPos = transform.position;
        lifeTimer = 0f;

        float s = profile.startScale;
        transform.localScale = Vector3.one * s;

        rb.linearVelocity = new Vector2(profile.speed * dir, profile.riseSpeed);
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

        rb.linearVelocity = new Vector2(profile.speed * dir, profile.riseSpeed);

        float dist = Vector3.Distance(spawnPos, transform.position);
        float t = Mathf.Clamp01(dist / profile.maxDistance);
        float scale = Mathf.Lerp(profile.startScale, profile.endScale, t);
        transform.localScale = Vector3.one * scale;

        if (dist >= profile.maxDistance)
            Pop();
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.collider.CompareTag("Wall"))
        {
            Pop();
            return;
        }

        if (owner != null && c.collider.gameObject == owner)
        {
            float py = owner.transform.position.y;
            float by = transform.position.y;

            var prb = owner.GetComponent<Rigidbody2D>();
            float pvy = prb ? prb.linearVelocity.y : 0f;

            // 아래에서 위로 치면 터짐
            if (py < by && pvy > 0f)
            {
                Pop();
                return;
            }

            // 위에서 밟으면 콩콩
            if (py > by && pvy <= 0f && prb)
            {
                prb.linearVelocity = new Vector2(prb.linearVelocity.x, 0f);
                prb.AddForce(Vector2.up * 12f, ForceMode2D.Impulse);
            }
        }
    }

    void Pop()
    {
        if (pool != null)
            pool.Release(poolKey, gameObject);
        else
            Destroy(gameObject);
    }
}
