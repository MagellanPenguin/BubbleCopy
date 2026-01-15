using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("ID (1~10)")]
    public int id;

    [Header("Anchors")]
    public Transform cameraAnchor;   // 카메라가 멈출 위치

    [Header("Entry Points")]
    public Transform entryPointP1;   // 1P 입장 위치
    public Transform entryPointP2;   // 2P 입장 위치 

    [Header("Neighbors")]
    public Room up;
    public Room down;
    public Room left;
    public Room right;
}
