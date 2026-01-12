using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackProfile")]
public class AttackProfile : ScriptableObject
{
    [Header("Projectile Prefab (must have BubbleProjectile)")]
    public GameObject projectilePrefab;

    [Header("Motion")]
    public float speed = 6f;
    public float riseSpeed = 1.2f;       // 점점 하늘로 올라가는 속도
    public float maxDistance = 7f;       // 최대 사거리

    [Header("Scale")]
    public float startScale = 0.35f;     // 처음 작게
    public float endScale = 1.0f;        // 최대 사거리쯤 커짐

    [Header("Lifetime")]
    public float maxLifetime = 4f;       // 안전장치
}
