using UnityEngine;

// 敌人难度随时间平滑增强。
// 以 Time.timeSinceLevelLoad(本局开始经过的秒数)为时钟:
//   开局敌人较弱(移动慢、开火慢),在 RampSeconds 内线性增强到「满级」。
//   满级值 = Enemy 预制体 Inspector 里配置的 moveSpeed / fireCooldown。
// 每次重新加载场景(重开一局)时间自动归零,难度随之重置。
public static class EnemyDifficulty
{
    // 从开局到满级所需时间(秒),越大越平缓。可按手感调。
    public const float RampSeconds = 120f;

    // 开局相对满级的弱化系数:
    //   移动速度  = 满级速度 * StartSpeedMul      (<1 → 开局更慢)
    //   开火冷却  = 满级冷却 * StartCooldownMul    (>1 → 开局开火更慢)
    public const float StartSpeedMul = 0.55f;
    public const float StartCooldownMul = 2.5f;

    // 0 → 1 的难度进度(0=开局最弱,1=满级)
    public static float Factor01
    {
        get { return Mathf.Clamp01(Time.timeSinceLevelLoad / RampSeconds); }
    }

    // 由「满级(最快)速度」求当前有效移动速度
    public static float MoveSpeed(float maxSpeed)
    {
        return Mathf.Lerp(maxSpeed * StartSpeedMul, maxSpeed, Factor01);
    }

    // 由「满级(最短)开火冷却」求当前有效冷却
    public static float FireCooldown(float minCooldown)
    {
        return Mathf.Lerp(minCooldown * StartCooldownMul, minCooldown, Factor01);
    }

    // 躲子弹熟练度 0 → 1:开局为 0(完全不躲),随时间提升。
    // Enemy 用它缩放「子弹反应距离」:熟练度越高越早发现来袭子弹、越能躲开。
    public static float DodgeSkill01
    {
        get { return Factor01; }
    }
}
