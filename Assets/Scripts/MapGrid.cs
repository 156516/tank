using System.Collections.Generic;
using UnityEngine;

// 静态网格地图:把场景离散为 23 x 19 格子,
// 记录每个格子的可走性,提供给 Enemy 做 DFS 路径规划。
public static class MapGrid
{
    // 与 MapCreation 的 InitMap 范围对齐
    public const int MinX = -11, MaxX = 11;
    public const int MinY = -9, MaxY = 9;
    public const int Cols = MaxX - MinX + 1;   // 23
    public const int Rows = MaxY - MinY + 1;   // 19

    // 格子类型:
    // 0 = 可走   1 = 永久阻挡(铁墙 / 河流)
    // 2 = 可击碎砖墙(阻挡但通过子弹可清除)
    // 3 = Heart(目的地,当 target == Heart 时可视为"可走")
    public const int Walkable = 0;
    public const int PermanentBlock = 1;
    public const int BreakableWall = 2;
    public const int HeartCell = 3;

    private static int[,] cellType;
    private static bool initialized;

    public static int Width => Cols;
    public static int Height => Rows;

    // 世界坐标 -> 格子下标
    public static Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x) - MinX;
        int y = Mathf.RoundToInt(worldPos.y) - MinY;
        x = Mathf.Clamp(x, 0, Cols - 1);
        y = Mathf.Clamp(y, 0, Rows - 1);
        return new Vector2Int(x, y);
    }

    // 格子下标 -> 世界坐标(返回格子中心点)
    public static Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x + MinX, cell.y + MinY, 0);
    }

    // 重新扫描场景中每个格子的中心点,初始化 cellType
    // 23 * 19 = 437 次 OverlapPoint,Unity 物理较快,可多次调用
    public static void Rebuild()
    {
        if (cellType == null || cellType.GetLength(0) != Cols || cellType.GetLength(1) != Rows)
        {
            cellType = new int[Cols, Rows];
        }
        else
        {
            // 清零再扫描
            for (int cx = 0; cx < Cols; cx++)
                for (int cy = 0; cy < Rows; cy++)
                    cellType[cx, cy] = 0;
        }
        for (int cx = 0; cx < Cols; cx++)
        {
            for (int cy = 0; cy < Rows; cy++)
            {
                Vector3 pos = new Vector3(cx + MinX, cy + MinY, 0);
                Collider2D col = Physics2D.OverlapPoint(pos);
                if (col == null) continue;
                string tag = col.tag;
                if (tag == "Barrier") cellType[cx, cy] = PermanentBlock;
                else if (tag == "Wall") cellType[cx, cy] = BreakableWall;
                else if (tag == "Heart") cellType[cx, cy] = HeartCell;
                else if (col.gameObject.name == "River") cellType[cx, cy] = PermanentBlock;
                // Enemy / Player 当作可走(它们会移动)
            }
        }
        initialized = true;
    }

    // 标记某砖墙被打碎(由 Bullet 调用)
    public static void MarkWallBroken(Vector2Int cell)
    {
        if (!initialized) return;
        if (cell.x < 0 || cell.x >= Cols || cell.y < 0 || cell.y >= Rows) return;
        if (cellType[cell.x, cell.y] == BreakableWall)
        {
            cellType[cell.x, cell.y] = Walkable;
        }
    }

    // 该格子是否可走:
    // - allowBreakable=true 时,砖墙视为"可走但要打碎"(会在走之前被子弹清除)
    public static bool IsWalkable(Vector2Int cell, bool allowBreakable)
    {
        if (!initialized) return true;  // 默认都走,初始化前不阻挡(防御)
        if (cell.x < 0 || cell.x >= Cols || cell.y < 0 || cell.y >= Rows) return false;
        int type = cellType[cell.x, cell.y];
        if (type == PermanentBlock) return false;
        if (type == BreakableWall) return allowBreakable;
        return true;
    }

    // DFS 路径:从 start 到 end,返回格点列表(含 end,不含 start),失败 null
    // maxDepth 防 stack overflow
    private const int MaxDepth = 200;

    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool allowBreakable)
    {
        return FindPath(start, end, allowBreakable, null);
    }

    // 重载:banned 集合中的格子视为 PermanentBlock(用于让 enemy 互相错开走)
    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool allowBreakable,
                                            HashSet<Vector2Int> banned)
    {
        if (!initialized) return null;
        if (start == end) return new List<Vector2Int> { end };
        if (!IsWalkableWithBan(end, allowBreakable, banned)) return null;

        bool[,] visited = new bool[Cols, Rows];
        List<Vector2Int> path = new List<Vector2Int>();
        if (DFS(start, end, allowBreakable, banned, visited, path, 0))
        {
            return path;
        }
        return null;
    }

    private static readonly Vector2Int[] FourDirs = {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)
    };

    private static bool DFS(Vector2Int cur, Vector2Int end, bool allowBreakable,
                            HashSet<Vector2Int> banned, bool[,] visited,
                            List<Vector2Int> path, int depth)
    {
        if (depth > MaxDepth) return false;
        if (cur == end)
        {
            path.Add(cur);
            return true;
        }
        if (visited[cur.x, cur.y]) return false;
        visited[cur.x, cur.y] = true;

        for (int i = 0; i < FourDirs.Length; i++)
        {
            Vector2Int next = cur + FourDirs[i];
            if (!IsWalkableWithBan(next, allowBreakable, banned)) continue;
            if (DFS(next, end, allowBreakable, banned, visited, path, depth + 1))
            {
                path.Add(cur);
                return true;
            }
        }
        return false;
    }

    private static bool IsWalkableWithBan(Vector2Int cell, bool allowBreakable, HashSet<Vector2Int> banned)
    {
        if (!IsWalkable(cell, allowBreakable)) return false;
        if (banned != null && banned.Contains(cell)) return false;
        return true;
    }

    // 「优先走空地」路径规划:先尝试不开砖墙的路径,失败再尝试允许破砖墙。
    // out usedBreakable 表示最终路径是否依赖破砖(让 Enemy 知道是否主动开火)
    public static List<Vector2Int> FindPathPreferOpen(Vector2Int start, Vector2Int end,
                                                     HashSet<Vector2Int> banned,
                                                     out bool usedBreakable)
    {
        // 第一轮:走空地优先(砖墙当阻挡)
        List<Vector2Int> path = FindPath(start, end, allowBreakable: false, banned);
        if (path != null)
        {
            usedBreakable = false;
            return path;
        }
        // 第二轮:允许破砖墙(实在绕不开时)
        path = FindPath(start, end, allowBreakable: true, banned);
        if (path != null)
        {
            usedBreakable = true;
            return path;
        }
        usedBreakable = false;
        return null;
    }
}
