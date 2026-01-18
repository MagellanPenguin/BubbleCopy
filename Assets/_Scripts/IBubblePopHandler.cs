public interface IBubblePopHandler
{
    // bubble: 터진 버블(본체)
    // firedDir: 버블 발사 방향 (1 또는 -1)
    void OnBubblePop(BubbleProjectile bubble, float firedDir);
}
