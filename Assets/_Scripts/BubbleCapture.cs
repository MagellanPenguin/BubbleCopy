using UnityEngine;

public class BubbleCapture : MonoBehaviour
{
    [Header("Bubble Collider must be Trigger")]
    public bool destroyBubbleAfterCapture = false;

    CapturableMonster captured;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        var rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (captured) return; // 이미 누군가 잡았으면 무시

        //  자식 콜라이더 대비
        var cap = other.GetComponentInParent<CapturableMonster>();
        if (!cap) return;

        cap.CaptureTo(transform);
        captured = cap;

        if (destroyBubbleAfterCapture)
            Destroy(gameObject);
    }
}
