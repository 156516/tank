using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    // ——玩家 1——
    public int lifeValue1 = 3;
    public int playerScore1 = 0;
    public bool isDead1;

    // ——玩家 2——
    public int lifeValue2 = 3;
    public int playerScore2 = 0;
    public bool isDead2;

    // 游戏是否失败(基地被毁 / 双方都没命)
    public bool isDefeat;

    // 玩家出生 Prefab(Born 类型;在场景中由 Inspector 配置)
    public GameObject player1Born;
    public GameObject player2Born;

    // 玩家 2 的实际预制体(Player 2.prefab),用于重生时把它注入到 Born 实例上
    public GameObject player2Prefab;

    // UI 引用
    public Text playerScoreText1;
    public Text playerScoreText2;
    public Text playerLifeValueText1;
    public Text playerLifeValueText2;
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
        // 双人模式下,如果 Inspector 没拖 player2Prefab,就自动从 MapCreation 同步一次,
        // 避免重生时 Born 因 player2Prefab 为 null 而错误地孵化成 Player 1
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

        if (isDead1) Recover(1);
        if (isDead2) Recover(2);

        if (playerScoreText1 != null) playerScoreText1.text = playerScore1.ToString();
        if (playerScoreText2 != null) playerScoreText2.text = playerScore2.ToString();
        if (playerLifeValueText1 != null) playerLifeValueText1.text = lifeValue1.ToString();
        if (playerLifeValueText2 != null) playerLifeValueText2.text = lifeValue2.ToString();
    }

    // 玩家死亡时由 Player.DieMethod 调用
    public void OnPlayerDie(int playerNumber)
    {
        if (playerNumber == 1) isDead1 = true;
        else if (playerNumber == 2) isDead2 = true;
    }

    // 击杀得分
    public void AddScore(int playerNumber)
    {
        if (playerNumber == 1) playerScore1++;
        else if (playerNumber == 2) playerScore2++;
    }

    // 基地被毁 / 其他原因直接判负
    public void TriggerDefeat()
    {
        isDefeat = true;
    }

    // 处理某个玩家的重生 / 生命耗尽
    private void Recover(int playerNumber)
    {
        if (playerNumber == 1)
        {
            if (lifeValue1 < 0)
            {
                isDead1 = false;
                // 若玩家 2 还在游戏则不立即结束
                if (!isDead2 && lifeValue2 >= 0)
                {
                    return;
                }
                isDefeat = true;
                Invoke("ReturnToTheMainMenu", 3);
                return;
            }
            lifeValue1--;
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
            isDead1 = false;
        }
        else if (playerNumber == 2)
        {
            if (lifeValue2 < 0)
            {
                isDead2 = false;
                if (!isDead1 && lifeValue1 >= 0)
                {
                    return;
                }
                isDefeat = true;
                Invoke("ReturnToTheMainMenu", 3);
                return;
            }
            lifeValue2--;
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
            isDead2 = false;
        }
    }

    private void ReturnToTheMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}
