using UnityEngine;

public class PlayerJoinControllerPrefab : MonoBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private GameObject player1Prefab;
    [SerializeField] private GameObject player2Prefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform p1Spawn;
    [SerializeField] private Transform p2Spawn;

    GameObject p1Instance;
    GameObject p2Instance;

    private void Start()
    {
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.OnStartPlayer += OnStartPlayer;
            InGameUIManager.Instance.OnContinuePlayer += OnContinuePlayer;
        }
    }

    private void OnDestroy()
    {
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.OnStartPlayer -= OnStartPlayer;
            InGameUIManager.Instance.OnContinuePlayer -= OnContinuePlayer;
        }
    }

    void OnStartPlayer(int player) => SpawnOrRevive(player);
    void OnContinuePlayer(int player) => SpawnOrRevive(player);

    void SpawnOrRevive(int player)
    {
        if (player == 1)
        {
            if (!p1Instance)
            {
                if (!player1Prefab) return;
                p1Instance = Instantiate(player1Prefab, GetSpawnPos(1), Quaternion.identity);
            }
            else
            {
                p1Instance.transform.position = GetSpawnPos(1);
                p1Instance.SetActive(true);
            }

            // 부활(무적/상태 초기화)
            p1Instance.GetComponent<IPlayerRevive>()?.Revive();
        }
        else
        {
            if (!p2Instance)
            {
                if (!player2Prefab) return;
                p2Instance = Instantiate(player2Prefab, GetSpawnPos(2), Quaternion.identity);
            }
            else
            {
                p2Instance.transform.position = GetSpawnPos(2);
                p2Instance.SetActive(true);
            }

            p2Instance.GetComponent<IPlayerRevive>()?.Revive();
        }
    }

    Vector3 GetSpawnPos(int player)
    {
        if (player == 1) return p1Spawn ? p1Spawn.position : Vector3.zero;
        return p2Spawn ? p2Spawn.position : Vector3.zero;
    }
}
