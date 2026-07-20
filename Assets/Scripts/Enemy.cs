using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float moveSpeed = 3;
    private Vector3 bullectEulerAngles;
    float h;
    float v = -1;

    // 击杀本敌人的玩家编号(由 Bullet 在击中时写入)
    public int killerPlayerNumber = 1;

    // 计时器
    private float timeVal;
    private float timeValChangeDirection = 4;

    // 贴图 / 预制体
    private SpriteRenderer sr;
    public Sprite[] tankSprite; // 上、下、左、右
    public GameObject bulletPrefab;
    public GameObject explosionPrefab;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {

    }

    void Update()
    {
        // 攻击计时器
        if (timeVal < 3f)
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
        MoveMethod();
    }

    // 坦克的攻击方法
    private void AttackMethod()
    {
        Instantiate(bulletPrefab, transform.position, Quaternion.Euler(transform.eulerAngles + bullectEulerAngles));
        timeVal = 0;
    }

    // 坦克的移动方法
    private void MoveMethod()
    {
        // 随机改变移动方向
        if (timeValChangeDirection >= 4)
        {
            int num = Random.Range(0, 8);
            if (num > 5)
            {
                v = -1;
                h = 0;
            }
            else if (num == 0)
            {
                v = 1;
                h = 0;
            }
            else if (num > 0 && num <= 2)
            {
                v = 0;
                h = -1;
            }
            else if (num > 2 && num <= 4)
            {
                v = 0;
                h = 1;
            }
            timeValChangeDirection = 0;
        }
        else
        {
            timeValChangeDirection += Time.fixedDeltaTime;
        }
        // 横向移动
        transform.Translate(Vector3.right * h * moveSpeed * Time.fixedDeltaTime, Space.World);
        if (h < 0)
        {
            sr.sprite = tankSprite[3];
            bullectEulerAngles = new Vector3(0, 0, 90);
        }
        else if (h > 0)
        {
            sr.sprite = tankSprite[1];
            bullectEulerAngles = new Vector3(0, 0, -90);
        }
        // 纵向移动
        transform.Translate(Vector3.up * v * moveSpeed * Time.fixedDeltaTime, Space.World);
        if (v < 0)
        {
            sr.sprite = tankSprite[2];
            bullectEulerAngles = new Vector3(0, 0, -180);
        }
        else if (v > 0)
        {
            sr.sprite = tankSprite[0];
            bullectEulerAngles = new Vector3(0, 0, 0);
        }
    }

    // 坦克的死亡方法:由 Bullet 击中时通过 SendMessage 调用
    private void DieMethod()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.AddScore(killerPlayerNumber);
        }
        Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            timeValChangeDirection = 4;
        }
    }
}
