using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Option : MonoBehaviour
{
    private int choice = 1;
    public Transform posOne;   // 单人游戏选项位置
    public Transform posTwo;   // 双人游戏选项位置

    void Start()
    {

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            choice = 1;
            transform.position = posOne.position;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            choice = 2;
            transform.position = posTwo.position;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 根据当前选择把模式写入 MenuOptions(供战斗场景 MapCreation 读取)
            MenuOptions.isTwoPlayerMode = (choice == 2);
            MenuOptions.menuChoiceMade = true;
            SceneManager.LoadScene(1);
        }
    }
}
