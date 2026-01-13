using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public class PoolItem
    {
        public string key;
        public GameObject prefab;
        public int preload = 20;
    }

    public PoolItem[] items;

    readonly Dictionary<string, Queue<GameObject>> pool = new();
    readonly Dictionary<string, GameObject> prefabs = new();

    void Awake()
    {
        // ===== Singleton 처리 =====
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 전환 시 유지 (원하면 제거 가능)

        // ===== Pool 초기화 =====
        foreach (var it in items)
        {
            if (it == null || string.IsNullOrEmpty(it.key) || it.prefab == null)
                continue;

            if (pool.ContainsKey(it.key))
            {
                Debug.LogWarning($"[ObjectPool] Duplicate key ignored: {it.key}");
                continue;
            }

            prefabs[it.key] = it.prefab;
            var q = new Queue<GameObject>();
            pool[it.key] = q;

            for (int i = 0; i < Mathf.Max(0, it.preload); i++)
            {
                var go = Instantiate(it.prefab, transform);
                go.SetActive(false);
                q.Enqueue(go);
            }
        }
    }

    // ===== 가져오기 =====
    public GameObject Get(string key, Vector3 pos, Quaternion rot)
    {
        if (!pool.TryGetValue(key, out var q))
        {
            Debug.LogError($"[ObjectPool] Pool key not found: {key}");
            return null;
        }

        GameObject go;
        if (q.Count > 0)
        {
            go = q.Dequeue();
        }
        else
        {
            if (!prefabs.TryGetValue(key, out var pf))
            {
                Debug.LogError($"[ObjectPool] Prefab not found for key: {key}");
                return null;
            }
            go = Instantiate(pf, transform);
        }

        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go;
    }

    // ===== 되돌리기 =====
    public void Release(string key, GameObject go)
    {
        if (go == null) return;

        if (!pool.TryGetValue(key, out var q))
        {
            Debug.LogWarning($"[ObjectPool] Release failed, key not found: {key}");
            Destroy(go);
            return;
        }

        go.SetActive(false);
        go.transform.SetParent(transform);
        q.Enqueue(go);
    }
}
