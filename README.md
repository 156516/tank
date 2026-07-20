# 🎮 Tank Battle(坦克大战)

一款基于 **Unity 2D** 制作的经典坦克大战复刻游戏。玩家控制己方坦克,在随机生成的地图中击毁不断来袭的敌方坦克,同时保护我方基地(Heart),坚持到最后一刻。

支持 **单人 / 双人本地合作** 两种模式:单人模式使用 `WASD + Space`;双人模式下一位玩家沿用 `WASD + Space`,另一位使用方向键 + `Enter`,共享同一张地图和同一座基地。

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
| 🤖 **敌人 AI** | 随机方向切换、定时开火、坦克之间不会重叠 |
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

地图以 `(-11, -9)` 到 `(11, 9)` 的 22 × 18 网格为范围,坦克与障碍物均在此区域内随机分布。

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
| `Player.cs` | 玩家坦克移动、转向、射击、无敌时间控制;键位可配置:`moveKeys[4]` + `fireKey`,由 `Born.ApplyKeyMap` 根据玩家编号注入 |
| `Enemy.cs` | 敌方 AI 坦克随机移动、定时射击、坦克间碰撞避让;击杀时记入对应玩家分数(`killerPlayerNumber`) |
| `Bullet.cs` | 子弹飞行、碰撞判定(对敌人 / 玩家 / 墙体 / Heart 的差异化处理);携带 `shootingPlayerNumber` 用于正确记分 |
| `MapCreation.cs` | 启动时一次性随机生成地图;周期性在三个刷新点孵化敌人;若 `twoPlayerMode=true` 复用 `item[3]` Born 模板在 `(-2,-8)` 与 `(2,-8)` 摆放两位玩家的出生点,运行时把 `player2Prefab` 字段注入 |
| `PlayerManager.cs` | 单例管理器:分别管理两位玩家的生命与分数、独立重生与失败判定(双方都无命或基地被毁才判负);Player 2 重生时也会向 Born 实例注入 `player2Prefab` |
| `Born.cs` | 出生点:1 秒后孵化坦克后销毁自身;已有 `playerPrefab` 与 `player2Prefab` 两个字段,`ApplyKeyMap` 根据 `playerNumber` 注入 WASD+Space / 方向键+Enter,playerNumber=2 时优先使用 `player2Prefab` |
| `Heart.cs` | 基地逻辑:被击中即切换破碎贴图,调用 `PlayerManager.TriggerDefeat()` 进入失败 |
| `Barrier.cs` | 障碍物被击中时播放音效 |
| `Explosion.cs` | 爆炸特效,0.167 秒后自动销毁 |
| `Option.cs` | 主菜单选项切换与场景切换 |

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

**敌人刷新**(`MapCreation.cs`):每 5 秒在顶部三处随机位置刷新一只敌人(`InvokeRepeating("CreateEnemy", 4, 5)`);双人模式下复用同一份 Born.prefab 在 `(2, -8)` 摆放 Player 2 的出生点,并把 `player2Prefab` 注入到该 Born 实例上。

**双人模式下失败判定**(`PlayerManager.cs`):任一玩家还有命时,另一方无命不会立刻结束游戏;只有当 *双方都无命* 或基地被毁时,才弹出失败 UI 并延迟 3 秒返回主菜单。

---

## 🔭 后续可扩展方向

- [ ] **多关卡系统**:增加关卡选择与 BOSS 关卡
- [ ] **道具系统**:补齐道具逻辑(无敌星、加速、炸雷、升级)
- [ ] **多种敌方坦克类型**:高速型、重甲型、追踪导弹型
- [ ] **更智能的 AI**:基于有限状态机 / 行为树
- [ ] **存档与排行榜**:保存最高分
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
