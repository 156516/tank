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
                    // 用 SendMessage 的目的是调 DieMethod,但 target 可能没挂 Enemy 组件;
                    // 不过 Enemy 必然存在(否则不会 tag = Enemy),所以 SendMessage OK
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
                // 注意:Boundary AirBarrier 的 tag 也是 Barrier,但没挂 Barrier 组件,
                // 用 GetComponent 防御性调用 — 没组件就静默不响
                if (isPlayerBullet)
                {
                    Barrier barrier = collision.GetComponent<Barrier>();
                    if (barrier != null) barrier.PlayAudio();
                }
                Destroy(this.gameObject);
                break;
            case "Wall":
                // 把这堵砖墙从 MapGrid 标记为已碎,通知所有 enemy 立即重算路径
                MapGrid.MarkWallBroken(MapGrid.WorldToCell(collision.transform.position));
                Destroy(collision.gameObject);
                Destroy(this.gameObject);
                break;
            default:
                break;
        }
    }
}
