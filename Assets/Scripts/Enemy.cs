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

    // AI 调参
    public float fireCooldown = 1.5f;
    public float changeDirMin = 1.5f;
    public float changeDirMax = 3.0f;
    public float chaseProbability = 0.78f;  // 改变方向时朝玩家的概率(其余则随机)
    public float detectRange = 1.0f;        // 前方射线长度

    // 贴图 / 预制体
    private SpriteRenderer sr;
    public Sprite[] tankSprite; // 上、下、左、右
    public GameObject bulletPrefab;
    public GameObject explosionPrefab;

    // 当前目标(最近且存活的玩家)
    private Transform targetTank;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // 让 AI 一出生就开始动,而不是等 4 秒
        nextChangeTime = Time.time + Random.Range(0.3f, 1.0f);
        // 略微错开开火时机,避免多只敌人同时开火
        fireTimer = Random.Range(0f, fireCooldown);
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

        // 周期性地重新选目标(场景里可能有玩家死亡重生)
        retargetTimer += Time.deltaTime;
        if (retargetTimer >= 0.5f || targetTank == null)
        {
            Retarget();
            retargetTimer = 0f;
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

    // 优先级:Heart(基地) > 最近玩家。
    // 因为撞掉基地 = 立刻结束游戏,所以敌人会优先尝试打基地;
    // 只有当基地已经被毁,才把火力转向玩家。
    private void Retarget()
    {
        GameObject heart = GameObject.FindGameObjectWithTag("Heart");
        if (heart != null && heart.activeInHierarchy)
        {
            targetTank = heart.transform;
            return;
        }
        targetTank = FindClosestPlayer();
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

    // 主移动:到时刻就重新选方向,根据检测到的障碍立刻转向
    private void MoveMethod()
    {
        if (Time.time >= nextChangeTime)
        {
            ChooseNewDirection();
            nextChangeTime = Time.time + Random.Range(changeDirMin, changeDirMax);
        }
        else if (IsFrontBlocked())
        {
            // 前方撞到东西(墙/障碍/enemy),立即换一个方向
            ChooseNewDirection();
            nextChangeTime = Time.time + Random.Range(changeDirMin * 0.5f, changeDirMax);
        }

        // 真正移动
        if (h != 0)
            transform.Translate(Vector3.right * h * moveSpeed * Time.fixedDeltaTime, Space.World);
        if (v != 0)
            transform.Translate(Vector3.up * v * moveSpeed * Time.fixedDeltaTime, Space.World);
    }

    // 判断前方 1 格内是否被墙 / 障碍 / 队友挡住
    private bool IsFrontBlocked()
    {
        Vector3 dir = new Vector3(h, v, 0);
        if (dir == Vector3.zero) return false;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, detectRange);
        if (hit.collider == null) return false;
        string tag = hit.collider.tag;
        return tag == "Wall" || tag == "Barrier" || tag == "Heart" || tag == "Enemy";
    }

    // 选择新方向:大概率朝目标,小概率随机
    private void ChooseNewDirection()
    {
        bool chase = targetTank != null && Random.value < chaseProbability;
        if (chase)
        {
            Vector3 diff = targetTank.position - transform.position;
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

    // 撞到同伴坦克:不必等下次换向,立刻绕路
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.gameObject.CompareTag("Enemy"))
        {
            ChooseNewDirection();
            nextChangeTime = Time.time + Random.Range(0.3f, 1.0f);
        }
    }
}
