using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Born : MonoBehaviour
{
    public GameObject playerPrefab;     // 玩家 1 预制体
    public GameObject player2Prefab;    // 玩家 2 预制体(双人模式用)

    public GameObject[] enemyPrefablist;

    public bool createPlayer;            // 由 MapCreation / PlayerManager 决定

    // 1 = Player 1, 2 = Player 2(由调用方在 Instantiate 后设置)
    public int playerNumber = 1;

    void Start()
    {
        Invoke("BornTank", 1f);
        Destroy(gameObject, 1f);
    }

    void Update()
    {

    }

    private void BornTank()
    {
        if (!createPlayer)
        {
            int index = Random.Range(0, enemyPrefablist.Length);
            Instantiate(enemyPrefablist[index], transform.position, Quaternion.identity);
            return;
        }
        GameObject prefabToUse = playerPrefab;
        if (playerNumber == 2 && player2Prefab != null)
        {
            prefabToUse = player2Prefab;
        }
        if (prefabToUse != null)
        {
            GameObject go = Instantiate(prefabToUse, transform.position, Quaternion.identity);
            Player p = go.GetComponent<Player>();
            if (p != null)
            {
                p.playerNumber = playerNumber;
                ApplyKeyMap(p);
            }
        }
    }

    // 根据玩家编号注入对应键位(WASD+Space 或 方向键+Enter)
    private void ApplyKeyMap(Player p)
    {
        if (playerNumber == 2)
        {
            p.moveKeys = new KeyCode[4] {
                KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
            };
            p.fireKey = KeyCode.Return; // Enter / Return
        }
        else
        {
            p.moveKeys = new KeyCode[4] {
                KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D
            };
            p.fireKey = KeyCode.Space;
        }
    }
}
