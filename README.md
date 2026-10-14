# IslandEexplore

一个使用 Unity 开发的俯视角 2D 浮岛探索 Demo。玩家通过采集资源、解锁岛屿、搭桥、建造熔炉和冶炼材料推进探索，并与岛上的怪物战斗。

## 打开项目

1. 克隆或下载本仓库，在 Unity Hub 中添加仓库根目录。
2. 优先使用 ProjectSettings/ProjectVersion.txt 记录的 **Unity 2022.3.42f1c1**。其他编辑器版本的导入与升级尚未验证。
3. 等待 Unity 通过 Packages/manifest.json 和 packages-lock.json 恢复包依赖。
4. 打开 Assets/Scenes/SampleScene.unity，进入 Play Mode。该场景已加入 Build Settings。

当前上传经过静态代码和资源检查，尚未在 Unity 编辑器中完成导入、编译和运行验证；请先查看下方的已知问题。

## 操作

| 操作 | 按键 / 鼠标 |
| --- | --- |
| 移动 | WASD / 方向键 |
| 装备工具 | 1 剑、2 镐、3 斧、4 铲 |
| 攻击 / 采集 | 鼠标左键或空格，朝向鼠标位置 |
| 打开 / 关闭背包 | E |
| 切换建造模式 | B |
| 建造模式选择 | 1 熔炉、2 仓库、3 桥；左键放置 |
| 查看附近熔炉配方 | 鼠标右键点击熔炉 |
| 解锁已揭示的相邻岛屿 | 左键点击岛屿；需要足够金币 |

工具和建筑模式共用数字键，左键也被多个系统处理；当前版本存在输入冲突，详见分析报告。

## 已实现的系统

- 岛屿地图、不同地形、邻岛揭示、金币解锁和可行走区域判定。
- 树木、石头、纤维以及矿物等资源的采集、掉落、拾取和重生。
- 4 类工具、8 种物品槽位的背包及相应 UI。
- 熔炉、仓库建筑和桥梁放置；当前只有铜锭、铁锭两种冶炼配方。
- 敌人生成、追击、攻击、生命值、掉落和玩家死亡后回到原点的逻辑。

仓库对象已能放置，但没有找到仓库储物或背包扩容实现；也没有找到存档、完整任务线或胜利条件。

## 项目结构

```text
Assets/
  Scenes/                  场景
  Scripts/
    Core/                  金币等全局状态
    Map/                   岛屿生成、渲染、解锁、行走判定
    Player/                移动、工具、采集和玩家生命值
    Resources/             资源定义、节点和掉落物
    Inventory/             背包、配方和制作
    Building/              建筑数据与放置
    Enemy/                 敌人行为和生成
    UI/                    背包、制作及 HUD
  Prefabs/                 UI 预制体
  Sprites/                 像素图资源及精灵数据
Packages/                  Unity 包依赖
ProjectSettings/           Unity 项目与构建配置
docs/                      游戏分析和上传审计
```

## 已知问题

- 背包满时，制作没有预先确认成品能否入包，可能扣除材料后丢失成品。
- 树精掉落使用“木材”，资源数据库使用“树木”，导致掉落查找失败。
- 场景中的玩家初始精灵和 Player.controller 的动画片段存在丢失引用。
- 建造预览反复创建 Texture2D / Sprite，存在持续分配和资源回收问题。
- 场景设置了 441 座岛；全部展开时，逐格 GameObject 渲染的潜在规模很大。

完整证据、影响和修复顺序见 [游戏分析报告](docs/GAME_ANALYSIS.md)，文件完整性和上传范围见 [上传审计](docs/UPLOAD_AUDIT.md)。

## 仓库内容

本仓库保留 Assets、Packages、ProjectSettings 及所有必要 .meta 文件，并新增说明文档、.gitignore、.gitattributes 及空目录占位文件。Library、Logs、UserSettings、IDE 缓存、生成的解决方案和历史可执行版本不进入 Git。此次整理未修复或改写游戏源码。

仓库未添加 LICENSE；原始项目也未附带素材授权说明，代码及素材的许可需由作者另行明确。
