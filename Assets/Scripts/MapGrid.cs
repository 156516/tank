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
        // 关键:刚 Instantiate 的碰撞体默认还没同步进物理世界(autoSyncTransforms=false),
        // 不先同步,下面的 OverlapPoint 会查不到任何障碍 → 整张网格被误判为空地。
        Physics2D.SyncTransforms();

        for (int cx = 0; cx < Cols; cx++)
        {
            for (int cy = 0; cy < Rows; cy++)
            {
                Vector3 pos = new Vector3(cx + MinX, cy + MinY, 0);
                Collider2D col = Physics2D.OverlapPoint(pos);
                if (col == null) continue;
                string tag = col.tag;
                // 注意:实例化后名字带 "(Clone)" 后缀,必须用 StartsWith 而不是 == "River"
                if (tag == "Barrier") cellType[cx, cy] = PermanentBlock;
                else if (tag == "Wall") cellType[cx, cy] = BreakableWall;
                else if (tag == "Heart") cellType[cx, cy] = HeartCell;
                else if (col.gameObject.name.StartsWith("River")) cellType[cx, cy] = PermanentBlock;
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

    // 读出某格类型(供外部在 IsWalkable 之外做更细的判断,比如区分砖墙 vs 永久阻挡)
    // 没初始化或越界都视为永久阻挡(防御性)
    public static int GetCellType(Vector2Int cell)
    {
        if (!initialized) return PermanentBlock;
        if (cell.x < 0 || cell.x >= Cols || cell.y < 0 || cell.y >= Rows) return PermanentBlock;
        return cellType[cell.x, cell.y];
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
        // HeartCell 视为可走:保证 A* 能一路(必要时炸墙)逼近基地。
        // 真正"不进入基地、停下开火"的行为由 Enemy.DecideDirection 处理,不在这里拦。
        return true;
    }

    // 扫描场上所有 tag=Enemy 的 GameObject,返回它们当前所在格的集合。
    // 供 Enemy 在 RecalculatePath 时作为 banned,让同伙主动错开,避免挤一起卡死。
    public static HashSet<Vector2Int> GetEnemyOccupiedCells()
    {
        var set = new HashSet<Vector2Int>();
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].activeInHierarchy)
            {
                set.Add(WorldToCell(enemies[i].transform.position));
            }
        }
        return set;
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

    // A* 寻路:f = g + h,g 是实际代价(每步 +1,破墙 +2),h 是曼哈顿距离
    // 找最优路径(在 admissible 启发式下);失败 null
    public static List<Vector2Int> FindPathAStar(Vector2Int start, Vector2Int end,
                                                bool allowBreakable, HashSet<Vector2Int> banned)
    {
        if (!initialized) return null;
        if (!IsWalkableWithBan(end, allowBreakable, banned)) return null;
        if (start == end) return new List<Vector2Int> { end };

        // 优先队列:用 List 而非真正 priority queue(437 格够用)
        List<AStarNode> open = new List<AStarNode>();
        // 每个 cell 当前已知的最小 g
        Dictionary<Vector2Int, float> bestG = new Dictionary<Vector2Int, float>();
        // 每个 cell 的前驱(用于回溯路径)
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        // closed 集(已扩展过)
        HashSet<Vector2Int> closed = new HashSet<Vector2Int>();

        bestG[start] = 0f;
        open.Add(new AStarNode(start, 0f, Heuristic(start, end)));

        const int MaxIter = 1000;
        int iter = 0;

        while (open.Count > 0 && iter < MaxIter)
        {
            iter++;
            // 找到 f = g + h 最小的节点
            int bestIdx = 0;
            float bestF = open[0].g + open[0].h;
            for (int i = 1; i < open.Count; i++)
            {
                float f = open[i].g + open[i].h;
                if (f < bestF) { bestF = f; bestIdx = i; }
            }
            AStarNode cur = open[bestIdx];
            open.RemoveAt(bestIdx);

            if (cur.cell == end)
            {
                return ReconstructPath(cameFrom, cur.cell);
            }
            closed.Add(cur.cell);

            for (int i = 0; i < FourDirs.Length; i++)
            {
                Vector2Int next = cur.cell + FourDirs[i];
                if (!IsWalkableWithBan(next, allowBreakable, banned)) continue;
                if (closed.Contains(next)) continue;

                // 砖墙:允许破墙时 cost=2(避免为微优化乱绕),不允许时不会被 IsWalkableWithBan 放过
                int type = cellType[next.x, next.y];
                float stepCost = (type == BreakableWall && allowBreakable) ? 2f : 1f;
                float tentativeG = cur.g + stepCost;

                if (bestG.TryGetValue(next, out float oldG) && tentativeG >= oldG) continue;
                bestG[next] = tentativeG;
                cameFrom[next] = cur.cell;
                open.Add(new AStarNode(next, tentativeG, Heuristic(next, end)));
            }
        }
        return null;
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);  // 曼哈顿距离
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom,
                                                    Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int cur = end;
        path.Add(cur);
        while (cameFrom.TryGetValue(cur, out Vector2Int prev))
        {
            path.Add(prev);
            cur = prev;
        }
        path.Reverse();   // 现在是 [start, ..., end]
        // 去掉起点(start 由调用方决定)
        if (path.Count > 0) path.RemoveAt(0);
        return path;
    }

    // A* 节点(用类而不是 struct,避免 List 复制)
    private class AStarNode
    {
        public Vector2Int cell;
        public float g;
        public float h;
        public AStarNode(Vector2Int c, float g_, float h_)
        {
            cell = c; g = g_; h = h_;
        }
    }

    private static bool IsWalkableWithBan(Vector2Int cell, bool allowBreakable, HashSet<Vector2Int> banned)
    {
        if (!IsWalkable(cell, allowBreakable)) return false;
        if (banned != null && banned.Contains(cell)) return false;
        return true;
    }

    // 「优先走空地」路径规划:用 A* 算法,先尝试不开砖墙的路径,失败再尝试允许破砖墙。
    // out usedBreakable 表示最终路径是否依赖破砖(让 Enemy 知道是否主动开火)
    public static List<Vector2Int> FindPathPreferOpen(Vector2Int start, Vector2Int end,
                                                     HashSet<Vector2Int> banned,
                                                     out bool usedBreakable)
    {
        // 第一轮:走空地优先(砖墙当阻挡)
        List<Vector2Int> path = FindPathAStar(start, end, allowBreakable: false, banned);
        if (path != null)
        {
            usedBreakable = false;
            return path;
        }
        // 第二轮:允许破砖墙(实在绕不开时)
        path = FindPathAStar(start, end, allowBreakable: true, banned);
        if (path != null)
        {
            usedBreakable = true;
            return path;
        }
        usedBreakable = false;
        return null;
    }

    // 统计一条路径经过的「可碎砖墙」格子数(用于判断是否值得付出多发子弹)
    public static int CountBreakableAlongPath(List<Vector2Int> path)
    {
        if (path == null) return int.MaxValue;
        int cost = 0;
        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int c = path[i];
            if (c.x < 0 || c.x >= Cols || c.y < 0 || c.y >= Rows) continue;
            if (cellType != null && cellType[c.x, c.y] == BreakableWall) cost++;
        }
        return cost;
    }
}
