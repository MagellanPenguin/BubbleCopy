using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("ID (1~10)")]
    public int id;

    [Header("Anchors")]
    public Transform cameraAnchor;  // 카메라가 멈출 위치
    public Transform entryPoint;    // 이 방으로 들어올 때 플레이어 위치(선택)

    [Header("Neighbors")]
    public Room up;
    public Room down;
    public Room left;
    public Room right;
}
