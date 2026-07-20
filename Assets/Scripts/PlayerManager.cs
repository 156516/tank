using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    // 共享生命池:任意一个玩家死亡都消耗 1,两位玩家都用这一条血
    public int lifeValue = 6;

    // 分数两位玩家分开记录
    public int playerScore1 = 0;
    public int playerScore2 = 0;

    // 是否处于「刚死 / 待重生」状态
    public bool isDead;
    // 最近一次死亡的玩家编号(用于在 Recover 时决定重生成哪一个)
    private int lastDeadPlayer = 1;

    // 游戏失败(基地被毁 / 生命耗尽)
    public bool isDefeat;

    // 玩家出生 Prefab(Born.prefab;player2Born 复用同一份即可)
    public GameObject player1Born;
    public GameObject player2Born;

    // 玩家 2 的实际预制体(Player 2.prefab),Born 会用它在重生时实例化
    public GameObject player2Prefab;

    // UI 引用 —— 血条共享为 1 个,分数各 1 个
    public Text playerScoreText1;
    public Text playerScoreText2;
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

        if (isDead) Recover();

        if (playerScoreText1 != null) playerScoreText1.text = playerScore1.ToString();
        if (playerScoreText2 != null) playerScoreText2.text = playerScore2.ToString();
        if (playerLifeValueText != null) playerLifeValueText.text = lifeValue.ToString();
    }

    // 玩家死亡时由 Player.DieMethod 调用,记录是哪位玩家死
    public void OnPlayerDie(int playerNumber)
    {
        lastDeadPlayer = (playerNumber == 2) ? 2 : 1;
        isDead = true;
    }

    // 击杀得分
    public void AddScore(int playerNumber)
    {
        if (playerNumber == 2) playerScore2++;
        else playerScore1++;
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
