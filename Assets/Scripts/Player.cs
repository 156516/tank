using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 一个升级等级对应的整套坦克贴图(4 方向静态帧 + 4 方向移动帧)
[System.Serializable]
public class TankLevelSprite
{
    public Sprite[] tankSprite;        // 上、右、下、左 静态帧
    public Sprite[] tankSpriteMoving;  // 上、右、下、左 移动帧
}

public class Player : MonoBehaviour
{
    // 玩家编号:1 = Player 1,2 = Player 2
    public int playerNumber = 1;

    // 不同玩家的移动键位(index 顺序固定为 [上, 下, 左, 右])
    // Player 1: WASD + Space
    // Player 2: ↑↓←→ + Enter
    public KeyCode[] moveKeys = new KeyCode[4] {
        KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D
    };
    public KeyCode fireKey = KeyCode.Space;

    // 移动速度
    public float moveSpeed = 3;
    private Vector3 bullectEulerAngles;
    private float timeVal;
    private float defendTimeVal = 3;
    private bool isDefended = true;

    // ---- 道具:星星升级状态 ----
    public int maxPowerLevel = 3;        // 升级上限
    public float attackCooldown = 0.4f;  // 攻击间隔(升级后变短)
    private int powerLevel = 0;          // 0 = 基础,每拾一颗星 +1

    // 贴图 / 预制体 / 音效
    private SpriteRenderer sr;
    public Sprite[] tankSprite;        // 上、下、左、右
    // 移动动画第 2 帧(履带):与 tankSprite 同索引同顺序;留空则不做动画(保持静态)
    public Sprite[] tankSpriteMoving;
    public float treadAnimInterval = 0.1f;   // 履带换帧间隔(秒),越小滚得越快
    private int treadFrame;                   // 0 = 静态帧,1 = 移动帧
    private float treadTimer;
    // 星星升级时按等级切换整套坦克贴图(index = powerLevel;留空则外观不变,只升属性)
    public TankLevelSprite[] levelSprites;
    private int lastDir = 0;                  // 最近朝向(0上/1右/2下/3左),升级后按此立刻刷新贴图
    public GameObject bulletPrefab;
    public GameObject explosionPrefab;
    public GameObject defendEffectPrefab;
    public AudioSource moveAudio;
    public AudioClip[] tankAudio;

