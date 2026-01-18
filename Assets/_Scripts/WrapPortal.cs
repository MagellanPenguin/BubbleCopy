using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WrapPortal : MonoBehaviour
{
    [Header("Link (필수)")]
    public WrapPortal other;          // 반대편 포탈

    [Header("Output X")]
    public bool forceToOtherCenterX = true;

    [Range(0f, 1f)]
    public float otherXNormalized = 0.5f;

    [Header("Extra Offset")]
    public Vector2 extraOffset = Vector2.zero;

    [Header("Who can warp")]
    public string playerLayerName = "Player";
    public string enemyLayerName = "Enemy";
    public string enemyTagFallback = "Enemy";
    public bool warpPlayers = true;
    public bool warpEnemies = true;

    [Header("Anti Ping-Pong")]
    public float reenterBlockTime = 0.20f;

    BoxCollider2D boxA;

    int playerLayer;
    int enemyLayer;

    static readonly Dictionary<int, float> warpBlockUntil = new Dictionary<int, float>(128);

    void Awake()
    {
        boxA = GetComponent<BoxCollider2D>();
        boxA.isTrigger = true;

        playerLayer = LayerMask.NameToLayer(playerLayerName);
        enemyLayer = LayerMask.NameToLayer(enemyLayerName);
    }

    void OnTriggerEnter2D(Collider2D otherCol) => TryWarp(otherCol);
    void OnTriggerStay2D(Collider2D otherCol) => TryWarp(otherCol); // 바닥/벽 접촉 안정화

    void TryWarp(Collider2D otherCol)
    {
        if (!other) return;
        if (!other.boxA) return;

        var go = otherCol.attachedRigidbody ? otherCol.attachedRigidbody.gameObject : otherCol.gameObject;
        if (!go) return;
        if (!IsAllowed(go)) return;

        int id = go.GetInstanceID();
        if (warpBlockUntil.TryGetValue(id, out float until) && Time.time < until)
            return;

        Vector3 outPos = ComputeOutPos(go.transform.position, boxA, other.boxA);
        outPos += (Vector3)extraOffset;

        TeleportKeepVelocity(go, outPos);

        warpBlockUntil[id] = Time.time + reenterBlockTime;
    }

    bool IsAllowed(GameObject go)
    {
        if (warpPlayers && go.layer == playerLayer) return true;

        if (warpEnemies)
        {
            if (enemyLayer != -1 && go.layer == enemyLayer) return true;
            if (!string.IsNullOrEmpty(enemyTagFallback) && go.CompareTag(enemyTagFallback)) return true;
        }
        return false;
    }

    Vector3 ComputeOutPos(Vector3 objWorld, BoxCollider2D from, BoxCollider2D to)
    {
        Vector2 fromCenter = (Vector2)from.transform.TransformPoint(from.offset);
        Vector2 fromSize = from.size;

        float fromMinY = fromCenter.y - fromSize.y * 0.5f;
        float fromMaxY = fromCenter.y + fromSize.y * 0.5f;
        float v = Mathf.InverseLerp(fromMinY, fromMaxY, objWorld.y);

        Vector2 toCenter = (Vector2)to.transform.TransformPoint(to.offset);
        Vector2 toSize = to.size;

        float toMinY = toCenter.y - toSize.y * 0.5f;
        float toMaxY = toCenter.y + toSize.y * 0.5f;
        float outY = Mathf.Lerp(toMinY, toMaxY, v);

        float outX;
        if (forceToOtherCenterX)
        {
            outX = toCenter.x;
        }
        else
        {
            float toMinX = toCenter.x - toSize.x * 0.5f;
            float toMaxX = toCenter.x + toSize.x * 0.5f;
            outX = Mathf.Lerp(toMinX, toMaxX, otherXNormalized);
        }

        return new Vector3(outX, outY, objWorld.z);
    }

    // 속도 유지 확정
    void TeleportKeepVelocity(GameObject go, Vector3 pos)
    {
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb)
        {
            Vector2 v = rb.linearVelocity;
            float av = rb.angularVelocity;

            rb.position = pos;

            rb.linearVelocity = v;
            rb.angularVelocity = av;
        }
        else
        {
            go.transform.position = pos;
        }
    }
}
