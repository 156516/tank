using UnityEngine;

// 用于在场景间共享菜单选项(单/双人)
// 静态类,不需要挂载到 GameObject 上
public static class MenuOptions
{
    // 默认双人,方便从编辑器直接 Play 时立刻看到双人模式
    public static bool isTwoPlayerMode = true;

    // 玩家是否在菜单中做过选择(true 时 MapCreation 会用它覆盖 Inspector 默认值)
    public static bool menuChoiceMade = false;
}
