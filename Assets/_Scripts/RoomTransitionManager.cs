using System.Collections;
using UnityEngine;

public class RoomTransitionManager : MonoBehaviour
{
    public static RoomTransitionManager Instance;

    [Header("Start")]
    [SerializeField] private Room startRoom;

    [Header("Player Prefabs")]
    [SerializeField] private GameObject player1Prefab;
    [SerializeField] private GameObject player2Prefab;

    [Header("Spawn FX")]
    [SerializeField] private GameObject spawnFxPrefab;   // 반짝이/소환 FX
    [SerializeField] private GameObject warpFxPrefab;    // 워프 FX 

    [Header("Invincible")]
    [SerializeField] private float spawnInvincibleTime = 2.0f;

    [Header("Transition")]
    [SerializeField] private float unlockDelay = 0.08f;
    [SerializeField] private float toCenterTime = 0.15f;   // 중앙으로 모으는 시간
    [SerializeField] private float toEntryTime = 0.25f;    // 엔트리로 보내는 시간

    Room currentRoom;

    // 런타임 인스턴스
    Player1Controller player1;
    Player2Controller player2;

    bool isWarping = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 시작 방 세팅(플레이어가 없어도 카메라는 세팅 가능)
        if (!currentRoom && startRoom)
            InitRoomOnly(startRoom);

