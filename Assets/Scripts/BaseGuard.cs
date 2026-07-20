using System.Collections;
using UnityEngine;

// 铁锹道具:把基地(0,-8)周围的砖墙临时换成钢墙,持续 seconds 秒后恢复成砖墙。
// 用法:场景里放一个空物体挂本脚本,Inspector 拖入 steelPrefab(Barrier 钢墙) 与 brickPrefab(Wall 砖墙)。
// 若场景没有 BaseGuard,铁锹道具会静默无效(Bonus 里做了判空)。
public class BaseGuard : MonoBehaviour
{
    public static BaseGuard Instance;

    public GameObject steelPrefab;   // 钢墙预制体(tag = Barrier),对应 MapCreation.item[2]
    public GameObject brickPrefab;   // 砖墙预制体(tag = Wall),对应 MapCreation.item[1]

    // 基地四周的护墙环(与 MapCreation.InitMap 中围基地的墙一致)
    private static readonly Vector3[] ring =
    {
        new Vector3(-1, -8, 0), new Vector3(1, -8, 0),
        new Vector3(-1, -7, 0), new Vector3(0, -7, 0), new Vector3(1, -7, 0),
    };

    private Coroutine running;

    void Awake()
    {
        Instance = this;
    }

    // 铁锹道具触发:钢墙护罩,seconds 秒后恢复砖墙。重复拾取会续期。
    public void Protect(float seconds)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ProtectRoutine(seconds));
    }

    private IEnumerator ProtectRoutine(float seconds)
    {
        BuildRing(steelPrefab);                 // 换成钢墙
        yield return new WaitForSeconds(seconds);
        BuildRing(brickPrefab);                 // 恢复砖墙
        running = null;
    }

    // 清空护墙环各格的旧墙,再铺上指定预制体,最后重扫网格让敌人寻路感知变化
    private void BuildRing(GameObject prefab)
    {
        Physics2D.SyncTransforms();
        for (int i = 0; i < ring.Length; i++)
        {
            ClearCell(ring[i]);
            if (prefab != null) Instantiate(prefab, ring[i], Quaternion.identity);
        }
        MapGrid.Rebuild();
    }

    // 销毁某格上已有的砖墙 / 钢墙(不动基地 Heart 和其它物体)
    private void ClearCell(Vector3 pos)
    {
        Collider2D[] cols = Physics2D.OverlapPointAll(pos);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null) continue;
            string t = cols[i].tag;
            if (t == "Wall" || t == "Barrier")
            {
                Destroy(cols[i].gameObject);
            }
        }
    }
}
