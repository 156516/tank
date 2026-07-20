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

    // 攻击 / 转向冷却
    private float fireTimer;          // 距离上一次攻击的累积时间
    private float retargetTimer;      // 多久重新选一次目标

    // 上一次同步等级的时间(用于运行时升级)
    private float lastAppliedLevel = -1;
    private float syncTimer;

    // AI 调参
    public float fireCooldown = 1.5f;
    public float detectRange = 1.0f;        // 前方射线长度

    // HeartChaser「绕心刺人」:路上遇到玩家,临时切去打这个玩家
    public float opportunisticRadius = 3.0f;

    // ——地图物体分类——
    // 前方检测可识别的物体类型;不同类型采取不同策略
    public enum BlockType { None, BreakableWall, SteelWall, Heart, EnemyTeammate, River, Grass, Other }

    // ——AI 协调策略——
    [Header("AI Role Distribution")]
    [Tooltip("出生时 roll < heartChaserChance -> 一心冲 Heart")]
    public float heartChaserChance = 0.5f;
    [Tooltip("追击玩家的概率(在非心机者里的比例,例如 0.7 = 心机者之外 70% 追玩家,30% 随机晃动)")]
    public float playerChaserShareInNonHearts = 0.7f;

    // 贴图 / 预制体
    private SpriteRenderer sr;
    public Sprite[] tankSprite; // 上、下、左、右
    public GameObject bulletPrefab;
    public GameObject explosionPrefab;

    // ——角色分配——
    public enum EnemyRole { HeartChaser, PlayerChaser, RandomWalker }
    private EnemyRole role;

    // 当前目标
    private Transform currentTarget;
    // HeartChaser 临时切去打玩家的「机会目标」,打掉 / 离开范围后清除
    private Transform opportunisticKill;

    // ——DFS 路径规划——
    private List<Vector2Int> pathPoints;     // 包含 [end, p1, p2, ...]
    private int pathIndex;                   // 当前正前往 pathPoints 中的第几个格子
    private float nextPathPlanTime;          // 下一次重算路径的绝对时间
    private float arrivalThreshold = 0.25f;  // 进入"到达当前格点"的距离阈值

    // 保存 Inspector 中原始参数,作为难度缩放的基准
    private float baseMoveSpeed;
    private float baseFireCooldown;
    private float baseOpportunisticRadius;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // 显式 axis-aligned 起始状态:水平方向 = 0,垂直方向 = -1(向下)
        h = 0f;
        v = -1f;
        // 略微错开开火时机,避免多只敌人同时开火
        fireTimer = Random.Range(0f, fireCooldown);
        // 决定本 enemy 一生扮演的角色(不会再变)
        AssignRole();
        // 把 Inspector 默认值记录下来,后续用作难度缩放基准
        baseMoveSpeed = moveSpeed;
        baseFireCooldown = fireCooldown;
        baseOpportunisticRadius = opportunisticRadius;
        // 出生时立即按当前难度同步一次
        SyncDifficulty();
    }

    // 出生时摇一次,决定此后走哪套决策
    private void AssignRole()
    {
        float roll = Random.value;
        if (roll < heartChaserChance)
        {
            role = EnemyRole.HeartChaser;
        }
        else
        {
            // 非心机者中,按 playerChaserShareInNonHearts 分配:追玩家 vs 随机晃
            float remaining = Random.value;
            role = (remaining < playerChaserShareInNonHearts)
                ? EnemyRole.PlayerChaser
                : EnemyRole.RandomWalker;
        }
    }

    void Start()
    {
        Retarget();
    }

    void Update()
    {
        // 攻击冷却
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireCooldown)
        {
            AttackMethod();
            fireTimer = 0f;
        }

        // 每 2 秒同步一次难度,等级变化时升级自身属性
        syncTimer += Time.deltaTime;
        if (syncTimer >= 2.0f)
        {
            SyncDifficulty();
            syncTimer = 0f;
        }

        // HeartChaser 机会目标:路上有玩家 → 临时切换去打
        if (role == EnemyRole.HeartChaser)
        {
            UpdateOpportunisticTarget();
        }

        // 周期性地重新选目标
        retargetTimer += Time.deltaTime;
        if (retargetTimer >= 0.5f || currentTarget == null)
        {
            Retarget();
            retargetTimer = 0f;
        }

        // 决定这一帧真正用的目标
        currentTarget = opportunisticKill != null ? opportunisticKill : currentTarget;

        // 防御:目标 GameObject 可能在这一帧被 Destroy(玩家阵亡),清掉野指针
        if (currentTarget == null ||
            (currentTarget.gameObject != null && !currentTarget.gameObject.activeInHierarchy))
        {
            currentTarget = null;
        }
    }

    // HeartChaser 的"绕心刺人"逻辑:
    // - 如果 opportunistic 还在 / 在范围内,继续追它
    // - 否则看周围有没有玩家,有就锁定
    // - 直到玩家死亡 / 离开范围,才回到 Heart
    private void UpdateOpportunisticTarget()
    {
        if (opportunisticKill != null)
        {
            // 当前有临时目标:检查是否还能继续追
            if (!opportunisticKill.gameObject.activeInHierarchy ||
                Vector3.Distance(transform.position, opportunisticKill.position) > opportunisticRadius * 1.5f)
            {
                // 失效,释放占用
                EnemyCoordination.Release(opportunisticKill.gameObject, this);
                opportunisticKill = null;
            }
            return;
        }
        // 没有临时目标 → 在周围扫描一个未锁满的玩家
        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Tank");
        float bestDist = opportunisticRadius * opportunisticRadius;
        Transform best = null;
        foreach (GameObject t in tanks)
        {
            if (t == null || !t.activeInHierarchy) continue;
            if (t.GetComponent<Player>() == null) continue;
            // 协调:已经被锁满就跳过,否则会变成全场 4 个 enemy 全盯一个玩家
            int lockedBy = EnemyCoordination.GetClaimCount(t.gameObject);
            if (lockedBy >= EnemyCoordination.MaxLockOnPlayer) continue;
            float d = (t.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = t.transform;
            }
        }
        opportunisticKill = best;
        // 临时目标也要占一个协调位(避免多个 HeartChaser 同时切去打一个玩家)
        if (opportunisticKill != null)
        {
            EnemyCoordination.TryClaim(opportunisticKill.gameObject, this);
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

    // 按角色分配 + 协调锁定,决定追逐目标
    private void Retarget()
    {
        // 先释放旧目标占用(防止泄漏)
        GameObject oldTargetGo = (currentTarget != null && currentTarget.gameObject != null)
            ? currentTarget.gameObject : null;
        EnemyCoordination.Release(oldTargetGo, this);

        switch (role)
        {
            case EnemyRole.HeartChaser:
                currentTarget = SelectTargetWithCoordination(preferHeart: true);
                break;
            case EnemyRole.PlayerChaser:
                currentTarget = SelectTargetWithCoordination(preferHeart: false);
                break;
            case EnemyRole.RandomWalker:
                currentTarget = null;
                break;
        }
    }

    // 协调选择目标:扫描候选(Heart / 玩家),按角色偏好 + 距离 + 锁定数挑选
    // 偏好 Heart 的角色会优先 Heart;锁满就换其次目标;全锁满就放弃(返回 null)
    private Transform SelectTargetWithCoordination(bool preferHeart)
    {
        // 收集所有候选目标(Heart + 活着的 Player)
        List<Transform> heartCandidates = new List<Transform>();
        List<Transform> playerCandidates = new List<Transform>();

        GameObject heart = GameObject.FindGameObjectWithTag("Heart");
        if (heart != null && heart.activeInHierarchy) heartCandidates.Add(heart.transform);

        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Tank");
        foreach (GameObject t in tanks)
        {
            if (t == null || !t.activeInHierarchy) continue;
            if (t.GetComponent<Player>() == null) continue;
            playerCandidates.Add(t.transform);
        }

        // 距离排序:就近优先
        Vector3 selfPos = transform.position;
        playerCandidates.Sort((a, b) =>
            (a.position - selfPos).sqrMagnitude.CompareTo((b.position - selfPos).sqrMagnitude));

        // 按角色偏好依次尝试:HeartChaser 首选 Heart,被锁满再选玩家;
        // PlayerChaser 优先最近玩家,被锁满再退而求 Heart 或其他
        System.Action<List<Transform>> tryClaim = (list) =>
        {
            foreach (Transform t in list)
            {
                if (EnemyCoordination.TryClaim(t.gameObject, this)) return;
            }
        };

        if (preferHeart)
        {
            // 先尝试 Heart,失败再尝试玩家
            if (heartCandidates.Count > 0 && EnemyCoordination.TryClaim(heartCandidates[0].gameObject, this))
                return heartCandidates[0];
            foreach (var p in playerCandidates)
            {
                if (EnemyCoordination.TryClaim(p.gameObject, this)) return p;
            }
        }
        else
        {
            // 先尝试最近玩家,失败再退而求 Heart
            foreach (var p in playerCandidates)
            {
                if (EnemyCoordination.TryClaim(p.gameObject, this)) return p;
            }
            if (heartCandidates.Count > 0 && EnemyCoordination.TryClaim(heartCandidates[0].gameObject, this))
                return heartCandidates[0];
        }
        return null;
    }

    // 找 Heart(若已被毁,返回 null,此时 HeartChaser 会走随机路径)
    private Transform FindHeart()
    {
        GameObject heart = GameObject.FindGameObjectWithTag("Heart");
        if (heart != null && heart.activeInHierarchy) return heart.transform;
        return null;
    }

    // 找最近的、活着的玩家坦克
    private Transform FindClosestPlayer()
    {
        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Tank");
        float bestDist = float.MaxValue;
        Transform best = null;
        foreach (GameObject t in tanks)
        {
            if (t == null || !t.activeInHierarchy) continue;
            // 只追 Player(Enemy 也是 Tank tag,但没有 Player 组件)
            if (t.GetComponent<Player>() == null) continue;
            float d = (t.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = t.transform;
            }
        }
        return best;
    }

    // 主移动:用 DFS 路径走,同时保持地图感知(撞墙/打墙)
    private void MoveMethod()
    {
        BlockType front = DetectFront();

        // 当前 path 节点已到达(进入阈值内)?前进到下一个
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

        if (needReplan)
        {
            PlanPath();
            // 重算间隔受难度缩放影响:高等级更频繁重算
            nextPathPlanTime = Time.time + Random.Range(0.8f, 1.6f) / PlayerManager.Instance.GetDifficultyMultiplier();
        }
        else if (front == BlockType.BreakableWall)
        {
            // 可碎砖墙:开炮打穿,不重算路径
            if (fireTimer >= fireCooldown * 0.6f) AttackMethod();
        }
        // Grass / 开放空间:按当前 (h, v) 直接走

        // ——axis-aligned 防御——
        if (h != 0 && v != 0) v = 0;

        // 真正移动(同一帧只能沿一个轴)
        if (h != 0)
            transform.Translate(Vector3.right * h * moveSpeed * Time.fixedDeltaTime, Space.World);
        else if (v != 0)
            transform.Translate(Vector3.up * v * moveSpeed * Time.fixedDeltaTime, Space.World);
    }

    // 重新规划路径(DFS);失败时退化到 chase 直线或随机方向
    private void PlanPath()
    {
        if (currentTarget == null)
        {
            // 无目标 → 退化到随机轴-aligned 方向
            int num = Random.Range(0, 4);
            if (num == 0) { v = 1f; h = 0f; }
            else if (num == 1) { v = -1f; h = 0f; }
            else if (num == 2) { v = 0f; h = 1f; }
            else { v = 0f; h = -1f; }
            pathPoints = null;
            ApplySprite();
            return;
        }

        // 重建地图网格(砖墙状态可能改变)
        MapGrid.Rebuild();
        // 收集附近敌人位置作为本 enemy 局部 ban,让多 enemy 自然分散到不同路径
        HashSet<Vector2Int> banned = CollectNearbyEnemyBans();

        Vector2Int start = MapGrid.WorldToCell(transform.position);
        Vector2Int end = MapGrid.WorldToCell(currentTarget.position);
        // allowBreakable=true:遇到砖墙也按"可走"对待(打碎就过)
        pathPoints = MapGrid.FindPath(start, end, allowBreakable: true, banned);
        pathIndex = 0;

        if (pathPoints != null && pathPoints.Count > 0)
        {
            SteerTowards(pathPoints[0]);
        }
        else
        {
            // DFS 失败:直接朝目标走(直线,即便撞墙就交给 DetectFront 处理)
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

    // 收集附近敌人位置作为本 enemy 暂时拒绝走的格子。
    // 这样多个 enemy 撞同一目标时,各自 DFS 出不同路径,自然分散。
    private HashSet<Vector2Int> CollectNearbyEnemyBans()
    {
        HashSet<Vector2Int> banned = new HashSet<Vector2Int>();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Vector3 selfPos = transform.position;
        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject e = enemies[i];
            if (e == null || e == this.gameObject) continue;
            float sqr = (e.transform.position - selfPos).sqrMagnitude;
            if (sqr > 16f) continue;  // 4 格以内才 ban
            banned.Add(MapGrid.WorldToCell(e.transform.position));
        }
        return banned;
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

    // 详细的"前方物体分类":用 tag 区分 Wall/Barrier/Heart/Enemy,
    // 用 GameObject.name 区分 River/Grass(River/Grass prefab 没设置自定义 tag)
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
        // River / Grass 用 GameObject.name 区分
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
        // 死亡时释放占用,避免 Coordination 持有 ghost enemy 引用
        GameObject cur = (currentTarget != null && currentTarget.gameObject != null)
            ? currentTarget.gameObject : null;
        EnemyCoordination.Release(cur, this);
        GameObject opp = (opportunisticKill != null && opportunisticKill.gameObject != null)
            ? opportunisticKill.gameObject : null;
        EnemyCoordination.Release(opp, this);

        Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }

    // 兜底:敌人 GameObject 被销毁(任意原因)时释放占用
    private void OnDestroy()
    {
        if (currentTarget != null && currentTarget.gameObject != null)
            EnemyCoordination.Release(currentTarget.gameObject, this);
        if (opportunisticKill != null && opportunisticKill.gameObject != null)
            EnemyCoordination.Release(opportunisticKill.gameObject, this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.gameObject.CompareTag("Enemy"))
        {
            // 撞到同伴:立刻重算路径,绕开拥堵
            PlanPath();
            nextPathPlanTime = Time.time + Random.Range(0.3f, 1.0f);
        }
    }

    // 把当前 PlayerManager 难度应用到本 enemy 的属性上
    // mult 范围 [1.0, 2.4];mult=1 时按 Inspector 默认值,mult=2.4 时大幅强化
    private void SyncDifficulty()
    {
        if (PlayerManager.Instance == null) return;
        int lvl = PlayerManager.Instance.currentLevel;
        if (lvl == lastAppliedLevel) return;
        lastAppliedLevel = lvl;

        float mult = PlayerManager.Instance.GetDifficultyMultiplier();
        // 移动速度、攻击间隔、机会半径随难度增长
        moveSpeed = baseMoveSpeed * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(1f, 2.4f, mult));
        fireCooldown = Mathf.Max(0.5f, baseFireCooldown / Mathf.Lerp(1f, 1.5f, Mathf.InverseLerp(1f, 2.4f, mult)));
        opportunisticRadius = baseOpportunisticRadius * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(1f, 2.4f, mult));
    }
}
