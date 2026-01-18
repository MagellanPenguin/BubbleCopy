using UnityEngine;

public class LightningBubbleReverseShot : MonoBehaviour, IBubblePopHandler
{
    [Header("Reverse Projectile")]
    public GameObject projectilePrefab;     // 반대로 날아갈 투사체 프리팹
    public float projectileSpeed = 10f;
    public float spawnOffset = 0.05f;       // 버블 안에서 겹치지 않게 살짝 띄움

    [Header("Optional - Rotate to dir")]
    public bool rotateToDirection = false;

    public void OnBubblePop(BubbleProjectile bubble, float firedDir)
    {
        if (!projectilePrefab) return;

        // 반대 방향
        float reverse = -Mathf.Sign(firedDir);
        Vector2 d = new Vector2(reverse, 0f);

        Vector3 spawnPos = bubble.transform.position + (Vector3)(d * spawnOffset);

        Quaternion rot = Quaternion.identity;
        if (rotateToDirection)
        {
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            rot = Quaternion.Euler(0, 0, angle);
        }

        GameObject go = Instantiate(projectilePrefab, spawnPos, rot);

        // Rigidbody2D가 있으면 속도로 발사
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = d * projectileSpeed;
            return;
        }

        // 없으면 간단 이동(투사체에 이동 스크립트가 이미 있으면 여기 안 탐)
        var mover = go.GetComponent<SimpleProjectile2D>();
        if (!mover) mover = go.AddComponent<SimpleProjectile2D>();
        mover.Fire(d, projectileSpeed);
    }
}

// 투사체에 Rigidbody2D가 없을 때를 위한 최소 이동
public class SimpleProjectile2D : MonoBehaviour
{
    Vector2 dir;
    float speed;

    public void Fire(Vector2 d, float s)
    {
        dir = d.sqrMagnitude < 0.0001f ? Vector2.right : d.normalized;
        speed = s;
    }

    void Update()
    {
        transform.position += (Vector3)(dir * speed * Time.deltaTime);
    }
}
