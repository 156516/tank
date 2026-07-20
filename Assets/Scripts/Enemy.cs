using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 简化的敌人:自动开火,只朝 Heart 走。
public class Enemy : MonoBehaviour
{
    public float moveSpeed = 3;
    public float fireCooldown = 1.5f;

    // 贴图 / 预制体
    private SpriteRenderer sr;
    public Sprite[] tankSprite;          // 上、下、左、右
    public GameObject bulletPrefab;
    public GameObject explosionPrefab;

    // 击杀本敌人的玩家编号(Bullet 在击中时写入)
    public int killerPlayerNumber = 1;

    // 移动方向(强制 axis-aligned)
    private Vector3 bullectEulerAngles;
    private float h;
    private float v = -1;

    // 开火冷却
    private float fireTimer;

    // 当前要冲的目标(由 Awake / Update 锁定 Heart)
    private Transform target;

    // 上次碰到墙后多久(用于撞墙 90 度转)
    private float collideCooldown;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // 起始 axis-aligned 朝下,不斜走
        h = 0f;
        v = -1f;
        fireTimer = Random.Range(0f, fireCooldown);
        collideCooldown = 0f;
        AcquireTarget();
    }

    void Update()
    {
        // 自动开火:timer 到点就开,不看前方是什么
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireCooldown)
        {
            Fire();
            fireTimer = 0f;
        }

        // Heart 可能被毁或动态生成;每 0.5s 重新锁定
        if (target == null || (target.gameObject != null && !target.gameObject.activeInHierarchy))
        {
            AcquireTarget();
        }
    }

    // 找 Heart 作为唯一目标
    private void AcquireTarget()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Heart");
        if (go != null && go.activeInHierarchy)
        {
            target = go.transform;
        }
        else
        {
            target = null;
        }
    }

    private void Fire()
    {
        Instantiate(bulletPrefab, transform.position,
            Quaternion.Euler(transform.eulerAngles + bullectEulerAngles));
    }

    void FixedUpdate()
    {
        Move();
    }

    // 朝 Heart 走;撞铁墙 / 河就 90 度随机转
    private void Move()
    {
        if (target == null)
        {
            // 没有目标:随机走
            ApplyRandomDirection();
            return;
        }

        // 朝向 Heart:选主轴方向(axis-aligned)
        Vector3 diff = target.position - transform.position;
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

        // 撞到铁墙 / 边界空气墙 / 河(撞前检测,不被嵌入):90 度随机转
        // 短冷却避免同帧反复触发
        if (collideCooldown <= 0f)
        {
            Vector3 dir = new Vector3(h, v, 0);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, 0.55f);
            if (hit.collider != null && IsBlocking(hit.collider))
            {
                // 旋转 90 度(若水平则改为垂直随机方向,反之亦然)
                if (h != 0) { v = Random.value > 0.5f ? 1 : -1; h = 0; }
                else        { h = Random.value > 0.5f ? 1 : -1; v = 0; }
                collideCooldown = 0.15f;
            }
        }
        else
        {
            collideCooldown -= Time.fixedDeltaTime;
        }

        ApplySprite();

        // axis-aligned 防御
        if (h != 0 && v != 0) v = 0;
        if (h != 0) transform.Translate(Vector3.right * h * moveSpeed * Time.fixedDeltaTime, Space.World);
        else if (v != 0) transform.Translate(Vector3.up * v * moveSpeed * Time.fixedDeltaTime, Space.World);
    }

    private void ApplyRandomDirection()
    {
        int n = Random.Range(0, 4);
        if (n == 0) { v = 1f; h = 0f; }
        else if (n == 1) { v = -1f; h = 0f; }
        else if (n == 2) { v = 0f; h = 1f; }
        else { v = 0f; h = -1f; }
        ApplySprite();
    }

    private bool IsBlocking(Collider2D col)
    {
        string tag = col.tag;
        if (tag == "Barrier" || tag == "Heart") return true;
        if (col.gameObject.name == "River") return true;
        return false;
    }

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
}
