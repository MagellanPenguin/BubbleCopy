using UnityEngine;

public class WaterFollowerPooled : MonoBehaviour
{
    public WaterFlowLeader leader;
    public int followIndex;       // leader.history에서 몇 번째 따라갈지
    public string poolKey;        // 이 팔로워를 풀에 반납할 때 사용할 키
    public ObjectPool pool;

    void Update()
    {
        if (leader == null || pool == null)
        {
            // 리더가 사라지면 알아서 풀로 복귀
            SafeRelease();
            return;
        }

        var h = leader.history;
        if (h == null || h.Count == 0) return;

        if (h.Count > followIndex) transform.position = h[followIndex];
        else transform.position = h[h.Count - 1];
    }

    public void SafeRelease()
    {
        if (pool != null && !string.IsNullOrEmpty(poolKey))
            pool.Release(poolKey, gameObject);
        else
            gameObject.SetActive(false);
    }
}
