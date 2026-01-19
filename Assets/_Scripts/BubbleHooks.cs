using UnityEngine;

public enum BubbleHitType
{
    MonsterCapture,   // capture success moment
    Wall,             // wall hit
    PlayerUnderHit    // player hit from below (pop trigger)
}

public interface IBubbleHitHandler
{
    void OnBubbleHit(BubbleProjectile bubble, Collider2D other, BubbleHitType hitType);
}

public interface IBubblePopHandler
{
    void OnBubblePop(BubbleProjectile bubble, float firedDir);
}
