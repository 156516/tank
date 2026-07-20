using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float moveSpeed = 3;
    private Vector3 bullectEulerAngles;
    private float h;
    private float v = -1;

    // 击杀本敌人的玩家编号(由 Bullet 在击中时写入)
    public int killerPlayerNumber = 1;

    // 攻击 / 寻路冷却
    private float fireTimer;
    private float retargetTimer;

    // 上一次同步等级的时间
    private float lastAppliedLevel = -1;
    private float syncTimer;

    // AI 调参
    public float fireCooldown = 1.5f;
    public float detectRange = 1.0f;

    // 破墙预算:一条路径需要打几堵砖墙才算"划算"
    public int maxWallCost = 1;

    // ——地图物体分类——
    public enum BlockType { None, BreakableWall, SteelWall, Heart, EnemyTeammate, River, Grass, Other }

    // 贴图 / 预制体
    private SpriteRenderer sr;
    public Sprite[] tankSprite;
    public GameObject bulletPrefab;
    public GameObject explosionPrefab;

    // 当前目标(由 SelectTargetByDistance 选定,供 PlanPath / Opportunistic 使用)
    private Transform currentTarget;

    // ——DFS 路径规划——
    private List<Vector2Int> pathPoints;
    private int pathIndex;
    private float nextPathPlanTime;
    private float arrivalThreshold = 0.25f;

    // ——时间窗口 ban——
    private readonly Queue<Vector2Int> recentCells = new Queue<Vector2Int>();
    private const int RecentMemory = 5;
    private Vector2Int lastCell;

    // Inspector 默认值,用于难度缩放
    private float baseMoveSpeed;
    private float baseFireCooldown;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // 显式 axis-aligned 起始
        h = 0f;
        v = -1f;
        fireTimer = Random.Range(0f, fireCooldown);
        baseMoveSpeed = moveSpeed;
        baseFireCooldown = fireCooldown;
        SyncDifficulty();
    }

    void Start()
    {
        Retarget();
    }

    void Update()
    {
        // 攻击冷却:看到砖墙就开火
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireCooldown)
        {
            if (IsBreakableWallInFront())
            {
                AttackMethod();
                fireTimer = 0f;
            }
        }

        // 每 2 秒同步一次难度
        syncTimer += Time.deltaTime;
        if (syncTimer >= 2.0f)
        {
            SyncDifficulty();
            syncTimer = 0f;
        }

        // 周期重选目标(按距离)
        retargetTimer += Time.deltaTime;
        if (retargetTimer >= 0.5f || currentTarget == null)
        {
            Retarget();
            retargetTimer = 0f;
        }

        // 野指针防御:目标 GameObject 可能这一帧被 Destroy
        if (currentTarget == null ||
            (currentTarget.gameObject != null && !currentTarget.gameObject.activeInHierarchy))
        {
            currentTarget = null;
        }
    }

    private void FixedUpdate()
    {
        MoveMethod();
    }

    private void AttackMethod()
    {
        Instantiate(bulletPrefab, transform.position, Quaternion.Euler(transform.eulerAngles + bullectEulerAngles));
    }

    // 核心:按距离选择目标 — 离玩家近就追玩家,离 Heart 近就追 Heart
    // 配合 EnemyCoordination.TryClaim 锁定,每个目标最多被 2 个 enemy 围攻
    private void Retarget()
    {
        // 先释放旧目标占用
        GameObject oldTargetGo = (currentTarget != null && currentTarget.gameObject != null)
            ? currentTarget.gameObject : null;
        EnemyCoordination.Release(oldTargetGo, this);

        currentTarget = SelectTargetByDistance();
    }

    // 收集候选目标(Heart + 存活玩家),按距离排序,锁定第一个可用
    private Transform SelectTargetByDistance()
    {
        Vector3 self = transform.position;
        List<Transform> candidates = new List<Transform>();

        GameObject heart = GameObject.FindGameObjectWithTag("Heart");
        if (heart != null && heart.activeInHierarchy)
        {
            candidates.Add(heart.transform);
        }

        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Tank");
        for (int i = 0; i < tanks.Length; i++)
        {
            GameObject t = tanks[i];
            if (t == null || !t.activeInHierarchy) continue;
            if (t.GetComponent<Player>() == null) continue;
            candidates.Add(t.transform);
        }

        if (candidates.Count == 0) return null;

        // 按距离升序排序
        candidates.Sort((a, b) =>
            (a.position - self).sqrMagnitude.CompareTo((b.position - self).sqrMagnitude));

        // 锁第一个还能装的;都装不下就返回 null(fallback 走随机)
        for (int i = 0; i < candidates.Count; i++)
        {
            if (EnemyCoordination.TryClaim(candidates[i].gameObject, this))
                return candidates[i];
        }
        return null;
    }

    // 主移动:DFS 路径走,撞墙换路线,砖墙开火
    private void MoveMethod()
    {
        BlockType front = DetectFront();
        UpdateRecentCells();

        // 当前节点已到达?前进到下一个
        if (pathPoints != null && pathIndex < pathPoints.Count)
        {
            Vector3 wp = MapGrid.CellToWorld(pathPoints[pathIndex]);
            if (Vector3.Distance(transform.position, wp) < arrivalThreshold)
            {
                pathIndex++;
                if (pathIndex < pathPoints.Count)
                {
                    SteerTowards(pathPoints[pathIndex]);
                }
                else
                {
                    PlanPath();
                }
            }
        }

        // 是否需要重算路径?
        bool needReplan = false;
        if (pathPoints == null || pathIndex >= pathPoints.Count) needReplan = true;
        if (Time.time >= nextPathPlanTime) needReplan = true;
        // 前方不可穿过的阻挡:立刻换路线
        if (front == BlockType.SteelWall || front == BlockType.Heart || front == BlockType.River || front == BlockType.EnemyTeammate)
        {
            needReplan = true;
        }
        // 前方是砖墙:让 PlanPath 重新规划(DFS 优先尝试不开墙的绕路);Update 会照常开火打碎它
        if (front == BlockType.BreakableWall)
        {
            needReplan = true;
        }

        if (needReplan)
        {
            PlanPath();
            nextPathPlanTime = Time.time + Random.Range(0.8f, 1.6f) / PlayerManager.Instance.GetDifficultyMultiplier();
        }
        // Grass / 开放空间:按当前 (h, v) 直接走

        // axis-aligned 防御
        if (h != 0 && v != 0) v = 0;

        // 真正移动
        if (h != 0)
            transform.Translate(Vector3.right * h * moveSpeed * Time.fixedDeltaTime, Space.World);
        else if (v != 0)
            transform.Translate(Vector3.up * v * moveSpeed * Time.fixedDeltaTime, Space.World);
    }

    // 重新规划路径(优先走空地,实在绕不开才破墙)
    private void PlanPath()
    {
        if (currentTarget == null)
        {
            // 无目标:随机 axis-aligned 走
            int num = Random.Range(0, 4);
            if (num == 0) { v = 1f; h = 0f; }
            else if (num == 1) { v = -1f; h = 0f; }
            else if (num == 2) { v = 0f; h = 1f; }
            else { v = 0f; h = -1f; }
            pathPoints = null;
            ApplySprite();
            return;
        }

        MapGrid.Rebuild();
        HashSet<Vector2Int> banned = CollectNearbyEnemyBans();

        Vector2Int start = MapGrid.WorldToCell(transform.position);
        Vector2Int end = MapGrid.WorldToCell(currentTarget.position);

        // A* 优先走空地,失败再尝试破墙
        bool usedBreak;
        pathPoints = MapGrid.FindPathPreferOpen(start, end, banned, out usedBreak);

        // 破墙代价太高就放弃这条路,fallback 朝目标直线;Update 看到砖墙会主动开火打碎
        if (usedBreak && MapGrid.CountBreakableAlongPath(pathPoints) > maxWallCost)
        {
            pathPoints = null;
        }
        pathIndex = 0;

        if (pathPoints != null && pathPoints.Count > 0)
        {
            SteerTowards(pathPoints[0]);
        }
        else
        {
            Vector3 diff = currentTarget.position - transform.position;
            if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
            {
                h = Mathf.Sign(diff.x);
                v = 0;
            }
            else
            {
                v = Mathf.Sign(diff.y);
                h = 0;
            }
            ApplySprite();
        }
    }

    // 收集附近敌人位置 + 自己最近走过的格子作为 ban 集,DFS/A* 不会走这些
    private HashSet<Vector2Int> CollectNearbyEnemyBans()
    {
        HashSet<Vector2Int> banned = new HashSet<Vector2Int>();
        foreach (var c in recentCells)
        {
            banned.Add(c);
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Vector3 selfPos = transform.position;
        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject e = enemies[i];
            if (e == null || e == this.gameObject) continue;
            float sqr = (e.transform.position - selfPos).sqrMagnitude;
            if (sqr > 16f) continue;
            banned.Add(MapGrid.WorldToCell(e.transform.position));
        }
        return banned;
    }

    // 每帧检查:进入新格子就压入 recentCells,超过容量出队
    private void UpdateRecentCells()
    {
        Vector2Int cur = MapGrid.WorldToCell(transform.position);
        if (cur == lastCell) return;
        lastCell = cur;
        recentCells.Enqueue(cur);
        while (recentCells.Count > RecentMemory)
        {
            recentCells.Dequeue();
        }
    }

    // 朝目标格子转方向(axis-aligned)
    private void SteerTowards(Vector2Int cell)
    {
        Vector3 wp = MapGrid.CellToWorld(cell);
        Vector3 diff = wp - transform.position;
        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
        {
            h = Mathf.Sign(diff.x);
            v = 0;
        }
        else
        {
            v = Mathf.Sign(diff.y);
            h = 0;
        }
        ApplySprite();
    }

    // 仅检测前方是否有可碎砖墙(tag = "Wall")。用于 AttackMethod 触发判定。
    private bool IsBreakableWallInFront()
    {
        Vector3 dir = new Vector3(h, v, 0);
        if (dir == Vector3.zero) return false;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, detectRange);
        return hit.collider != null && hit.collider.CompareTag("Wall");
    }

    // 详细的前方物体分类
    private BlockType DetectFront()
    {
        Vector3 dir = new Vector3(h, v, 0);
        if (dir == Vector3.zero) return BlockType.None;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, detectRange);
        if (hit.collider == null) return BlockType.None;

        string tag = hit.collider.tag;
        if (tag == "Wall") return BlockType.BreakableWall;
        if (tag == "Barrier") return BlockType.SteelWall;
        if (tag == "Heart") return BlockType.Heart;
        if (tag == "Enemy") return BlockType.EnemyTeammate;
        if (hit.collider.gameObject.name == "River") return BlockType.River;
        if (hit.collider.gameObject.name == "Grass") return BlockType.Grass;
        return BlockType.Other;
    }

    // 根据当前 h / v 切换精灵与子弹朝向
    private void ApplySprite()
    {
        if (h > 0) { sr.sprite = tankSprite[1]; bullectEulerAngles = new Vector3(0, 0, -90); }
        else if (h < 0) { sr.sprite = tankSprite[3]; bullectEulerAngles = new Vector3(0, 0, 90); }
        else if (v > 0) { sr.sprite = tankSprite[0]; bullectEulerAngles = new Vector3(0, 0, 0); }
        else if (v < 0) { sr.sprite = tankSprite[2]; bullectEulerAngles = new Vector3(0, 0, -180); }
    }

    private void DieMethod()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.AddScore(killerPlayerNumber);
        }
        // 死亡时释放占用
        GameObject cur = (currentTarget != null && currentTarget.gameObject != null)
            ? currentTarget.gameObject : null;
        EnemyCoordination.Release(cur, this);

        Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }

    // 兜底:GameObject 被销毁时释放占用
    private void OnDestroy()
    {
        if (currentTarget != null && currentTarget.gameObject != null)
            EnemyCoordination.Release(currentTarget.gameObject, this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.gameObject.CompareTag("Enemy"))
        {
            PlanPath();
            nextPathPlanTime = Time.time + Random.Range(0.3f, 1.0f);
        }
    }

    // 难度升级
    private void SyncDifficulty()
    {
        if (PlayerManager.Instance == null) return;
        int lvl = PlayerManager.Instance.currentLevel;
        if (lvl == lastAppliedLevel) return;
        lastAppliedLevel = lvl;

        float mult = PlayerManager.Instance.GetDifficultyMultiplier();
        moveSpeed = baseMoveSpeed * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(1f, 2.4f, mult));
        fireCooldown = Mathf.Max(0.5f, baseFireCooldown / Mathf.Lerp(1f, 1.5f, Mathf.InverseLerp(1f, 2.4f, mult)));
    }
}
