using System.Collections;
using UnityEngine;

public class RoomTransitionManager : MonoBehaviour
{
    public static RoomTransitionManager Instance;

    [Header("Start")]
    [SerializeField] private Room startRoom;
    [SerializeField] private Player1Controller player;

    [Header("Transition")]
    [SerializeField] private float unlockDelay = 0.08f;

    Room currentRoom;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 시작 세팅
        if (!currentRoom && startRoom && player)
            Init(startRoom, player);
    }

    public void Init(Room start, Player1Controller p)
    {
        currentRoom = start;
        player = p;

        if (currentRoom.cameraAnchor)
            CameraRoomSlider.Instance.SnapTo(currentRoom.cameraAnchor);

        if (currentRoom.entryPoint)
            player.transform.position = currentRoom.entryPoint.position;
    }

    public bool TryMoveTo(Room next)
    {
        if (!currentRoom || !next || !player) return false;
        if (CameraRoomSlider.Instance.IsSliding) return false;
        if (!next.cameraAnchor) return false;

        // 입력 잠금
        player.SetControlEnabled(false);

        // 카메라 이동
        CameraRoomSlider.Instance.SlideTo(next.cameraAnchor, () =>
        {
            // 다음 방 입장 위치(선택)
            if (next.entryPoint) player.transform.position = next.entryPoint.position;

            currentRoom = next;
            StartCoroutine(UnlockAfter());
        });

        return true;
    }

    IEnumerator UnlockAfter()
    {
        yield return new WaitForSeconds(unlockDelay);
        player.SetControlEnabled(true);
    }

    // ✅ 자동 진행: 현재 id+1로 이동
    public bool AutoAdvance()
    {
        if (!currentRoom) return false;
        if (currentRoom.id >= 10) return false;

        int nextId = currentRoom.id + 1;
        Room next = FindRoomById(nextId);
        if (!next) return false;

        return TryMoveTo(next);
    }

    Room FindRoomById(int id)
    {
        var all = FindObjectsOfType<Room>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i].id == id) return all[i];
        return null;
    }

    public Room CurrentRoom => currentRoom;
}