        // (선택) UI 이벤트로 조인하고 싶으면 여기서 구독
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.OnStartPlayer += OnStartPlayer;
            InGameUIManager.Instance.OnContinuePlayer += OnContinuePlayer;
        }
    }

    void OnDestroy()
    {
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.OnStartPlayer -= OnStartPlayer;
            InGameUIManager.Instance.OnContinuePlayer -= OnContinuePlayer;
        }
    }

    void Update()
    {
        // (선택) 코인 넣고 1/2로 시작을 “키 입력”으로도 하려면:
        // 여기서는 단순히 1/2 눌렀을 때 스폰하도록 해둠. (코인 체크는 너 시스템에 맞게 붙이면 됨)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            SpawnOrRevive(1);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            SpawnOrRevive(2);
    }

    void OnStartPlayer(int p) => SpawnOrRevive(p);
    void OnContinuePlayer(int p) => SpawnOrRevive(p);

    // ---- 초기 카메라만 세팅 (플레이어 없어도 됨)
    public void InitRoomOnly(Room start)
    {
        currentRoom = start;

        if (currentRoom.cameraAnchor)
            CameraRoomSlider.Instance.SnapTo(currentRoom.cameraAnchor);
    }

    // ---- 플레이어 스폰/부활
    public void SpawnOrRevive(int p)
    {
        if (!currentRoom) return;

        if (p == 1)
        {
            if (!player1)
            {
                if (!player1Prefab) return;

                Vector3 spawnPos = GetEntryPosP1(currentRoom);
                var go = Instantiate(player1Prefab, spawnPos, Quaternion.identity);
                player1 = go.GetComponent<Player1Controller>();

                PlayFx(spawnFxPrefab, spawnPos);
                GiveInvincible(go, spawnInvincibleTime);

                go.GetComponent<IPlayerRevive>()?.Revive();
            }
            else
            {
                // 부활
                player1.gameObject.SetActive(true);
                Vector3 spawnPos = GetEntryPosP1(currentRoom);
                Teleport(player1.transform, spawnPos);

                PlayFx(spawnFxPrefab, spawnPos);
                GiveInvincible(player1.gameObject, spawnInvincibleTime);

                player1.GetComponent<IPlayerRevive>()?.Revive();
            }
        }
        else // p == 2
        {
            if (!player2)
            {
                if (!player2Prefab) return;

                Vector3 spawnPos = GetEntryPosP2(currentRoom);
                var go = Instantiate(player2Prefab, spawnPos, Quaternion.identity);
                player2 = go.GetComponent<Player2Controller>();

                PlayFx(spawnFxPrefab, spawnPos);
                GiveInvincible(go, spawnInvincibleTime);

                go.GetComponent<IPlayerRevive>()?.Revive();
            }
            else
            {
                player2.gameObject.SetActive(true);
                Vector3 spawnPos = GetEntryPosP2(currentRoom);
                Teleport(player2.transform, spawnPos);

                PlayFx(spawnFxPrefab, spawnPos);
                GiveInvincible(player2.gameObject, spawnInvincibleTime);

                player2.GetComponent<IPlayerRevive>()?.Revive();
            }
        }
    }

    // ---- 룸 이동
    public bool TryMoveTo(Room next)
    {
        if (!currentRoom || !next) return false;
        if (!next.cameraAnchor) return false;
        if (CameraRoomSlider.Instance.IsSliding) return false;
        if (isWarping) return false;

        isWarping = true;

        SetControlAll(false);

        // 카메라 슬라이드
        CameraRoomSlider.Instance.SlideTo(next.cameraAnchor, () =>
        {
            StartCoroutine(WarpPlayersRoutine(next));
        });

        return true;
    }

    IEnumerator WarpPlayersRoutine(Room next)
    {
        // 1) 화면 중앙으로 이동
        Vector3 center = GetCameraCenterWorld();

        if (player1) yield return StartCoroutine(RoughMove(player1.transform, center, toCenterTime));
        if (player2) yield return StartCoroutine(RoughMove(player2.transform, center, toCenterTime));

        PlayFx(warpFxPrefab, center);

        // 2) 다음 룸 entry로 러프 이동
        Vector3 p1Target = GetEntryPosP1(next);
        Vector3 p2Target = GetEntryPosP2(next);

        if (player1) yield return StartCoroutine(RoughMove(player1.transform, p1Target, toEntryTime));
        if (player2) yield return StartCoroutine(RoughMove(player2.transform, p2Target, toEntryTime));

        PlayFx(warpFxPrefab, p1Target);
        if (player2) PlayFx(warpFxPrefab, p2Target);

        currentRoom = next;

        // 3) 잠깐 딜레이 후 컨트롤 복구
        yield return new WaitForSeconds(unlockDelay);
        SetControlAll(true);

        isWarping = false;
    }

    // ---- 위치 계산
    Vector3 GetEntryPosP1(Room room)
    {
        if (room.entryPointP1) return room.entryPointP1.position;
        return GetCameraCenterWorld(); // fallback
    }

    Vector3 GetEntryPosP2(Room room)
    {
        if (room.entryPointP2) return room.entryPointP2.position;
        if (room.entryPointP1) return room.entryPointP1.position + Vector3.right * 1.2f;
        return GetCameraCenterWorld() + Vector3.right * 1.2f;
    }

    Vector3 GetCameraCenterWorld()
    {
        // 2D 기준: 카메라 가운데 월드 좌표
        Camera cam = Camera.main;
        if (!cam) return Vector3.zero;

        Vector3 v = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        v.z = 0f;
        return v;
    }

    // ---- 이동/제어/이펙트 유틸
    void SetControlAll(bool enabled)
    {
        if (player1) player1.SetControlEnabled(enabled);
        if (player2) player2.SetControlEnabled(enabled);
    }

    IEnumerator RoughMove(Transform t, Vector3 target, float duration)
    {
        if (!t) yield break;

        // 물리 있으면 잠시 멈추는 게 안전
        var rb = t.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        Vector3 start = t.position;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float a = (duration <= 0f) ? 1f : Mathf.Clamp01(time / duration);
            t.position = Vector3.Lerp(start, target, a);
            yield return null;
        }

        t.position = target;

        if (rb) rb.simulated = true;
    }

    void Teleport(Transform t, Vector3 pos)
    {
        if (!t) return;
        var rb = t.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = pos;
        }
        else t.position = pos;
    }

    void PlayFx(GameObject fxPrefab, Vector3 pos)
    {
        if (!fxPrefab) return;
        Instantiate(fxPrefab, pos, Quaternion.identity);
    }

    void GiveInvincible(GameObject playerObj, float sec)
    {
        if (!playerObj) return;

        // 1) IPlayerRevive가 무적을 내부에서 처리한다면 굳이 필요 없음
        // 2) 아니면, 플레이어 컨트롤러에 "SetInvincible(float)" 같은 함수가 있다면 이걸로 통일 추천
        playerObj.SendMessage("SetInvincible", sec, SendMessageOptions.DontRequireReceiver);

        // 최소 보장: SetInvincible가 없다면, 너가 가진 무적 로직 방식에 맞춰 연결하면 됨
    }

    public Room CurrentRoom => currentRoom;
}
