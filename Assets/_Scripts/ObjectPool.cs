using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
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
        foreach (var it in items)
        {
            if (it == null || string.IsNullOrEmpty(it.key) || it.prefab == null) continue;

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

    public GameObject Get(string key, Vector3 pos, Quaternion rot)
    {
        if (!pool.TryGetValue(key, out var q))
            throw new System.Exception($"Pool key not found: {key}");

        GameObject go;
        if (q.Count > 0)
        {
            go = q.Dequeue();
        }
        else
        {
            if (!prefabs.TryGetValue(key, out var pf))
                throw new System.Exception($"Prefab not found for key: {key}");
            go = Instantiate(pf, transform);
        }

        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go;
    }

    public void Release(string key, GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(transform);

        if (!pool.TryGetValue(key, out var q))
        {
            // key가 잘못되면 그냥 파괴(실수 방지)
            Destroy(go);
            return;
        }

        q.Enqueue(go);
    }
}
