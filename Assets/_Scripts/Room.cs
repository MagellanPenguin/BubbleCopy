using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("ID (1~10)")]
    public int id;

    [Header("Anchors")]
    public Transform cameraAnchor;

    [Header("Entry Points")]
    public Transform entryPointP1;
    public Transform entryPointP2;

    [Header("Neighbors")]
    public Room up;
    public Room down;
    public Room left;
    public Room right;

    [Header("Stage Options")]
    public bool isBossRoom = false;             // 보스룸이면 타이머 제외
    public MoveDir nextDir = MoveDir.Right;     // 클리어 시 자동 이동 방향

    public enum MoveDir { Up, Down, Left, Right, None }
}
