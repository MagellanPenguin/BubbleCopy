using UnityEngine;

public class LightningBubbleReverseShot : MonoBehaviour, IBubblePopHandler
{
    [Header("Reverse Projectile (on Pop)")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float spawnOffset = 0.05f;

    [Header("Optional")]
    public bool rotateToDirection = false;

    public void OnBubblePop(BubbleProjectile bubble, float firedDir)
    {
        if (!projectilePrefab) return;

        float reverse = -Mathf.Sign(firedDir);
        if (reverse == 0f) reverse = -1f;

        Vector2 d = new Vector2(reverse, 0f);
        Vector3 spawnPos = bubble.transform.position + (Vector3)(d * spawnOffset);

        Quaternion rot = Quaternion.identity;
        if (rotateToDirection)
        {
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            rot = Quaternion.Euler(0, 0, angle);
        }

        GameObject go = Instantiate(projectilePrefab, spawnPos, rot);

        var prb = go.GetComponent<Rigidbody2D>();
        if (prb)
        {
            prb.linearVelocity = d * projectileSpeed;
            return;
        }

        var mover = go.GetComponent<SimpleProjectile2D>();
        if (!mover) mover = go.AddComponent<SimpleProjectile2D>();
        mover.Fire(d, projectileSpeed);
    }
}

public class LightningBubbleHitSpawn : MonoBehaviour, IBubbleHitHandler
{
    [Header("Spawn On Hit")]
    public GameObject hitPrefab;
    public bool spawnOnMonsterCapture = true;
    public bool spawnOnWall = false;
    public bool spawnOnPlayerUnderHit = false;

    [Header("Spawn Options")]
    public Vector3 offset;
    public bool followRotation = false;

    [Header("Spawn Position")]
    public bool useClosestPoint = true; // recommended

    [Header("Anti Double Spawn")]
    public bool spawnOnlyOnce = true;
    bool spawnedOnce = false;

    public void OnBubbleHit(BubbleProjectile bubble, Collider2D other, BubbleHitType hitType)
    {
        if (!hitPrefab) return;
        if (spawnOnlyOnce && spawnedOnce) return;

        if (hitType == BubbleHitType.MonsterCapture && !spawnOnMonsterCapture) return;
        if (hitType == BubbleHitType.Wall && !spawnOnWall) return;
        if (hitType == BubbleHitType.PlayerUnderHit && !spawnOnPlayerUnderHit) return;

        spawnedOnce = true;

        Vector3 pos;
        if (useClosestPoint && other)
            pos = other.ClosestPoint(bubble.transform.position);
        else
            pos = bubble.transform.position;

        pos += offset;

        Quaternion rot = followRotation ? bubble.transform.rotation : Quaternion.identity;
        Instantiate(hitPrefab, pos, rot);
    }
}

// Remove this class if you already have SimpleProjectile2D.cs in your project.
public class SimpleProjectile2D : MonoBehaviour
{
    Vector2 dir;
    float speed;

    [Header("Optional")]
    public float lifeTime = 3f;
    float t;

    public void Fire(Vector2 d, float s)
    {
        dir = d.sqrMagnitude < 0.0001f ? Vector2.right : d.normalized;
        speed = s;
        t = 0f;
    }

    void Update()
    {
        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        if (lifeTime > 0f)
        {
            t += Time.deltaTime;
            if (t >= lifeTime)
                Destroy(gameObject);
        }
    }
}
