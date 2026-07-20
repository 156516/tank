using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MapCreation : MonoBehaviour
{
    // 物品数组,初始化地图所有的元素
    // 0.玩家基地 1.墙 2.障碍物 3.出生效果 4.河流/道具 5.草 6.边界墙
    public GameObject[] item;

    // Player 2 出生点模板(Born Prefab),在 Inspector 中拖入 Born 2.prefab
    public GameObject player2BornPrefab;

    // true = 双人模式(同时生成 player1 / player2 出生点);false = 单人
    public bool twoPlayerMode = true;

    // 已经拥有东西的位置列表
    private List<Vector3> itemPositionList = new List<Vector3>();

    private void Awake()
    {
        InitMap();
    }

    private void InitMap()
    {
        // 实例化基地
        CreateItem(item[0], new Vector3(0, -8, 0), Quaternion.identity);
        // 用墙把基地围起来
        CreateItem(item[1], new Vector3(-1, -8, 0), Quaternion.identity);
        CreateItem(item[1], new Vector3(1, -8, 0), Quaternion.identity);
        for (int i = -1; i < 2; i++)
        {
            CreateItem(item[1], new Vector3(i, -7, 0), Quaternion.identity);
        }
        // 实例化边界墙
        for (int i = -11; i < 12; i++)
        {
            CreateItem(item[6], new Vector3(i, -9, 0), Quaternion.identity);
            CreateItem(item[6], new Vector3(i, 9, 0), Quaternion.identity);
        }
        for (int i = -9; i < 9; i++)
        {
            CreateItem(item[6], new Vector3(-11, i, 0), Quaternion.identity);
            CreateItem(item[6], new Vector3(11, i, 0), Quaternion.identity);
        }
        // 初始化玩家 1
        GameObject go = Instantiate(item[3], new Vector3(-2, -8, 0), Quaternion.identity);
        Born player1Born = go.GetComponent<Born>();
        player1Born.createPlayer = true;
        player1Born.playerNumber = 1;
        // 初始化玩家 2(双人模式)
        if (twoPlayerMode && player2BornPrefab != null)
        {
            GameObject go2 = Instantiate(player2BornPrefab, new Vector3(2, -8, 0), Quaternion.identity);
            Born player2Born = go2.GetComponent<Born>();
            if (player2Born != null)
            {
                player2Born.createPlayer = true;
                player2Born.playerNumber = 2;
            }
        }

        // 敌人出生点
        CreateItem(item[3], new Vector3(-10, 8, 0), Quaternion.identity);
        CreateItem(item[3], new Vector3(0, 8, 0), Quaternion.identity);
        CreateItem(item[3], new Vector3(10, 8, 0), Quaternion.identity);

        InvokeRepeating("CreateEnemy", 4, 5);

        // 实例化地图
        for (int i = 0; i < 60; i++)
        {
            CreateItem(item[1], CreateRandomPosition(), Quaternion.identity);
        }
        for (int i = 0; i < 20; i++)
        {
            CreateItem(item[2], CreateRandomPosition(), Quaternion.identity);
        }
        for (int i = 0; i < 20; i++)
        {
            CreateItem(item[4], CreateRandomPosition(), Quaternion.identity);
        }
        for (int i = 0; i < 20; i++)
        {
            CreateItem(item[5], CreateRandomPosition(), Quaternion.identity);
        }
    }

    private void CreateItem(GameObject createGameObject, Vector3 createPosition, Quaternion createRotation)
    {
        GameObject itemGo = Instantiate(createGameObject, createPosition, createRotation);
        itemGo.transform.SetParent(gameObject.transform);
        itemPositionList.Add(createPosition);
    }

    private Vector3 CreateRandomPosition()
    {
        // 位置在 x=-10,10 之间,y=-8,8 之间,保证坦克不会在地图边缘卡住
        while (true)
        {
            int x = Random.Range(-9, 10);
            int y = Random.Range(-7, 8);
            Vector3 createPosition = new Vector3(x, y, 0);
            if (!HasThePosition(createPosition))
            {
                return createPosition;
            }
        }
    }

    private bool HasThePosition(Vector3 createPosition)
    {
        for (int i = 0; i < itemPositionList.Count; i++)
        {
            if (createPosition == itemPositionList[i])
            {
                return true;
            }
        }
        return false;
    }

    private void CreateEnemy()
    {
        int num = Random.Range(0, 3);
        Vector3 EnemyPos = new Vector3();
        if (num == 0)
        {
            EnemyPos = new Vector3(-10, 8, 0);
        }
        else if (num == 1)
        {
            EnemyPos = new Vector3(0, 8, 0);
        }
        else if (num == 2)
        {
            EnemyPos = new Vector3(10, 8, 0);
        }
        CreateItem(item[3], EnemyPos, Quaternion.identity);
    }
}
