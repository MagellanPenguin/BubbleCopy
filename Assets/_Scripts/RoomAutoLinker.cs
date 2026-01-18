using UnityEngine;

public class RoomAutoLinker : MonoBehaviour
{
    public Room[] rooms = new Room[10];

    [ContextMenu("Auto Link (Snake 3x3 + extra 10)")]
    public void AutoLink()
    {
        if (rooms == null || rooms.Length < 10)
        {
            return;
        }

        // id 자동 세팅
        for (int i = 0; i < 10; i++)
        {
            if (!rooms[i]) continue;
            rooms[i].id = i + 1;
            rooms[i].up = rooms[i].down = rooms[i].left = rooms[i].right = null;
        }

        // grid[x,y], y=0 top
        Room[,] grid = new Room[3, 3];

        grid[0, 0] = rooms[0]; // 1
        grid[1, 0] = rooms[1]; // 2
        grid[2, 0] = rooms[2]; // 3

        grid[2, 1] = rooms[3]; // 4
        grid[1, 1] = rooms[4]; // 5
        grid[0, 1] = rooms[5]; // 6

        grid[0, 2] = rooms[6]; // 7
        grid[1, 2] = rooms[7]; // 8
        grid[2, 2] = rooms[8]; // 9

        // 내부 연결
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                Room r = grid[x, y];
                if (!r) continue;

                if (x - 1 >= 0 && grid[x - 1, y]) r.left = grid[x - 1, y];
                if (x + 1 < 3 && grid[x + 1, y]) r.right = grid[x + 1, y];

                if (y - 1 >= 0 && grid[x, y - 1]) r.up = grid[x, y - 1];
                if (y + 1 < 3 && grid[x, y + 1]) r.down = grid[x, y + 1];
            }
        }

        // 9 <-> 10
        Room room9 = rooms[8];
        Room room10 = rooms[9];
        if (room9 && room10)
        {
            room9.right = room10;
            room10.left = room9;
        }

        Debug.Log(" AutoLink 완료: Snake 3x3 + 10번 연결");
    }
}
