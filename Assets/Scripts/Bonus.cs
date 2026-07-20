using UnityEngine;

// 道具类型(顺序对应 Bonus.bmp 切片 Bonus_0..5,可按你的实际图序在 BonusSpawner 里调整)
public enum BonusType
{
    ExtraLife = 0,  // 坦克:+1 命
    Star = 1,       // 星星:玩家火力升级(射速/弹速↑,满级击穿钢墙)
    Grenade = 2,    // 手雷:摧毁场上所有敌人
    Helmet = 3,     // 头盔:一段时间无敌
    Shovel = 4,     // 铁锹:基地围墙临时变钢墙
    Clock = 5,      // 时钟:冻结所有敌人一段时间
}

// 道具本体:玩家坦克(tag = Tank)靠近即拾取,派发对应效果,播放音效并销毁。
// 拾取用「邻近距离检测」而非物理触发,避免因玩家/道具缺少 Rigidbody2D 或没勾 IsTrigger 而捡不到。
public class Bonus : MonoBehaviour
{
    public BonusType type = BonusType.Star;

    public AudioClip pickupAudio;      // GetBonus.wav
    public float lifeTime = 15f;       // 未被拾取时自动消失的时间
    public float blinkInterval = 0.25f;// 闪烁频率(视觉提示)
    public float pickupRadius = 0.7f;  // 玩家中心进入此半径即拾取(世界单位≈格)

    // 各类效果时长
    public float helmetSeconds = 8f;   // 头盔无敌
    public float clockSeconds = 8f;    // 时钟冻结
    public float shovelSeconds = 15f;  // 铁锹护罩

    private SpriteRenderer sr;
    private float blinkTimer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 简单闪烁,提醒玩家有道具
        if (sr != null)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                sr.enabled = !sr.enabled;
            }
        }

        // 邻近拾取:玩家坦克中心进入 pickupRadius 即触发(不依赖物理触发/刚体)
        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Tank");
        float r2 = pickupRadius * pickupRadius;
        for (int i = 0; i < tanks.Length; i++)
        {
            if (tanks[i] == null || !tanks[i].activeInHierarchy) continue;
            Vector2 d = (Vector2)(tanks[i].transform.position - transform.position);
            if (d.sqrMagnitude <= r2)
            {
                Pickup(tanks[i].GetComponent<Player>());
                return;
            }
        }
    }

    // 由 BonusSpawner 生成时调用,设定类型与图标
    public void Configure(BonusType t, Sprite icon)
    {
        type = t;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;
    }

    private void Pickup(Player picker)
    {
        Apply(picker);
        if (pickupAudio != null)
            AudioSource.PlayClipAtPoint(pickupAudio, transform.position);
        Destroy(gameObject);
    }

    private void Apply(Player picker)
    {
        PlayerManager pm = PlayerManager.Instance;

        switch (type)
        {
            case BonusType.ExtraLife:
                if (pm != null) pm.AddLife(1);
                break;

            case BonusType.Star:
                if (picker != null) picker.Upgrade();
                break;

            case BonusType.Grenade:
                Enemy.KillAll();
                break;

            case BonusType.Helmet:
                if (picker != null) picker.AddShield(helmetSeconds);
                break;

            case BonusType.Shovel:
                if (BaseGuard.Instance != null) BaseGuard.Instance.Protect(shovelSeconds);
                break;

            case BonusType.Clock:
                Enemy.Freeze(clockSeconds);
                break;
        }

        if (pm != null) pm.AddBonusScore();   // 拾取加分(仿原版)
    }
}
