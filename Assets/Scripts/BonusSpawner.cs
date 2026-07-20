using System.Collections;
using UnityEngine;

// 道具生成器:每隔一段随机时间,在地图上一个随机空地生成一个随机道具。
// 用法:场景放一个空物体挂本脚本;bonusPrefab 拖入带 Bonus 脚本的预制体;
//       bonusSprites 拖入 Bonus.bmp 的 6 张切片(顺序对应 BonusType:命/星/雷/盔/锹/钟)。
public class BonusSpawner : MonoBehaviour
{
    public GameObject bonusPrefab;     // 带 Bonus + SpriteRenderer + IsTrigger Collider2D
    public Sprite[] bonusSprites;      // 6 张,索引对应 BonusType 枚举值

    public float minInterval = 12f;    // 两次生成的最短间隔
    public float maxInterval = 20f;    // 最长间隔
    public int maxActive = 1;          // 场上同时存在的道具上限

    // 生成范围(与地图可放置区域一致,避开边界)
    public int minX = -9, maxX = 9, minY = -7, maxY = 7;

    void Start()
    {
        if (bonusPrefab != null)
            StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            if (CountActive() >= maxActive) continue;

            Vector3 pos;
            if (!TryFindEmptyCell(out pos)) continue;

            GameObject go = Instantiate(bonusPrefab, pos, Quaternion.identity);
            Bonus bonus = go.GetComponent<Bonus>();
            if (bonus != null)
            {
                int typeCount = System.Enum.GetValues(typeof(BonusType)).Length;
                int idx = Random.Range(0, typeCount);
                Sprite icon = (bonusSprites != null && idx < bonusSprites.Length) ? bonusSprites[idx] : null;
                bonus.Configure((BonusType)idx, icon);
            }
        }
    }

    private int CountActive()
    {
        return Object.FindObjectsOfType<Bonus>().Length;
    }

    // 随机找一个可走空地(避开墙/河/铁块/基地),最多尝试若干次
    private bool TryFindEmptyCell(out Vector3 pos)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            int x = Random.Range(minX, maxX + 1);
            int y = Random.Range(minY, maxY + 1);
            Vector3 world = new Vector3(x, y, 0);
            if (MapGrid.GetCellType(MapGrid.WorldToCell(world)) == MapGrid.Walkable)
            {
                pos = world;
                return true;
            }
        }
        pos = Vector3.zero;
        return false;
    }
}
