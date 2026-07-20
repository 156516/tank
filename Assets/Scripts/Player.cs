using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // 玩家编号:1 = Player 1,2 = Player 2
    public int playerNumber = 1;

    // 不同玩家的移动键位(index 顺序固定为 [上, 下, 左, 右])
    // Player 1: WASD + Space
    // Player 2: ↑↓←→ + Enter
    public KeyCode[] moveKeys = new KeyCode[4] {
        KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D
    };
    public KeyCode fireKey = KeyCode.Space;

    // 移动速度
    public float moveSpeed = 3;
    private Vector3 bullectEulerAngles;
    private float timeVal;
    private float defendTimeVal = 3;
    private bool isDefended = true;

    // 贴图 / 预制体 / 音效
    private SpriteRenderer sr;
    public Sprite[] tankSprite;        // 上、下、左、右
    public GameObject bulletPrefab;
    public GameObject explosionPrefab;
    public GameObject defendEffectPrefab;
    public AudioSource moveAudio;
    public AudioClip[] tankAudio;

    // 处理同时按两方向时优先采用最新输入
    private float lastHorizontalTime;
    private float lastVerticalTime;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.isDefeat)
        {
            return;
        }
        MoveUpdata();
        // 无敌帧
        if (isDefended)
        {
            defendEffectPrefab.SetActive(true);
            defendTimeVal -= Time.deltaTime;
            if (defendTimeVal <= 0)
            {
                isDefended = false;
                defendEffectPrefab.SetActive(false);
            }
        }
        // 攻击 CD
        if (timeVal < 0.4f)
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
        if (PlayerManager.Instance != null && PlayerManager.Instance.isDefeat)
        {
            return;
        }
        MoveMethod();
    }

    // 攻击:发射子弹
    private void AttackMethod()
    {
        if (Input.GetKeyDown(fireKey))
        {
            Instantiate(bulletPrefab, transform.position, Quaternion.Euler(transform.eulerAngles + bullectEulerAngles));
            timeVal = 0;
        }
    }

    // 记录方向键的最后按下时刻
    private void MoveUpdata()
    {
        if (Input.GetKeyDown(moveKeys[2]) || Input.GetKeyDown(moveKeys[3]))
        {
            lastHorizontalTime = Time.time;
        }
        if (Input.GetKeyDown(moveKeys[0]) || Input.GetKeyDown(moveKeys[1]))
        {
            lastVerticalTime = Time.time;
        }
    }

    // 实际移动
    private void MoveMethod()
    {
        float h = 0f;
        float v = 0f;
        if (Input.GetKey(moveKeys[2])) h -= 1f;   // 左
        if (Input.GetKey(moveKeys[3])) h += 1f;   // 右
        if (Input.GetKey(moveKeys[1])) v -= 1f;   // 下
        if (Input.GetKey(moveKeys[0])) v += 1f;   // 上

        if ((Mathf.Abs(v) > 0.05f) || (Mathf.Abs(h) > 0.05f))
        {
            moveAudio.clip = tankAudio[1];
            if (!moveAudio.isPlaying)
            {
                moveAudio.Play();
            }
        }
        else
        {
            moveAudio.clip = tankAudio[0];
            if (!moveAudio.isPlaying)
            {
                moveAudio.Play();
            }
        }

        // 同时按下水平 + 垂直时,采用最新按下的方向
        if (h != 0 && v != 0)
        {
            if (lastHorizontalTime > lastVerticalTime)
            {
                v = 0;
            }
            else
            {
                h = 0;
            }
        }

        if (h != 0)
        {
            transform.Translate(Vector3.right * h * moveSpeed * Time.fixedDeltaTime, Space.World);
            if (h < 0)
            {
                sr.sprite = tankSprite[3];        // 左
                bullectEulerAngles = new Vector3(0, 0, 90);
            }
            else if (h > 0)
            {
                sr.sprite = tankSprite[1];        // 右
                bullectEulerAngles = new Vector3(0, 0, -90);
            }
        }
        if (v != 0)
        {
            transform.Translate(Vector3.up * v * moveSpeed * Time.fixedDeltaTime, Space.World);
            if (v < 0)
            {
                sr.sprite = tankSprite[2];        // 下
                bullectEulerAngles = new Vector3(0, 0, -180);
            }
            else if (v > 0)
            {
                sr.sprite = tankSprite[0];        // 上
                bullectEulerAngles = new Vector3(0, 0, 0);
            }
        }
    }

    // 死亡:由 Bullet 在命中玩家时调用
    private void DieMethod()
    {
        if (isDefended)
        {
            return;
        }
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerDie(playerNumber);
        }
        Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }
}
