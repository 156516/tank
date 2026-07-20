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
    private float nextChangeTime;     // 距离下一次强制改变方向的绝对时间(Time.time)
    private float retargetTimer;      // 多久重新选一次目标

    // 上一次同步等级的时间(用于运行时升级)
    private float lastAppliedLevel = -1;
    private float syncTimer;

    // AI 调参
    public float fireCooldown = 1.5f;
    public float changeDirMin = 1.5f;
    public float changeDirMax = 3.0f;
    public float chaseProbability = 0.78f;  // 换方向时朝目标的概率
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

    // 保存 Inspector 中原始参数,作为难度缩放的基准
    private float baseMoveSpeed;
    private float baseFireCooldown;
    private float baseChaseProbability;
    private float baseOpportunisticRadius;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // 让 AI 一出生就开始动,而不是等 4 秒
        nextChangeTime = Time.time + Random.Range(0.3f, 1.0f);
        // 略微错开开火时机,避免多只敌人同时开火
        fireTimer = Random.Range(0f, fireCooldown);
        // 决定本 enemy 一生扮演的角色(不会再变)
        AssignRole();
        // 把 Inspector 默认值记录下来,后续用作难度缩放基准
        baseMoveSpeed = moveSpeed;
        baseFireCooldown = fireCooldown;
        baseChaseProbability = chaseProbability;
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
            // 失去了(被子弹击中或距离过远),清掉,回到冲 Heart
            if (!opportunisticKill.gameObject.activeInHierarchy ||
                Vector3.Distance(transform.position, opportunisticKill.position) > opportunisticRadius * 1.5f)
            {
                opportunisticKill = null;
            }
            return;
        }
        // 当前没有临时目标 → 在周围扫描一个玩家
        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Tank");
        float bestDist = opportunisticRadius * opportunisticRadius; // 用平方比较
        Transform best = null;
        foreach (GameObject t in tanks)
        {
            if (t == null || !t.activeInHierarchy) continue;
            if (t.GetComponent<Player>() == null) continue;
            float d = (t.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = t.transform;
            }
        }
        opportunisticKill = best;
    }

    private void FixedUpdate()
    {
        MoveMethod();
    }

    private void AttackMethod()
    {
        Instantiate(bulletPrefab, transform.position, Quaternion.Euler(transform.eulerAngles + bullectEulerAngles));
    }

    // 按角色分配决定追逐目标
    private void Retarget()
    {
        switch (role)
        {
            case EnemyRole.HeartChaser:
                currentTarget = FindHeart();
                break;
            case EnemyRole.PlayerChaser:
                currentTarget = FindClosestPlayer();
                break;
            case EnemyRole.RandomWalker:
                currentTarget = null;
                break;
        }
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

    // 主移动:到时刻就重新选方向,根据检测到的障碍立刻转向 / 打碎
    private void MoveMethod()
    {
        BlockType front = DetectFront();

        if (Time.time >= nextChangeTime)
        {
            ChooseNewDirection();
            nextChangeTime = Time.time + Random.Range(changeDirMin, changeDirMax);
        }
        else
        {
            // 不同物体不同反应:
            switch (front)
            {
                case BlockType.BreakableWall:
                    // 可碎墙:立刻开炮打穿(不动方向,等下一发子弹把墙打掉就能直走)
                    if (fireTimer >= fireCooldown * 0.6f) AttackMethod();
                    break;
                case BlockType.SteelWall:
                case BlockType.Heart:
                case BlockType.EnemyTeammate:
                case BlockType.River:
                    // 不可碎 / 不能直接接触:换向绕路
                    ChooseNewDirection();
                    nextChangeTime = Time.time + Random.Range(changeDirMin * 0.5f, changeDirMax);
                    break;
                case BlockType.Grass:
                    // 装饰草:直接穿过即可
                    break;
                case BlockType.None:
                case BlockType.Other:
                default:
                    // 开放空间 / 识别不到:不动
                    break;
            }
        }

        // 真正移动
        if (h != 0)
            transform.Translate(Vector3.right * h * moveSpeed * Time.fixedDeltaTime, Space.World);
        if (v != 0)
            transform.Translate(Vector3.up * v * moveSpeed * Time.fixedDeltaTime, Space.World);
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

    // 选择新方向:大概率朝目标,小概率随机
    private void ChooseNewDirection()
    {
        bool chase = currentTarget != null && Random.value < chaseProbability;
        if (chase)
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
        }
        else
        {
            // 4 方向完全随机
            int num = Random.Range(0, 4);
            if (num == 0) { v = 1; h = 0; }
            else if (num == 1) { v = -1; h = 0; }
            else if (num == 2) { v = 0; h = 1; }
            else { v = 0; h = -1; }
        }
        ApplySprite();
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
        Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.gameObject.CompareTag("Enemy"))
        {
            ChooseNewDirection();
            nextChangeTime = Time.time + Random.Range(0.3f, 1.0f);
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
        // 多属性同步调整
        // 移动速度:线性放大(注意保留 1P/2P 玩家速度不变;只影响 enemy)
        // 攻击间隔:变小(fireCooldown / mult,但不低于 0.5s 防止无敌)
        // 追踪概率:加分母式,趋向 0.95
        // 机会半径:加大
        moveSpeed = baseMoveSpeed * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(1f, 2.4f, mult));
        fireCooldown = Mathf.Max(0.5f, baseFireCooldown / Mathf.Lerp(1f, 1.5f, Mathf.InverseLerp(1f, 2.4f, mult)));
        chaseProbability = Mathf.Clamp(baseChaseProbability + (mult - 1f) * 0.08f, 0.5f, 0.95f);
        opportunisticRadius = baseOpportunisticRadius * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(1f, 2.4f, mult));
    }
}
