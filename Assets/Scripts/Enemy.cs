using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 敌人:A* 路径规划 + grid-aligned 走向 + cell-based 撞墙检测
// "完全进入一个格子才转弯" 的实施:
//   - tank 不在 cell 中央 -> 朝 cell 中央走 (朝 -off), 不重新决策方向
//   - tank 在 cell 中央 -> 重新决定方向 (基于 A* path) + 走之前看 target cell (cell-based 转 90° / 开火)
public class Enemy : MonoBehaviour
{
    // 这两个 Inspector 值现在表示「满级(最强)」时的数值:
    //   moveSpeed    = 满级最快移动速度
    //   fireCooldown = 满级最短开火冷却
    // 实际生效值由 EnemyDifficulty 随时间从「弱」插值到这里(见 Awake / Update)。
    public float moveSpeed = 3;
    public float fireCooldown = 1.5f;

    // ---- 坦克类型差异(不同 prefab 配不同值)----
    public int maxHp = 1;          // 生命值:装甲坦克设 4,普通坦克 1(需被击中多次才死)
    public float bulletSpeed = 0f; // >0 时覆盖子弹速度(强化/快速坦克的子弹更快)
    public int scoreValue = 1;     // 击杀该坦克的得分
    private int hp;

    // 满级基准值(Awake 时从上面的 Inspector 值捕获,之后不再改动)
    private float maxMoveSpeed;
    private float minFireCooldown;

    private SpriteRenderer sr;
    public Sprite[] tankSprite;
    // 移动动画第 2 帧(履带):与 tankSprite 同索引;留空则静态不动画
    public Sprite[] tankSpriteMoving;
    public float treadAnimInterval = 0.12f;   // 履带换帧间隔(秒)
    private int treadFrame;                    // 0 = 静态帧,1 = 移动帧
    private float treadTimer;
    public GameObject bulletPrefab;
    public GameObject explosionPrefab;

    public int killerPlayerNumber = 1;

    private Vector3 bullectEulerAngles;
    private float h;
    private float v = -1;

    private float fireTimer;
    private Transform target;

    // ---- A* 路径规划 ----
    private List<Vector2Int> currentPath;
    private float pathRecalcInterval = 0.5f;
    private float pathRecalcTimer;

    // 目标切换迟滞:新目标要比当前目标近至少这么多格才切换,避免在两个目标间反复横跳
    private const int TargetSwitchMargin = 3;

    // ---- 格子对齐移动 ----
    // 关键:坦克一次只朝「一个相邻格的中心」直线走,到达后精确吸附到格心,
    // 只有站在格心时才重新决策方向。这样跨轴坐标恒为整数,不会累计漂移,撞墙检测才准确。
    private Vector2Int targetCell;   // 当前正在前往的相邻格
    private bool hasTarget;          // 是否有正在前往的目标格

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        h = 0f;
        v = -1f;
        hp = Mathf.Max(1, maxHp);

        // 捕获满级基准,并按「当前难度」立即算出生效值,保证刚生成的敌人也符合当前强度
        maxMoveSpeed = moveSpeed;
        minFireCooldown = fireCooldown;
        ApplyDifficulty();

        fireTimer = Random.Range(0f, fireCooldown);
        pathRecalcTimer = pathRecalcInterval;
        currentPath = null;
        hasTarget = false;

