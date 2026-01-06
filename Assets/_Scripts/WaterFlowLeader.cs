using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaterFlowLeader : MonoBehaviour
{
    [Header("Pool")]
    public ObjectPool pool;
    public string leaderKey = "water_leader";
    public string followerKey = "water_follower";

    [Header("Trail")]
    public int followerCount = 5;     // 총 6장 = 1 + 5
    public int spacingSteps = 2;      // 꼬리 간격(기록 인덱스 간격)

    [Header("Map")]
    public Grid grid;
    public Tilemap wallMap;           // 막는 타일맵

    [Header("Movement")]
    public float stepInterval = 0.06f; // 칸 이동 간격
    public int maxSteps = 140;         // 수명(이동 스텝)
    public int historyCapacity = 400;  // 기록 버퍼
    public int seedSide = 0;           // 0이면 랜덤, 1=오른쪽 시작, -1=왼쪽 시작

    [HideInInspector] public List<Vector3> history = new();

    float t;
    int steps;
    int sideDir;            // -1=left, 1=right
    bool onGroundOnce;      // 지면에 "처음 닿았는지"
    readonly List<WaterFollowerPooled> followers = new();

    void Awake()
    {
        if (grid == null) grid = FindAnyObjectByType<Grid>();
        if (pool == null) pool = FindAnyObjectByType<ObjectPool>();
    }

    void OnEnable()
    {
        // 재사용될 수 있으니 초기화
        t = 0f;
        steps = 0;
        onGroundOnce = false;
        history.Clear();
        followers.Clear();

        // 시작 위치 그리드 스냅
        var startCell = grid.WorldToCell(transform.position);
        transform.position = grid.GetCellCenterWorld(startCell);

        // 방향 초기화
        if (seedSide == 1 || seedSide == -1) sideDir = seedSide;
        else sideDir = (Random.value < 0.5f) ? -1 : 1;

        // 초기 기록
        Record();

        // 팔로워 풀에서 꺼내기
        SpawnFollowers();
    }

    void Update()
    {
        t += Time.deltaTime;
        while (t >= stepInterval)
        {
            t -= stepInterval;

            if (steps >= maxSteps)
            {
                ReleaseAll();
                return;
            }

            StepLogic();
            Record();
            steps++;
        }
    }

    void SpawnFollowers()
    {
        if (pool == null || followerCount <= 0) return;

        for (int i = 1; i <= followerCount; i++)
        {
            var go = pool.Get(followerKey, transform.position, Quaternion.identity);
            var f = go.GetComponent<WaterFollowerPooled>();
            if (f == null)
            {
                // 프리팹에 스크립트 없으면 안전하게 비활성
                pool.Release(followerKey, go);
                continue;
            }

            f.pool = pool;
            f.poolKey = followerKey;
            f.leader = this;
            f.followIndex = i * spacingSteps;
            followers.Add(f);
        }
    }

    void StepLogic()
    {
        var cell = grid.WorldToCell(transform.position);

        // 아래 체크
        var down = cell + Vector3Int.down;

        // 아래가 비면 => 떨어짐
        if (!IsBlocked(down))
        {
            MoveTo(down);
            return;
        }

        // 여기까지 왔으면 "바닥(아래 막힘)" 상태
        if (!onGroundOnce)
        {
            // 지면에 처음 닿는 순간, 좌우 방향 랜덤
            sideDir = (Random.value < 0.5f) ? -1 : 1;
            onGroundOnce = true;
        }

        // 옆으로 이동 시도 (현재 방향)
        var side = cell + new Vector3Int(sideDir, 0, 0);
        if (!IsBlocked(side))
        {
            MoveTo(side);
            return;
        }

        // 옆이 막혔으면 => 반대 방향으로 "되돌아가기"
        sideDir *= -1;

        var back = cell + new Vector3Int(sideDir, 0, 0);
        if (!IsBlocked(back))
        {
            MoveTo(back);
            return;
        }

        // 좌우 모두 막힘이면: 제자리(정지) or 종료 중 택1
        // 보글보글 느낌이면 보통 잠깐 정지 후 소멸이 자연스러움
        // 여기서는 "정지" 대신 종료 처리
        ReleaseAll();
    }

    void Record()
    {
        history.Insert(0, transform.position);
        if (history.Count > historyCapacity)
            history.RemoveAt(history.Count - 1);
    }

    void MoveTo(Vector3Int cell)
    {
        transform.position = grid.GetCellCenterWorld(cell);
    }

    bool IsBlocked(Vector3Int cell)
    {
        return wallMap != null && wallMap.HasTile(cell);
    }

    void ReleaseAll()
    {
        // 팔로워 먼저 반납
        for (int i = 0; i < followers.Count; i++)
        {
            if (followers[i] != null) followers[i].SafeRelease();
        }
        followers.Clear();

        // 리더도 반납
        if (pool != null && !string.IsNullOrEmpty(leaderKey))
            pool.Release(leaderKey, gameObject);
        else
            gameObject.SetActive(false);
    }
}
