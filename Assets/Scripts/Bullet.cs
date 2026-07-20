using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float moveSpeed = 10;

    // 是否是玩家发射的子弹
    public bool isPlayerBullet;

    // 由哪个玩家发射的(1 = Player 1, 2 = Player 2),用于击杀得分归属
    public int shootingPlayerNumber = 1;

    void Start()
    {

    }

    void Update()
    {
        transform.Translate(transform.up * Time.deltaTime * moveSpeed, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        switch (collision.tag)
        {
            case "Enemy":
                if (isPlayerBullet)
                {
                    // 把击杀者编号写入敌人,击杀时由敌人把它传给 PlayerManager.AddScore
                    Enemy enemy = collision.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.killerPlayerNumber = shootingPlayerNumber;
                    }
                    collision.SendMessage("DieMethod");
                    Destroy(this.gameObject);
                }
                break;
            case "Tank":
                if (!isPlayerBullet)
                {
                    collision.SendMessage("DieMethod");
                    Destroy(this.gameObject);
                }
                break;
            case "Heart":
                collision.SendMessage("Die");
                Destroy(this.gameObject);
                break;
            case "Barrier":
                if (isPlayerBullet)
                {
                    collision.SendMessage("PlayAudio");
                }
                Destroy(this.gameObject);
                break;
            case "Wall":
                Destroy(collision.gameObject);
                Destroy(this.gameObject);
                break;
            default:
                break;
        }
    }
}
