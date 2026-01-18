using UnityEngine;

public class RoomStageConfig : MonoBehaviour
{
    [Header("Spawn Points (각 포인트에 1마리씩)")]
    public Transform[] spawnPoints;

    [Header("Random Monster Pool")]
    public GameObject[] monsterPrefabs;   // 여기 등록된 것 중 랜덤으로 뽑음

    [Header("Options")]
    public bool allowDuplicate = true;    // false면 같은 몬스터 중복 방지(풀 수 < 포인트 수면 자동 중복 허용처럼 동작)
    public bool shuffleSpawnPoints = false; // 포인트 순서도 섞고 싶으면 true

    [Header("Boss Room Option")]
    public GameObject bossPrefab;         // 보스룸이면 이거 1마리만 스폰(선택)
}
