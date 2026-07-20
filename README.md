# 🎮 Tank Battle(坦克大战)

一款基于 **Unity 2D** 制作的经典坦克大战复刻游戏。玩家控制己方坦克,在随机生成的地图中击毁不断来袭的敌方坦克,同时保护我方基地(Heart),坚持到最后一刻。

支持 **单人 / 双人本地合作** 两种模式:单人模式使用 `WASD + Space`;双人模式下一位玩家沿用 `WASD + Space`,另一位使用方向键 + `Enter`,**共享同一座基地与同一份总分**。生命与刷怪按模式区分:**单人 3 命、从 2 处刷敌**;**双人共享 6 命、从 3 处刷敌**。敌人采用 **A\* 寻路 + 贴格移动**,会**协作分工**(离基地近的打基地、离玩家近的追玩家),并且**难度随游戏时间平滑增强**(开局较弱,约 2 分钟内逐渐达到满级)。

> 致敬 1985 年 FC/NES 上的经典《Battle City》——本项目以现代 Unity 引擎重新演绎。

---

## 📑 目录

- [游戏简介](#-游戏简介)
- [核心特性](#-核心特性)
- [操作说明](#-操作说明)
- [地图元素](#-地图元素)
- [技术栈](#-技术栈)
- [项目结构](#-项目结构)
- [快速开始](#-快速开始)
- [脚本说明](#-脚本说明)
- [后续可扩展方向](#-后续可扩展方向)
- [致谢](#-致谢)

---

## 🕹️ 游戏简介

- **玩家坦克**:受玩家操控,通过移动、转向、开火击败敌人。
- **敌方坦克**:AI 控制,会随机变换方向并定时发射子弹,具备一定威胁。
- **基地(Heart)**:玩家守护的核心目标,一旦被击中即游戏失败。
- **生命系统**:每位玩家初始拥有 3 条生命,阵亡后在出生点复活并获得短暂无敌保护。
- **随机地图**:每局游戏开始时自动生成地图布局,具有可重玩性。

---

## ✨ 核心特性

| 特性 | 描述 |
| --- | --- |
| 🚗 **玩家控制** | WASD / 方向键移动,空格 / Enter 发射子弹;同时按两方向时以最后按下的方向为准 |
| 👥 **单人 / 双人本地合作** | 双人模式:Player1 用 WASD+Space,Player2 用方向键+Enter,共享同一张地图与基地;任一玩家有命时游戏继续 |
| 🤖 **敌人 AI** | A\* 寻路绕开障碍、贴格移动不撞墙、按距离协作分工、随时间由弱变强 |
| 🗺️ **随机地图生成** | 启动时自动铺设围墙、障碍物、草地、水域 |
| 🛡️ **无敌保护** | 玩家出生 / 重生后 3 秒内免疫伤害 |
| 💥 **粒子爆炸** | 坦克被摧毁时生成爆炸特效 |
| 🔊 **音效反馈** | 移动、开火、撞击、爆炸等场景都有音效 |
| ❤️ **生命与分数** | 实时显示两位玩家的剩余生命与击毁得分(按击杀来源归入对应玩家) |
| 💀 **失败结算** | 基地被毁 **或** 双方玩家均无命后弹出失败 UI 并自动返回主菜单 |

---

## 🎮 操作说明

### 单人模式(默认 `Player 1`)

| 操作 | 按键 |
| --- | --- |
| 向上移动 | `W` |
| 向下移动 | `S` |
| 向左移动 | `A` |
| 向右移动 | `D` |
| 发射子弹 | `Space` |

### 双人模式(本地合作)

| 操作 | Player 1 | Player 2 |
| --- | --- | --- |
| 向上移动 | `W` | `↑`(`UpArrow`) |
| 向下移动 | `S` | `↓`(`DownArrow`) |
| 向左移动 | `A` | `←`(`LeftArrow`) |
| 向右移动 | `D` | `→`(`RightArrow`) |
| 发射子弹 | `Space` | `Enter`(`Return`) |

### 主菜单

| 操作 | 按键 |
| --- | --- |
| 切换选项 | `W` / `S` |
| 确认 | `Space` |

> 同时按下水平与垂直方向时,以 **最后按下的方向** 作为实际朝向,避免冲突。

---

## 🗺️ 地图元素

| 编号 | 元素 | 行为 |
| --- | --- | --- |
| 0 | 玩家基地 / 老家 | 装饰性占位 |
| 1 | 砖墙(可破坏) | 子弹可以击碎 |
| 2 | 障碍物(铁墙,不可破坏) | 子弹碰到即被销毁 |
| 3 | 出生点(Born) | 1 秒后孵化坦克后销毁 |
| 4 | 河流 / 道具 | 随机地图点缀,阻挡通行 |
| 5 | 草地 | 装饰用 |
| 6 | 钢墙 | 地图边界 |

地图以 `(-11, -9)` 到 `(11, 9)` 的 **23 × 19** 网格为范围,坦克与障碍物均在此区域内随机分布。

---

## 🛠️ 技术栈

- **引擎**:Unity **2022.3.62f1**(项目同时兼容 **Tuanjie(团结)引擎 1.9.3**)
- **语言**:C#
- **渲染**:Unity 2D(Sprite)
- **物理**:Unity 内置 2D 物理(Box2D)
- **UI**:uGUI + TextMeshPro
- **输入**:Unity `Input.GetKeyDown` / `Input.GetAxisRaw`
- **音效**:Unity `AudioSource` / `AudioClip`

---

## 📂 项目结构

```
tank/
├── Assets/
│   ├── Animation/             # 动画片段
│   ├── AnimatorController/    # Animator Controller 资源
│   ├── GameResource/          # 美术 & 音效素材
│   │   ├── AudioSource/       # WAV / AIF
│   │   ├── Fonts/
│   │   └── Graphics/          # BMP 贴图(含 Player1.bmp / Player2.bmp)
│   ├── Prefabs/
│   │   ├── Effect/            # 爆炸、护盾、Born 特效
│   │   ├── Map/               # 墙、障碍、Heart
│   │   └── Tank/              # Player / Player2 / Enemy / Bullet
│   ├── Scenes/                # Main.scene / SampleScene.unity
│   └── Scripts/               # C# 脚本(详见下表)
├── Packages/                  # 包管理(manifest.json)
├── ProjectSettings/           # Unity 工程配置
├── UserSettings/
├── tank.sln                   # Visual Studio 解决方案
└── Assembly-CSharp.csproj     # C# 项目文件
```

---

## 🚀 快速开始

### 1. 准备环境

- 安装 **Unity Hub**
- 通过 Unity Hub 安装 **Unity 2022.3.62f1**(或等价的 Tuanjie 1.9.3)
- 推荐安装 **Visual Studio 2022 / JetBrains Rider**(用于查看与编辑 C# 脚本)

### 2. 打开项目

```bash
# 在 Unity Hub 中点击 "Add",选择本项目根目录(tank.sln 所在目录)
```

或在 Unity Hub 中:
1. 点击 `Add` 选择本项目根目录
2. 在项目列表中点击该项目
3. 选择 Unity 2022.3.62f1 打开

### 3. 创建 Player 2 预制体(双人模式需要)

1. 在 `Project` 窗口定位到 `Assets/Prefabs/Tank/Player.prefab`
2. 在该文件上 **右键 → Duplicate**,得到 `Player 2.prefab`
3. 双击打开 `Player 2.prefab`
4. 选中根 GameObject,在 Inspector 中:
   - `Sprite Renderer` → `Sprite` 字段:从 `Assets/GameResource/Graphics/Player2.bmp` 中选第一帧
   - `Player` 组件 → `Tank Sprite [4]`:依次拖入 Player2.bmp 的 0、8、16、24 帧(对应上、右、下、左四个方向)

> 单人模式可跳过本步骤。**无需另建 Born 2.prefab**:Born.prefab 上已经存在 `playerPrefab` 与 `player2Prefab` 两个字段,MapCreation 与 PlayerManager 会直接复用原 Born.prefab,运行时把 `player2Prefab` 注入。

### 4. 配置场景组件

打开 `Assets/Scenes/Main.scene`,在 Inspector 中配置:

- `MapCreation` 组件:
  - `Two Player Mode`:勾选(双人)/ 不勾选(单人)
  - `Player 2 Prefab`:双人模式下拖入 `Player 2.prefab`(脚本会在 (2, -8) 孵化时把它注入到 Born 实例的 `player2Prefab` 字段)

- `PlayerManager` 组件(可选 UI):
  - `Player 1 Born`:拖入 `Born.prefab`
  - `Player 2 Born`:拖入 `Born.prefab`(同一份,脚本会再注入 `player2Prefab`)
  - `Player 2 Prefab`:双人模式下拖入 `Player 2.prefab`(供重生用)
  - 4 个 Text(可选):`Player Score Text 1/2` + `Player Life Value Text 1/2`
  - `Is Defeat UI`

### 5. 运行

1. 点击顶部 ▶ **Play** 按钮
2. **单人**:WASD 移动,`Space` 开火
3. **双人**:Player 1 WASD+Space,Player 2 ↑↓←→ + Enter

> **打包发布**:Unity → `File` → `Build Settings` → 切换平台 → `Build`。

---

## 📜 脚本说明

所有核心逻辑位于 `Assets/Scripts/` 目录下:

| 脚本 | 职责 |
| --- | --- |
| `Player.cs` | 玩家坦克移动、转向、射击、无敌时间控制;键位可配置 `moveKeys[4] + fireKey`,由 `Born.ApplyKeyMap` 根据玩家编号注入;`AttackMethod` 创建子弹后立即写 `isPlayerBullet + shootingPlayerNumber` |
| `Born.cs` | 出生点:1 秒后孵化坦克后销毁自身;**已有 `playerPrefab` 与 `player2Prefab` 两个字段**(无需单独建 Born 2.prefab);`ApplyKeyMap` 根据 `playerNumber` 注入 WASD+Space / 方向键+Enter |
| `Bullet.cs` | 子弹飞行、碰撞判定(对敌人 / 玩家 / 砖墙 / 铁墙 / Heart / Barrier);击中砖墙时调用 `MapGrid.MarkWallBroken` 让敌人重算路径;GetComponent 防御性 PlayAudio |
| `MapGrid.cs` | 静态网格:23×19 格子化地图;`Rebuild` 先 `Physics2D.SyncTransforms()` 再用 `OverlapPoint` 扫描(否则刚实例化的碰撞体查不到);`FindPathPreferOpen` 提供 **A\*** 寻路(先走空地,不行才破墙,破墙 cost=2);河流按 `name.StartsWith("River")` 识别 |
| `MapCreation.cs` | 启动时一次性随机生成地图;`MapGrid.Rebuild()` 在 InitMap 末尾调用;敌人刷怪点按模式区分(单人 2 处 / 双人 3 处),无并发上限;若 `twoPlayerMode=true` 在 `(-2,-8)` 与 `(2,-8)` 用同一份 Born 模板摆放两位玩家的出生点,运行时把 `player2Prefab` 注入 |
| `Enemy.cs` | 敌方 AI(见下方"AI 子系统详解"):A\* 寻路 + 贴格移动、按距离协作选目标(基地/玩家)、撞墙转向/砖墙开火、随时间由弱变强 |
| `EnemyDifficulty.cs` | 静态难度曲线:以 `Time.timeSinceLevelLoad` 为时钟,把敌人移动速度/开火冷却从「弱」在 `RampSeconds` 内线性插值到 Inspector 满级值 |
| `PlayerManager.cs` | 单例管理器:**共享生命池**(单人 3 命 / 双人 6 命,`Start` 里按模式设定)与 **总分 `score`**,所有玩家共用;失败判定:`heart 被毁`(TriggerDefeat)或双方都无命,3 秒后回主菜单 |
| `Heart.cs` | 基地逻辑:被击中即切换破碎贴图,调用 `PlayerManager.TriggerDefeat()` |
| `Barrier.cs` | 障碍物被击中时播放音效;`GetComponent<Barrier>` 检查避免 no receiver 警告 |
| `Explosion.cs` | 爆炸特效,0.167 秒后自动销毁 |
| `Option.cs` | 主菜单选项切换与场景切换;按 Space 时根据 `choice` 把 `MenuOptions.isTwoPlayerMode` 写入再加载战斗场景 |
| `MenuOptions.cs` | 静态类:在主菜单与战斗场景间共享单/双人模式 |

### 关键逻辑摘录

**Player 键位配置化**(`Player.cs`):通过 `moveKeys[4]` + `fireKey` 让一个组件适配 1P / 2P,`Born.ApplyKeyMap` 根据 `playerNumber` 注入默认值:

```csharp
if (playerNumber == 2)
{
    p.moveKeys = new KeyCode[4] {
        KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
    };
    p.fireKey = KeyCode.Return;
}
```

**玩家同时按两个方向时**,以 **最后按下** 的方向为准(`Player.cs`):

```csharp
if (h != 0 && v != 0)
{
    if (lastHorizontalTime > lastVerticalTime) v = 0;
    else                                      h = 0;
}
```

**子弹差异化碰撞**(`Bullet.cs`):玩家子弹只对敌人 / Heart / 可破坏墙生效;敌方子弹只对玩家生效。玩家子弹击中敌人前会写入 `enemy.killerPlayerNumber = shootingPlayerNumber`,这样击杀分记到正确的玩家身上。

**敌人刷新**(`MapCreation.cs`):每 5 秒刷新一只敌人(`InvokeRepeating("CreateEnemy", 4, 5)`),**无并发上限**;刷怪点按模式区分——**单人 2 处**(左 `(-10,8)` / 右 `(10,8)`),**双人 3 处**(再加中间 `(0,8)`),开局初始出生点同样遵循此规则。双人模式下复用同一份 Born.prefab 在 `(2, -8)` 摆放 Player 2 的出生点,并把 `player2Prefab` 注入到该 Born 实例上。

**双人模式下失败判定**(`PlayerManager.cs`):任一玩家还有命时,另一方无命不会立刻结束游戏;只有当 *双方都无命* 或基地被毁时,才弹出失败 UI 并延迟 3 秒返回主菜单。

---

## 🧠 AI 子系统详解

### 整体决策链

```
Update 每帧:
  ├─ ApplyDifficulty():按当前游戏时间刷新移动速度 / 开火冷却(随时间变强)
  ├─ 攻击冷却到点 → 开火
  └─ 目标失效(玩家/基地被毁)→ 立即重新选目标

FixedUpdate → Move():
  ├─ 每 0.5s:AcquireTarget()(协作分工) + RecalculatePath()(A* 重算)
  ├─ 不在格心 → 朝当前目标格中心直线走(不改方向)
  └─ 到达格心 → 吸附到中心,再 DecideDirection() 决策下一步
```

### 网格地图:`MapGrid`

- 把场景离散成 **23 × 19** 格,`Rebuild()` 用 `Physics2D.OverlapPoint` 逐格扫描物体类型。
- **关键修复**:`Rebuild()` 在扫描前先调用 `Physics2D.SyncTransforms()`——刚 `Instantiate` 的碰撞体默认还没同步进物理世界,不同步会导致整张网格被误判为空地(河流/铁块全查不到)。
- 河流预制体实例化后名字带 `(Clone)` 后缀,用 `name.StartsWith("River")` 识别(不能用 `== "River"`)。
- 格子类型:`Walkable(0) / PermanentBlock(1) 铁墙·河流 / BreakableWall(2) 砖墙 / HeartCell(3) 基地`。

### 寻路:`MapGrid.FindPathPreferOpen`(A\*)

- **A\*** 算法,`f = g + h`,`h` 为曼哈顿距离(admissible)。
- **第一轮**:砖墙视为阻挡,只走空地(`allowBreakable=false`)。
- **第二轮**:走不通才允许破墙(`allowBreakable=true`,破墙 `cost=2`,避免为微优化乱拆墙)。
- 每次重算时把**其它敌人当前所在格**加入 `banned` 集(`GetEnemyOccupiedCells`),让同伙自动错开、天然分散。

### 贴格移动(核心:不撞墙的关键)

敌人**一次只朝一个相邻格的中心直线移动**,到达后**精确吸附到格心**,**只有站在格心时才重新决策方向**(`DecideDirection`)。

> 这样跨轴坐标恒为整数,不会因连续位移产生累计漂移,撞墙检测(基于 `WorldToCell` 的格子)才始终准确。早期"坦克半个身子卡进墙里、脱离规划路径"的问题正源于缺少这一对齐逻辑。

### 撞墙 / 目标格反应(`DecideDirection`)

| 前方目标格 | 反应 |
| --- | --- |
| **铁墙 / 河流**(PermanentBlock) | 朝路径期望方向转 90°;若转后仍是阻挡则本帧停步,避免钻墙 |
| **砖墙**(BreakableWall) | 冷却好了就开火炸墙,本帧停步等待(不逐帧刷子弹) |
| **基地**(HeartCell) | 面向基地开火、停步不进入 —— 消除"到基地旁左右乱晃" |
| **空地**(Walkable) | 锁定为目标格,开始移动 |

### 协作分工:按距离选目标(`AcquireTarget`)

- 候选 = **基地 + 所有存活玩家**(玩家 tag = `Tank`,基地 tag = `Heart`)。
- 选**格子曼哈顿距离最近**者作为进攻目标 → 离基地近的打基地,离某玩家近的追那个玩家,敌人自然分散不扎堆。
- **迟滞**(`TargetSwitchMargin = 3`):新目标要比当前目标近至少 3 格才切换,避免两目标距离相近时反复横跳。
- 每 0.5s 随路径重算刷新一次;玩家移动后会重新分工。

### 难度递增:`EnemyDifficulty`

以 `Time.timeSinceLevelLoad`(本局经过秒数)为时钟,在 `RampSeconds` 内把敌人从"弱"线性插值到"满级";**已存活的敌人也随时间变强**(`Update` 每帧刷新),重开一局自动重置。

| 参数 | 默认 | 含义 |
| --- | --- | --- |
| `RampSeconds` | 120 s | 从开局到满级所需时间,越大越平缓 |
| `StartSpeedMul` | 0.55 | 开局移动速度 = 满级 × 0.55(更慢) |
| `StartCooldownMul` | 2.5 | 开局开火冷却 = 满级 × 2.5(打得更慢) |
| 满级值 | — | 即 Enemy 预制体 Inspector 里的 `moveSpeed` / `fireCooldown` |

### Inspector 调参(Enemy.prefab)

| 字段 | 默认 | 含义 |
| --- | --- | --- |
| `moveSpeed` | 3 | **满级(最快)**移动速度 |
| `fireCooldown` | 1.5 s | **满级(最短)**开火间隔 |

> 想整体降低难度:调小 `EnemyDifficulty.StartSpeedMul`、调大 `StartCooldownMul` 或 `RampSeconds`。

---

## 👥 双人模式约定

### 生命与分数共享

| 资源 | 单人模式 | 双人模式 |
| --- | --- | --- |
| 生命值 | **3 命** | **同一份 6 命**(共享) |
| 敌人刷怪点 | **2 处**(左 / 右) | **3 处**(左 / 中 / 右) |
| 总分 | 0 | **同一份 0 分**(共享) |
| 玩家初始位置 | (-2, -8) | P1=(-2,-8),P2=(2,-8) |
| 失败条件 | lifeValue < 0 或 Heart 毁 | 同上,但**任一方还有命时游戏继续** |

> 生命数与刷怪点数量可在 Inspector 调:`PlayerManager.singlePlayerLife / twoPlayerLife`;刷怪点数量由 `MapCreation.twoPlayerMode` 驱动(单人 2 处 / 双人 3 处)。

### 主菜单流程

```
Main.scene(主菜单,默认场景)
  ├─ Option.cs 按 W/S 切换选项
  └─ 按 Space:
      ├─ choice == 1 (单人): MenuOptions.isTwoPlayerMode = false → LoadScene(1)
      └─ choice == 2 (双人): MenuOptions.isTwoPlayerMode = true  → LoadScene(1)

SampleScene(战斗)
  └─ MapCreation.Awake 读 MenuOptions.isTwoPlayerMode 决定双/单
```

### 双人模式必做

1. **复制 Player.prefab → Player 2.prefab**(在 Project 中右键 Duplicate)
2. 双击 `Player 2.prefab`,把 SpriteRenderer 的 Sprite 改为 Player2.bmp 子 sprite
3. 把 `Born.prefab` 的 `player2Prefab` 字段拖入 Player 2.prefab
4. `MapCreation.twoPlayerMode = true`、`player2Prefab` 拖入 `Player 2.prefab`

---

## 🔭 后续可扩展方向

- [ ] **多关卡系统**:每关卡独立的 `MapGrid` 与难度起步阈值
- [ ] **道具系统**:补齐道具逻辑(无敌星、加速、炸雷、升级)
- [ ] **多种敌方坦克类型**:BigEnemy / SmallEnemy 加差异化(不同 `moveSpeed` / `fireCooldown` 分级)
- [ ] **路径距离选目标**:目标选择改用 A\* 实际路径长度替代曼哈顿直线距离,隔墙时分工更精准
- [ ] **墙体 HP**:铁墙 HP=∞,砖墙 HP=1 — 让 AI 知道一击必破
- [ ] **存档与排行榜**:PlayerPrefs 记录最高分/最远关卡
- [ ] **关卡动画**:升 level 时屏幕闪"Lv UP!"提示
- [ ] **Flocking Boids**:多 enemy 一起移动时显出"群"的自然感
- [ ] **HeartChaser 围堵**:玩家用 1 面砖墙守,敌人在两侧 2 个卡位堵门 → 玩家被迫同时防多个角度
- [ ] **完整音效 / BGM**:背景音乐与场景切换音乐
- [ ] **重制美术资源**:以原版 16×16 像素贴图为蓝本的现代高清风或赛博风

---

## 📄 许可证

本项目以学习交流为目的发布。如需商用或二次发布,请联系作者。

---

## 🙌 致谢

- 经典原作 FC/NES《Battle City》(Namco, 1985)
- Unity 官方文档与社区教程
- 所有坦克大战开源项目作者的灵感

---

Happy tanking! 🎯🚜
