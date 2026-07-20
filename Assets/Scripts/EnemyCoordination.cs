using System.Collections.Generic;
using UnityEngine;

// 敌人之间的协调:避免多个敌人同时挤在一个目标前围观。
// - 每个 Player 最多被 2 名敌人同时追
// - Heart 最多被 4 名敌人同时冲(鼓励"散开围堵"而不是"一锅端")
// - 锁满后,新敌人被迫换目标(打另一个玩家)或降级为随机走
public static class EnemyCoordination
{
    // key = 目标 GameObject(Heart / Player),value = 当前锁定此目标的 Enemy 列表
    private static Dictionary<GameObject, HashSet<Enemy>> claims =
        new Dictionary<GameObject, HashSet<Enemy>>();

    public static int MaxLockOnHeart = 4;
    public static int MaxLockOnPlayer = 2;

    public static bool TryClaim(GameObject target, Enemy enemy)
    {
        if (target == null || enemy == null) return false;
        if (!claims.TryGetValue(target, out HashSet<Enemy> set))
        {
            set = new HashSet<Enemy>();
            claims[target] = set;
        }
        // 已存在:不再计数
        if (set.Contains(enemy)) return true;
        int max = IsHeart(target) ? MaxLockOnHeart : MaxLockOnPlayer;
        if (set.Count >= max) return false;
        set.Add(enemy);
        return true;
    }

    public static void Release(GameObject target, Enemy enemy)
    {
        if (target == null) return;
        if (claims.TryGetValue(target, out HashSet<Enemy> set))
        {
            set.Remove(enemy);
            if (set.Count == 0) claims.Remove(target);
        }
    }

    public static int GetClaimCount(GameObject target)
    {
        if (target == null) return 0;
        return claims.TryGetValue(target, out HashSet<Enemy> set) ? set.Count : 0;
    }

    private static bool IsHeart(GameObject go)
    {
        return go != null && go.CompareTag("Heart");
    }

    // 场景重载 / 失败时统一清理
    public static void Clear()
    {
        claims.Clear();
    }
}