    // 处理同时按两方向时优先采用最新输入
    private float lastHorizontalTime;
    private float lastVerticalTime;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.isDefeat)
        {
            return;
        }
        MoveUpdata();
        // 无敌帧
        if (isDefended)
        {
            defendEffectPrefab.SetActive(true);
            defendTimeVal -= Time.deltaTime;
            if (defendTimeVal <= 0)
            {
                isDefended = false;
                defendEffectPrefab.SetActive(false);
            }
        }
        // 攻击 CD
        if (timeVal < attackCooldown)
        {
            timeVal += Time.deltaTime;
        }
        else
        {
            AttackMethod();
        }
    }

    private void FixedUpdate()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.isDefeat)
        {
            return;
        }
        MoveMethod();
    }

    // 攻击:发射子弹
    private void AttackMethod()
    {
        if (Input.GetKeyDown(fireKey))
        {
            GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.Euler(transform.eulerAngles + bullectEulerAngles));
            // 显式标记这是玩家子弹 + 是哪个玩家发射的
            // 这样不依赖 PlayerBullet.prefab 的 Inspector 设置,确保两位玩家都正常
            if (bulletObj != null)
            {
                Bullet b = bulletObj.GetComponent<Bullet>();
                if (b != null)
                {
                    b.isPlayerBullet = true;
                    b.shootingPlayerNumber = playerNumber;
                    // 星星升级:弹速随等级加快,满级可击穿钢墙
                    b.moveSpeed *= 1f + 0.2f * powerLevel;
                    b.canBreakSteel = powerLevel >= maxPowerLevel;
                }
            }
            timeVal = 0;
        }
    }

    // 星星道具:提升火力等级(射速更快、弹速更快,满级击穿钢墙),并按等级切换坦克贴图
    public void Upgrade()
    {
        if (powerLevel < maxPowerLevel) powerLevel++;
        attackCooldown = Mathf.Max(0.15f, 0.4f - 0.08f * powerLevel);
        ApplyLevelSprite();
    }

    // 按当前 powerLevel 换上对应的整套贴图,并立刻刷新当前朝向(未配置则外观不变)
    private void ApplyLevelSprite()
    {
        if (levelSprites == null || levelSprites.Length == 0) return;
        int idx = Mathf.Clamp(powerLevel, 0, levelSprites.Length - 1);
        TankLevelSprite ls = levelSprites[idx];
        if (ls == null) return;
        if (ls.tankSprite != null && ls.tankSprite.Length >= 4) tankSprite = ls.tankSprite;
        if (ls.tankSpriteMoving != null && ls.tankSpriteMoving.Length >= 4) tankSpriteMoving = ls.tankSpriteMoving;
        if (sr != null) sr.sprite = TreadSprite(lastDir);   // 立刻换脸,不必等下次移动
    }

    // 头盔道具:获得(或续期)一段无敌保护,复用出生无敌机制
    public void AddShield(float seconds)
    {
        isDefended = true;
        defendTimeVal = seconds;
        if (defendEffectPrefab != null) defendEffectPrefab.SetActive(true);
    }

    // 记录方向键的最后按下时刻
    private void MoveUpdata()
    {
        if (Input.GetKeyDown(moveKeys[2]) || Input.GetKeyDown(moveKeys[3]))
        {
            lastHorizontalTime = Time.time;
        }
        if (Input.GetKeyDown(moveKeys[0]) || Input.GetKeyDown(moveKeys[1]))
        {
            lastVerticalTime = Time.time;
        }
    }

    // 实际移动
    private void MoveMethod()
    {
        float h = 0f;
        float v = 0f;
        if (Input.GetKey(moveKeys[2])) h -= 1f;   // 左
        if (Input.GetKey(moveKeys[3])) h += 1f;   // 右
        if (Input.GetKey(moveKeys[1])) v -= 1f;   // 下
        if (Input.GetKey(moveKeys[0])) v += 1f;   // 上

        if ((Mathf.Abs(v) > 0.05f) || (Mathf.Abs(h) > 0.05f))
        {
            moveAudio.clip = tankAudio[1];
            if (!moveAudio.isPlaying)
            {
                moveAudio.Play();
            }
        }
        else
        {
            moveAudio.clip = tankAudio[0];
            if (!moveAudio.isPlaying)
            {
                moveAudio.Play();
            }
        }

        // 同时按下水平 + 垂直时,采用最新按下的方向
        if (h != 0 && v != 0)
        {
            if (lastHorizontalTime > lastVerticalTime)
            {
                v = 0;
            }
            else
            {
                h = 0;
            }
        }

        // 履带动画:移动时按间隔交替第 2 帧(未配置移动帧则保持静态)
        if (h != 0 || v != 0)
        {
            treadTimer += Time.fixedDeltaTime;
            if (treadTimer >= treadAnimInterval) { treadTimer = 0f; treadFrame ^= 1; }
        }
        else
        {
            treadTimer = 0f;
            treadFrame = 0;
        }

        if (h != 0)
        {
            transform.Translate(Vector3.right * h * moveSpeed * Time.fixedDeltaTime, Space.World);
            if (h < 0)
            {
                bullectEulerAngles = new Vector3(0, 0, 90);
                lastDir = 3;
                sr.sprite = TreadSprite(3);        // 左
            }
            else if (h > 0)
            {
                bullectEulerAngles = new Vector3(0, 0, -90);
                lastDir = 1;
                sr.sprite = TreadSprite(1);        // 右
            }
        }
        if (v != 0)
        {
            transform.Translate(Vector3.up * v * moveSpeed * Time.fixedDeltaTime, Space.World);
            if (v < 0)
            {
                bullectEulerAngles = new Vector3(0, 0, -180);
                lastDir = 2;
                sr.sprite = TreadSprite(2);        // 下
            }
            else if (v > 0)
            {
                bullectEulerAngles = new Vector3(0, 0, 0);
                lastDir = 0;
                sr.sprite = TreadSprite(0);        // 上
            }
        }
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

    // 死亡:由 Bullet 在命中玩家时调用
    private void DieMethod()
    {
        if (isDefended)
        {
            return;
        }
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerDie(playerNumber);
        }
        Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }
}
