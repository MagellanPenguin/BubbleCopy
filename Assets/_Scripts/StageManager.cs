using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    [Header("Player Layer")]
    public string playerLayerName = "Player";
    int playerLayer;

    [Header("Special Monster (Clear 제외)")]
    public GameObject specialMonsterPrefab;
    public bool spawnSpecialOncePerRoom = true;

    [Header("Clear Options")]
    public bool autoClearWhenNoSpawns = false;

    Room currentRoom;
    RoomStageConfig currentConfig;

    // 클리어 조건: "룸 기본 스폰 몹"만 카운트
    int normalAliveCount = 0;
    readonly HashSet<EnemyDeathReporter> normalAlive = new HashSet<EnemyDeathReporter>();

    // 특수몹은 클리어 제외(따로 추적)
    EnemyDeathReporter specialReporter;
    bool specialSpawned = false;

    bool clearTriggered = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        playerLayer = LayerMask.NameToLayer(playerLayerName);
    }

    void Start()
    {
        currentRoom = RoomTransitionManager.Instance ? RoomTransitionManager.Instance.CurrentRoom : null;
        EnterRoom(currentRoom);
    }

    void Update()
    {
        var rtm = RoomTransitionManager.Instance;
        if (!rtm) return;

        if (currentRoom != rtm.CurrentRoom)
        {
            currentRoom = rtm.CurrentRoom;
            EnterRoom(currentRoom);
            return;
        }

        // (중요) 리포터 누락/파괴로 카운트가 안 줄어드는 케이스 방지
        if (!clearTriggered && normalAliveCount > 0)
            CleanupDestroyedReportersAndCheckClear();
    }

    void CleanupDestroyedReportersAndCheckClear()
    {
        // HashSet을 직접 순회하며 remove하면 오류날 수 있어서 임시 리스트 사용
        s_tmp.Clear();
        foreach (var r in normalAlive)
        {
            if (r == null) s_tmp.Add(r);
        }

        if (s_tmp.Count == 0) return;

        for (int i = 0; i < s_tmp.Count; i++)
        {
            normalAlive.Remove(s_tmp[i]);
            normalAliveCount = Mathf.Max(0, normalAliveCount - 1);
        }

        if (!clearTriggered && normalAliveCount == 0)
            TriggerClear();
    }
    static readonly List<EnemyDeathReporter> s_tmp = new List<EnemyDeathReporter>(64);

    void EnterRoom(Room room)
    {
        clearTriggered = false;
        specialSpawned = false;
        specialReporter = null;

        normalAliveCount = 0;
        normalAlive.Clear();

        if (!room) return;

        currentConfig = room.GetComponent<RoomStageConfig>();

        // 타이머: 보스룸 제외 (네 UI 타이머 함수들 있는 전제)
        if (InGameUIManager.Instance)
        {
            if (room.isBossRoom) InGameUIManager.Instance.DisableStageTimer();
            else InGameUIManager.Instance.StartStageTimer();
        }

        SpawnRoomEnemiesRandom(room, currentConfig);

        if (!room.isBossRoom && autoClearWhenNoSpawns && normalAliveCount == 0)
            TriggerClear();
    }

    void SpawnRoomEnemiesRandom(Room room, RoomStageConfig cfg)
    {
        if (!cfg) return;
        if (cfg.spawnPoints == null || cfg.spawnPoints.Length == 0) return;

        // 보스룸이면 bossPrefab 1마리만(선택)
        if (room.isBossRoom && cfg.bossPrefab)
        {
            SpawnOneNormal(room, cfg.bossPrefab, cfg.spawnPoints[0].position);
            return;
        }

        if (cfg.monsterPrefabs == null || cfg.monsterPrefabs.Length == 0) return;

        for (int i = 0; i < cfg.spawnPoints.Length; i++)
        {
            var sp = cfg.spawnPoints[i];
            if (!sp) continue;

            var prefab = cfg.monsterPrefabs[Random.Range(0, cfg.monsterPrefabs.Length)];
            if (!prefab) continue;

            SpawnOneNormal(room, prefab, sp.position);
        }
    }

    void SpawnOneNormal(Room room, GameObject prefab, Vector3 pos)
    {
        pos.z = 0f;
        var go = Instantiate(prefab, pos, Quaternion.identity);

        // EnemyBase 초기화(DeathReporter 바인딩 포함)
        var eb = go.GetComponent<EnemyBase>();
        if (eb) eb.Init(room, playerLayer);

        // Reporter 보장 + 룸 바인딩
        var rep = go.GetComponent<EnemyDeathReporter>();
        if (!rep) rep = go.AddComponent<EnemyDeathReporter>();
        rep.BindToRoom(room);

        normalAlive.Add(rep);
        normalAliveCount++;
    }

    public void NotifyEnemyDead(EnemyDeathReporter e)
    {
        if (!e) return;
        if (currentRoom && e.RoomOwner != currentRoom) return;

        // 특수몹이면 클리어 카운트 제외
        if (specialReporter == e)
        {
            specialReporter = null;
            return;
        }

        if (normalAlive.Remove(e))
            normalAliveCount = Mathf.Max(0, normalAliveCount - 1);

        if (!clearTriggered && normalAliveCount == 0)
            TriggerClear();
    }

    void TriggerClear()
    {
        if (clearTriggered) return;
        clearTriggered = true;

        // 클리어 시 특수몹 살아있으면 제거(원하면 유지해도 됨)
        if (specialReporter && specialReporter.gameObject)
            Destroy(specialReporter.gameObject);

        if (InGameUIManager.Instance && currentRoom && !currentRoom.isBossRoom)
            InGameUIManager.Instance.ResetStageTimer();

        Room next = ResolveNext(currentRoom);
        if (next) RoomTransitionManager.Instance.TryMoveTo(next);
    }

    Room ResolveNext(Room r)
    {
        if (!r) return null;
        switch (r.nextDir)
        {
            case Room.MoveDir.Up: return r.up;
            case Room.MoveDir.Down: return r.down;
            case Room.MoveDir.Left: return r.left;
            case Room.MoveDir.Right: return r.right;
            default: return null;
        }
    }

    // 타이머 만료 시 특수몹 스폰 (클리어 제외)
    public void OnStageTimerExpired()
    {
        if (!currentRoom) return;
        if (currentRoom.isBossRoom) return;
        if (!specialMonsterPrefab) return;
        if (spawnSpecialOncePerRoom && specialSpawned) return;

        specialSpawned = true;

        Vector3 pos = currentRoom.cameraAnchor ? currentRoom.cameraAnchor.position : currentRoom.transform.position;
        pos.z = 0;
        pos += (Vector3)Random.insideUnitCircle * 0.8f;

        var go = Instantiate(specialMonsterPrefab, pos, Quaternion.identity);

        var sp = go.GetComponent<EnemySpecialChaser>();
        if (!sp) sp = go.AddComponent<EnemySpecialChaser>();
        sp.Init(currentRoom, playerLayer);

        var rep = go.GetComponent<EnemyDeathReporter>();
        if (!rep) rep = go.AddComponent<EnemyDeathReporter>();
        rep.BindToRoom(currentRoom);

        specialReporter = rep;
    }
}
