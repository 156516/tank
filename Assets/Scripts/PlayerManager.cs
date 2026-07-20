using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    // 共享生命池:任意一个玩家死亡都消耗 1,两位玩家都用这一条血。
    // 实际初始值在 Start() 里按单/双人模式设定(见 singlePlayerLife / twoPlayerLife)。
    public int lifeValue = 6;

    // 单人 / 双人模式各自的初始生命(单人更少,难度更低)
    public int singlePlayerLife = 3;
    public int twoPlayerLife = 6;

    // 总分:任意一位玩家击杀敌人都累加到这里,两位玩家共用一个分数
    public int score = 0;

    // 是否处于「刚死 / 待重生」状态
    public bool isDead;
    // 最近一次死亡的玩家编号(用于在 Recover 时决定重生成哪一个)
    private int lastDeadPlayer = 1;

    // 游戏失败(基地被毁 / 生命耗尽)
    public bool isDefeat;

    // ——难度递增——
    // 当前关卡(随时间自动升级,影响 Enemy 的速度 / 攻击间隔 / 追踪概率)
    public int currentLevel = 1;
    public float upgradeInterval = 30f;   // 每隔多少秒升一级
    private float nextUpgradeTime = 30f;
    public float gameTime;                // 已玩游戏时间(失败时停止累计)

    // 每级带来的属性倍率(0.10 ≈ 升一级 +10%)
    public float difficultyStep = 0.10f;
    // 难度上限倍率,防止后期 AI 太快
    public float maxDifficultyMultiplier = 2.4f;

    // 玩家出生 Prefab(Born.prefab;player2Born 复用同一份即可)
    public GameObject player1Born;
    public GameObject player2Born;

    // 玩家 2 的实际预制体(Player 2.prefab),Born 会用它在重生时实例化
    public GameObject player2Prefab;

    // UI 引用 —— 血条 / 总分都各 1 个,两位玩家共用
    public Text playerScoreText;
    public Text playerLifeValueText;
    public GameObject isDefeatUI;

    // 单例
    private static PlayerManager instance;
    public static PlayerManager Instance
    {
        get { return instance; }
        set { instance = value; }
    }

    private void Awake()
    {
        instance = this;
        SyncTwoPlayerRefs();
    }

    private void Start()
    {
        // 在所有 Awake 之后读取最终模式(MapCreation.twoPlayerMode 此时已确定),按模式设初始生命
        lifeValue = ResolveTwoPlayerMode() ? twoPlayerLife : singlePlayerLife;
        Enemy.ResetFreeze();   // 复位上一局残留的敌人冻结状态(时钟道具)
    }

    // 解析当前是否双人模式:优先取 MapCreation 的最终值,退而取 MenuOptions
    private bool ResolveTwoPlayerMode()
    {
        MapCreation mc = FindObjectOfType<MapCreation>();
        if (mc != null) return mc.twoPlayerMode;
        return MenuOptions.isTwoPlayerMode;
    }

    private void SyncTwoPlayerRefs()
    {
        if (player2Prefab != null) return;
        MapCreation mc = FindObjectOfType<MapCreation>();
        if (mc != null)
        {
            player2Prefab = mc.player2Prefab;
        }
    }

    void Update()
    {
        if (isDefeat)
        {
            if (isDefeatUI != null) isDefeatUI.SetActive(true);
            Invoke("ReturnToTheMainMenu", 3);
            return;
        }

        // 累计游戏时间 + 周期性升等级
        gameTime += Time.deltaTime;
        if (gameTime >= nextUpgradeTime)
        {
            currentLevel++;
            nextUpgradeTime = gameTime + upgradeInterval;
        }

        if (isDead) Recover();

        if (playerScoreText != null) playerScoreText.text = score.ToString();
        if (playerLifeValueText != null) playerLifeValueText.text = lifeValue.ToString();
    }

    // 供 Enemy 查询:基于当前关卡的难度倍率,范围 [1.0, maxDifficultyMultiplier]
    public float GetDifficultyMultiplier()
    {
        float mult = 1f + (currentLevel - 1) * difficultyStep;
        return Mathf.Min(maxDifficultyMultiplier, mult);
    }

    // 玩家死亡时由 Player.DieMethod 调用,记录是哪位玩家死
    public void OnPlayerDie(int playerNumber)
    {
        lastDeadPlayer = (playerNumber == 2) ? 2 : 1;
        isDead = true;
    }

    // 击杀得分:任何一位玩家击杀敌人都 +1,playerNumber 参数保留以兼容 Enemy.cs 的调用
    public void AddScore(int playerNumber)
    {
        AddScore(playerNumber, 1);
    }

    // 带分值的重载:不同类型敌人击杀分值不同(基础1/快速2/装甲4 等)
    public void AddScore(int playerNumber, int amount)
    {
        score += amount;
    }

    // 坦克道具:增加共享生命
    public void AddLife(int n)
    {
        lifeValue += n;
    }

    // 拾取道具的固定加分(仿原版每个道具 500 分)
    public int bonusScore = 500;
    public void AddBonusScore()
    {
        score += bonusScore;
    }

    // 基地被毁 / 其他原因直接判负
    public void TriggerDefeat()
    {
        isDefeat = true;
    }

    // 处理共享生命池的重生 / 失败判定
    private void Recover()
    {
        if (lifeValue <= 0)
        {
            // 共享生命已耗尽,失败
            isDead = false;
            isDefeat = true;
            Invoke("ReturnToTheMainMenu", 3);
            return;
        }

        lifeValue--;

        // 重生上一次死亡的玩家
        if (lastDeadPlayer == 1)
        {
            if (player1Born != null)
            {
                GameObject go = Instantiate(player1Born, new Vector3(-2, -8, 0), Quaternion.identity);
                Born b = go.GetComponent<Born>();
                if (b != null)
                {
                    b.createPlayer = true;
                    b.playerNumber = 1;
                }
            }
        }
        else // lastDeadPlayer == 2
        {
            if (player2Born != null)
            {
                GameObject go = Instantiate(player2Born, new Vector3(2, -8, 0), Quaternion.identity);
                Born b = go.GetComponent<Born>();
                if (b != null)
                {
                    b.createPlayer = true;
                    b.playerNumber = 2;
                    b.player2Prefab = player2Prefab;
                }
            }
        }

        isDead = false;
    }

    private void ReturnToTheMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}