        AcquireTarget();
        RecalculatePath();
        ApplySprite();
    }

    // 按当前游戏时间刷新有效移动速度 / 开火冷却(随时间由弱变强)
    private void ApplyDifficulty()
    {
        moveSpeed = EnemyDifficulty.MoveSpeed(maxMoveSpeed);
        fireCooldown = EnemyDifficulty.FireCooldown(minFireCooldown);
    }

    void Update()
    {
        // 每帧刷新难度:已存活的敌人也会随时间逐渐变快、开火变密
        ApplyDifficulty();

        // 时钟道具:冻结期间不开火(移动在 Move 里同样被拦)
        if (!IsFrozen)
        {
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireCooldown)
            {
                Fire();
                fireTimer = 0f;
            }
        }

        if (target == null || (target.gameObject != null && !target.gameObject.activeInHierarchy))
        {
            AcquireTarget();
            if (currentPath != null) currentPath.Clear();
        }
    }

    void FixedUpdate()
    {
        // 时钟道具:冻结期间静止不动
        if (IsFrozen)
        {
            treadFrame = 0;
            treadTimer = 0f;
            ApplySprite();
            return;
        }
        Move();
    }

    // ---- 时钟道具:全体敌人冻结 ----
    private static float frozenUntil = 0f;
    public static bool IsFrozen { get { return Time.timeSinceLevelLoad < frozenUntil; } }
    public static void Freeze(float seconds) { frozenUntil = Time.timeSinceLevelLoad + seconds; }
    public static void ResetFreeze() { frozenUntil = 0f; }

    // ---- 手雷道具:摧毁场上所有敌人 ----
    public static void KillAll()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;
            Enemy en = enemies[i].GetComponent<Enemy>();
            if (en != null) en.DieMethod();
        }
    }

    private void AcquireTarget()
    {
        // 协作分工:在「基地 + 所有存活玩家」中,选格子曼哈顿距离最近的作为进攻目标。
        // → 离基地近的敌人去打基地,离某玩家近的去打那个玩家,自然分散不扎堆。
        Vector2Int myCell = MapGrid.WorldToCell(transform.position);
        Transform best = null;
        int bestDist = int.MaxValue;

        // 候选 1:基地
        GameObject heart = GameObject.FindGameObjectWithTag("Heart");
        if (heart != null && heart.activeInHierarchy)
        {
            best = heart.transform;
            bestDist = ManhattanTo(myCell, heart.transform.position);
        }

        // 候选 2:所有存活玩家(tag = Tank)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Tank");
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || !players[i].activeInHierarchy) continue;
            int d = ManhattanTo(myCell, players[i].transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = players[i].transform;
            }
        }

        // 迟滞:当前目标仍有效且新目标没有「明显更近」时,保持当前目标,避免来回抖动切换
        if (target != null && target.gameObject != null && target.gameObject.activeInHierarchy)
        {
            int curDist = ManhattanTo(myCell, target.position);
            if (bestDist + TargetSwitchMargin >= curDist) return;
        }

        target = best;
    }

    // 以格子为单位的曼哈顿距离(与坦克贴格移动一致,比欧氏距离更贴近实际步数)
    private int ManhattanTo(Vector2Int fromCell, Vector3 worldPos)
    {
        Vector2Int c = MapGrid.WorldToCell(worldPos);
        return Mathf.Abs(c.x - fromCell.x) + Mathf.Abs(c.y - fromCell.y);
    }

    private void Fire()
    {
        GameObject b = Instantiate(bulletPrefab, transform.position,
            Quaternion.Euler(transform.eulerAngles + bullectEulerAngles));
        if (bulletSpeed > 0f && b != null)
        {
            Bullet bb = b.GetComponent<Bullet>();
            if (bb != null) bb.moveSpeed = bulletSpeed;   // 强化坦克:更快的子弹
        }
    }

    private void Move()
    {
        // 1. 路径需要时重算(只更新 currentPath,不打断当前正在走的这一格)
        pathRecalcTimer += Time.fixedDeltaTime;
        if (pathRecalcTimer >= pathRecalcInterval ||
            currentPath == null ||
            currentPath.Count == 0)
        {
            pathRecalcTimer = 0f;
            AcquireTarget();      // 随玩家移动重新分工:离谁近就改打谁(带迟滞)
            RecalculatePath();
        }

        float step = moveSpeed * Time.fixedDeltaTime;

        // 2. 没有正在前往的目标格 → 站在格心重新决策方向
        if (!hasTarget)
        {
            if (!DecideDirection())
            {
                // 决策要求停步(面前是砖墙需先开火 / 转向后仍撞墙)→ 本帧不移动,履带停
                treadTimer = 0f;
                treadFrame = 0;
                ApplySprite();
                return;
            }
        }

        // 3. 朝目标格中心直线移动(targetCell 必为轴向相邻,移动纯粹沿单轴,跨轴坐标恒定)
        Vector3 tw = MapGrid.CellToWorld(targetCell);
        Vector3 pos = transform.position;
        Vector3 delta = tw - pos;
        float dist = delta.magnitude;
        if (dist <= step || dist < 1e-4f)
        {
            // 到达 → 精确吸附到格心,消除累计漂移;清目标,下一帧再决策
            transform.position = new Vector3(tw.x, tw.y, pos.z);
            hasTarget = false;
        }
        else
        {
            transform.position = pos + (delta / dist) * step;
        }

        // 履带动画:移动中按间隔交替第 2 帧
        treadTimer += Time.fixedDeltaTime;
        if (treadTimer >= treadAnimInterval) { treadTimer = 0f; treadFrame ^= 1; }

        ApplySprite();
    }

    // 站在格心决策下一步方向:
    //   返回 true  → 已设定 targetCell,本帧继续移动
    //   返回 false → 本帧应停步(砖墙待炸 / 无路可走)
    private bool DecideDirection()
    {
        Vector2Int myCell = MapGrid.WorldToCell(transform.position);
        // 先精确吸附到当前格心,清除历史漂移,保证后续撞墙检测基于真实格子
        Vector3 c = MapGrid.CellToWorld(myCell);
        transform.position = new Vector3(c.x, c.y, transform.position.z);

        // 最高优先级:检测来袭玩家子弹,能躲就侧身让开弹道
        Vector2Int dodgeDir;
        if (TryGetDodgeDirection(myCell, out dodgeDir))
        {
            h = dodgeDir.x;
            v = dodgeDir.y;
            targetCell = myCell + dodgeDir;
            hasTarget = true;
            pathRecalcTimer = pathRecalcInterval;   // 躲完下帧立即重算路径回到航线
            return true;
        }

        // 期望方向:优先 A* path,其次朝 Heart 主轴,再次随机
        Vector2Int desired = Vector2Int.zero;
        Vector2Int next;
        if (TryGetNextStep(out next))
        {
            desired = new Vector2Int(next.x - myCell.x, next.y - myCell.y);
        }
        else if (target != null)
        {
            Vector3 diff = target.position - transform.position;
            if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
                desired = new Vector2Int((int)Mathf.Sign(diff.x), 0);
            else
                desired = new Vector2Int(0, (int)Mathf.Sign(diff.y));
        }
        if (desired == Vector2Int.zero)
        {
            desired = RandomDir();
        }

        // path 期望方向(撞墙转 90° 时用来决定往哪一侧转)
        int pathDx = desired.x, pathDy = desired.y;

        // 应用方向(供 sprite / 开火朝向)
        h = desired.x;
        v = desired.y;

        // 撞墙检测:查目标格类型
        Vector2Int tgt = myCell + desired;
        int tType = MapGrid.GetCellType(tgt);

        if (tType == MapGrid.HeartCell)
        {
            // 已抵达基地旁 → 面向基地开火并停步,绝不进入(避免左右乱晃)
            if (fireTimer >= fireCooldown)
            {
                Fire();
                fireTimer = 0f;
            }
            return false;
        }

        if (tType == MapGrid.PermanentBlock)
        {
            // 铁块 / 河流(永久阻挡)→ 朝 path 期望方向转 90°
            PickPerpendicularDirection(pathDx, pathDy);
            pathRecalcTimer = pathRecalcInterval;   // 下帧强制重算路径
            desired = new Vector2Int((int)h, (int)v);
            tgt = myCell + desired;
            tType = MapGrid.GetCellType(tgt);
            if (tType == MapGrid.PermanentBlock || tType == MapGrid.HeartCell)
            {
                // 转 90° 后仍是阻挡/基地 → 本帧停步,避免钻墙
                return false;
            }
        }

        if (tType == MapGrid.BreakableWall)
        {
            // 砖墙 → 冷却好了就开火炸墙,本帧停步等待(不逐帧刷子弹)
            if (fireTimer >= fireCooldown)
            {
                Fire();
                fireTimer = 0f;
            }
            return false;
        }

        // 目标格可走 → 锁定,开始朝它移动
        targetCell = tgt;
        hasTarget = true;
        return true;
    }

    private Vector2Int RandomDir()
    {
        int n = Random.Range(0, 4);
        if (n == 0) return new Vector2Int(0, 1);
        if (n == 1) return new Vector2Int(0, -1);
        if (n == 2) return new Vector2Int(1, 0);
        return new Vector2Int(-1, 0);
    }

    // ---- 躲避子弹 ----
    private const float DodgeReactRange = 6f;      // 满级(熟练度=1)时的最大反应距离(世界单位≈格数);实际按熟练度缩放
    private const float DodgeCorridorHalf = 0.5f;  // 判定"在同一条弹道上"的横向半宽

    // 检测来袭玩家子弹:若有子弹正沿弹道朝自己飞来,返回一个垂直于弹道、指向可走空地的逃离方向。
    private bool TryGetDodgeDirection(Vector2Int myCell, out Vector2Int dodgeDir)
    {
        dodgeDir = Vector2Int.zero;

        // 反应距离随躲避熟练度增长:开局熟练度=0 → 距离=0 → 完全不躲;随时间越躲越远、越灵。
        float reactRange = DodgeReactRange * EnemyDifficulty.DodgeSkill01;
        if (reactRange < 0.5f) return false;

        Vector2 myPos = transform.position;

        Bullet[] bullets = Object.FindObjectsOfType<Bullet>();
        bool threat = false;
        float nearest = float.MaxValue;
        Vector2 threatBulletDir = Vector2.zero;
        Vector2 threatLateral = Vector2.zero;

        for (int i = 0; i < bullets.Length; i++)
        {
            Bullet b = bullets[i];
            if (b == null || !b.isPlayerBullet) continue;   // 敌方子弹伤不到自己,只躲玩家子弹

            Vector2 dir = QuantizeDir(b.transform.up);       // 子弹前进方向(量化到四轴)
            Vector2 toEnemy = myPos - (Vector2)b.transform.position;

            float forward = Vector2.Dot(toEnemy, dir);       // 沿弹道方向的距离(需 >0 = 在子弹前方)
            if (forward <= 0f || forward > reactRange) continue;

            Vector2 lateral = toEnemy - forward * dir;       // 相对弹道中心的横向偏移
            if (lateral.magnitude > DodgeCorridorHalf) continue;  // 不在同一条弹道走廊内

            if (forward < nearest)
            {
                nearest = forward;
                threatBulletDir = dir;
                threatLateral = lateral;
                threat = true;
            }
        }

        if (!threat) return false;

        // 逃离方向 = 垂直于弹道;两侧都试,优先朝已偏出的一侧,选可走空地
        Vector2Int perp = PerpendicularOf(threatBulletDir);
        Vector2Int optA = perp;
        Vector2Int optB = new Vector2Int(-perp.x, -perp.y);
        if (Vector2.Dot(threatLateral, new Vector2(perp.x, perp.y)) < 0f)
        {
            Vector2Int tmp = optA; optA = optB; optB = tmp;
        }

        if (MapGrid.GetCellType(myCell + optA) == MapGrid.Walkable) { dodgeDir = optA; return true; }
        if (MapGrid.GetCellType(myCell + optB) == MapGrid.Walkable) { dodgeDir = optB; return true; }
        return false;   // 两侧都堵,躲不了,交给正常逻辑
    }

    // 把任意向量量化到最接近的四轴单位向量
    private static Vector2 QuantizeDir(Vector2 v)
    {
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y)) return new Vector2(Mathf.Sign(v.x), 0f);
        return new Vector2(0f, Mathf.Sign(v.y));
    }

    // 弹道水平 → 上下逃;弹道垂直 → 左右逃
    private static Vector2Int PerpendicularOf(Vector2 bulletDir)
    {
        if (Mathf.Abs(bulletDir.x) > Mathf.Abs(bulletDir.y)) return new Vector2Int(0, 1);
        return new Vector2Int(1, 0);
    }

    // 朝 path 期望方向转 90°(否则随机)
    private void PickPerpendicularDirection(int pathDx, int pathDy)
    {
        if (h != 0f)
        {
            if (pathDy > 0) v = 1f;
            else if (pathDy < 0) v = -1f;
            else v = Random.value > 0.5f ? 1f : -1f;
            h = 0f;
        }
        else if (v != 0f)
        {
            if (pathDx > 0) h = 1f;
            else if (pathDx < 0) h = -1f;
            else h = Random.value > 0.5f ? 1f : -1f;
            v = 0f;
        }
        else
        {
            int n = Random.Range(0, 4);
            if (n == 0) { v = 1f;  h = 0f; }
            else if (n == 1) { v = -1f; h = 0f; }
            else if (n == 2) { h = 1f;  v = 0f; }
            else             { h = -1f; v = 0f; }
        }
    }

    // A* 重算到 Heart 的路径;banned = 同伙当前所在格让他们错开
    private void RecalculatePath()
    {
        if (target == null)
        {
            currentPath = null;
            return;
        }
        Vector2Int start = MapGrid.WorldToCell(transform.position);
        Vector2Int end = MapGrid.WorldToCell(target.position);
        if (!MapGrid.IsWalkable(end, false))
        {
            end = GetApproachCell(end);
        }
        if (start == end)
        {
            currentPath = new List<Vector2Int> { end };
            return;
        }

        HashSet<Vector2Int> banned = MapGrid.GetEnemyOccupiedCells();
        currentPath = MapGrid.FindPathPreferOpen(start, end, banned, out _);
        if (currentPath == null)
        {
            currentPath = null;
        }
    }

    private Vector2Int GetApproachCell(Vector2Int goal)
    {
        Vector2Int[] dirs = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };
        for (int i = 0; i < dirs.Length; i++)
        {
            Vector2Int nb = goal + dirs[i];
            if (MapGrid.IsWalkable(nb, false)) return nb;
        }
        return goal;
    }

    private bool TryGetNextStep(out Vector2Int next)
    {
        next = default(Vector2Int);
        if (currentPath == null || currentPath.Count == 0) return false;
        Vector2Int myCellCoord = MapGrid.WorldToCell(transform.position);
        for (int i = 0; i < currentPath.Count; i++)
        {
            Vector2Int c = currentPath[i];
            int dx = Mathf.Abs(c.x - myCellCoord.x);
            int dy = Mathf.Abs(c.y - myCellCoord.y);
            if (dx + dy == 1)
            {
                next = c;
                return true;
            }
        }
        currentPath = null;
        return false;
    }

    private void ApplySprite()
    {
        int dir = -1;
        if (h > 0) { dir = 1; bullectEulerAngles = new Vector3(0, 0, -90); }
        else if (h < 0) { dir = 3; bullectEulerAngles = new Vector3(0, 0, 90); }
        else if (v > 0) { dir = 0; bullectEulerAngles = new Vector3(0, 0, 0); }
        else if (v < 0) { dir = 2; bullectEulerAngles = new Vector3(0, 0, -180); }
        if (dir >= 0) sr.sprite = TreadSprite(dir);
    }

    // 选取当前方向要显示的帧:移动帧就绪且处于交替相位时用第 2 帧,否则用静态帧
    private Sprite TreadSprite(int dir)
    {
        if (treadFrame == 1 && tankSpriteMoving != null &&
            dir < tankSpriteMoving.Length && tankSpriteMoving[dir] != null)
        {
            return tankSpriteMoving[dir];
        }
        return tankSprite[dir];
    }

    // 被玩家子弹击中:扣血,血尽才死(装甲坦克需多次击中)
    public void Hit(int killerPlayer)
    {
        killerPlayerNumber = killerPlayer;
        hp--;
        if (hp <= 0)
        {
            DieMethod();
        }
    }

    public void DieMethod()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.AddScore(killerPlayerNumber, scoreValue);
        }
        Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }
}
