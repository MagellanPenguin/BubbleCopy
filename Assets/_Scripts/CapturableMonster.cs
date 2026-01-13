using UnityEngine;

public class CapturableMonster : MonoBehaviour
{
    public bool IsCaptured { get; private set; }

    [Header("Disable On Capture")]
    [SerializeField] private MonoBehaviour[] disableBehaviours; // 몬스터 AI/공격 스크립트들 넣기
    [SerializeField] private Collider2D[] disableColliders;     // 몬스터 공격/몸통 콜라이더들(선택)
    [SerializeField] private Rigidbody2D rb;                    // 있으면 넣고, 없으면 자동 탐색

    Transform originalParent;
    Vector3 originalScale;
    RigidbodyType2D originalBodyType;
    bool originalSimulated;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();

        originalParent = transform.parent;
        originalScale = transform.localScale;

        if (rb)
        {
            originalBodyType = rb.bodyType;
            originalSimulated = rb.simulated;
        }
    }

    public void CaptureTo(Transform bubbleTransform)
    {
        if (IsCaptured) return;
        IsCaptured = true;

        // AI/공격 끄기
        if (disableBehaviours != null)
            foreach (var b in disableBehaviours)
                if (b) b.enabled = false;

        if (disableColliders != null)
            foreach (var c in disableColliders)
                if (c) c.enabled = false;

        // 물리 멈추기 (Unity 6+ 방식)
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            rb.bodyType = RigidbodyType2D.Kinematic; 
            rb.simulated = false;                    
        }

        // 버블에 붙이기
        transform.SetParent(bubbleTransform, worldPositionStays: true);
        transform.localPosition = Vector3.zero;
    }

    public void ReleaseFromBubble()
    {
        if (!IsCaptured) return;
        IsCaptured = false;

        // 원래 부모로 복귀
        transform.SetParent(originalParent, true);
        transform.localScale = originalScale;

        // AI/공격 다시 켜기
        if (disableBehaviours != null)
            foreach (var b in disableBehaviours)
                if (b) b.enabled = true;

        if (disableColliders != null)
            foreach (var c in disableColliders)
                if (c) c.enabled = true;

        // 물리 복구 (Unity 6+ 방식)
        if (rb)
        {
            rb.bodyType = originalBodyType;
            rb.simulated = originalSimulated;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
