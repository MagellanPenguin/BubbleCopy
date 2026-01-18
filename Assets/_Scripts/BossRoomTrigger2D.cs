using UnityEngine;

public class BossRoomTrigger2D : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponentInParent<Player1Controller>();
        if (p) p.EnterBossBattle();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var p = other.GetComponentInParent<Player1Controller>();
        if (p) p.ExitBossBattle();
    }
}
