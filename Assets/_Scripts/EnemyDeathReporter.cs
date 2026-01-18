using UnityEngine;

// 몬스터가 "죽을 때" StageManager에 알려주는 브릿지 컴포넌트
public class EnemyDeathReporter : MonoBehaviour
{
    public Room RoomOwner { get; private set; }

    bool reported = false;

    public void BindToRoom(Room room)
    {
        RoomOwner = room;
        reported = false;
    }

    public void ReportDead()
    {
        if (reported) return;
        reported = true;

        if (StageManager.Instance)
            StageManager.Instance.NotifyEnemyDead(this);
    }
}
