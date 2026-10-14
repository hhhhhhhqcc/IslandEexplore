# 浮岛开拓者 Demo：静态分析

分析对象：E:\unity\My project。日期：2026-10-14。

本报告依据 24 个 C# 脚本、SampleScene 场景、ScriptableObject 数据和项目设置。未启动 Unity Editor，未进行实际试玩、编译或性能测量；下列实现描述和缺陷证据来自源码及序列化配置。

## 游戏定位与现状

这是俯视 2D 浮岛探索、资源采集、架桥建造和近战战斗 Demo，代码内部名称为“浮岛开拓者”。项目使用 Unity 2022.3.42f1c1（中国版），传统 Input Manager 和 UGUI。当前只有一个启用场景 Assets/Scenes/SampleScene.unity，3 个 UI 预制体和 23 张 PNG 资源，Assets 下共 162 个文件（包含 .meta）。项目设置里的产品名仍为 My project。

真实玩法循环为：装备工具 → 采集资源/击杀敌人获取材料和金币 → 花金币解锁相邻岛屿 → 用材料逐格架桥 → 进入新地形采集矿物 → 建熔炉冶炼铜锭、铁锭。当前尚未形成完整的成长闭环：工具开局即有，金属锭暂无后续用途，仓库没有实际扩容逻辑；没有发现存档、任务、胜利条件、主菜单或暂停系统。

## 场景真实配置

- 地图范围：worldRadius=10，即 21×21 共 441 座岛屿；每岛 25×25 格，岛间 3 格空隙。证据：Assets/Scenes/SampleScene.unity:183-188。
- 地形：Grassland、Forest、Desert、Snowfield、Volcano。地形分类采用固定坐标的 PerlinNoise 和距离，资源和敌人的位置采用随机数。证据：Assets/Scripts/Map/TileManager.cs:36-53、207-225。
- 初始中央岛自动解锁；解锁一个岛会揭示四个相邻岛，只有与已解锁岛相邻的岛可购买。证据：TileManager.cs:30-34、110-117、125-151。
- 解锁费用为 floor(10 × 1.5^已解锁岛数)，初次手动解锁约 15 金币。证据：TileManager.cs:120-122 和场景:187-188。
- 开局发 100000 金币，以及树木、石头、纤维、铜矿石、铁矿石、金矿石、煤炭各 5 个。背包最多 8 种物品，每种 999 个。证据：Assets/Scripts/Inventory/InventoryManager.cs:9、14-23、32-46。
- 新解锁的岛尝试生成约 62-88 个资源点和 3-6 个敌人；随机重复位置可能减少实际资源点数量。证据：TileManager.cs:207-225；Assets/Scripts/Enemy/EnemySpawner.cs:11-31。
- 资源被击碎会立即发金币，掉出 2 个材料，15 秒后重生；掉落物有 4 单位磁吸范围、1.5 单位拾取范围和 30 秒生命周期。证据：Assets/Scripts/Resources/ResourceNode.cs:11-13、93-124；DroppedItem.cs:6-9、87-99。
- 玩家有 100 HP，死亡后补满血并返回 (0,0,0)，未发现资源/金币损失惩罚。证据：Assets/Scripts/Player/PlayerController.cs:236-244。

## 操作说明（按实际场景绑定）

| 操作 | 功能 |
| --- | --- |
| WASD / 方向键 | 移动，禁止走到未解锁岛和未架桥海面 |
| 1 | 剑：20 伤害，0.25 秒冷却，用于打怪 |
| 2 | 镐：15 伤害，0.35 秒冷却，用于石头、矿石、煤炭 |
| 3 | 斧：12 伤害，0.45 秒冷却，用于树木 |
| 4 | 铲：8 伤害，0.4 秒冷却，用于纤维 |
| 鼠标左键 / 空格 | 朝鼠标方向射线采集或攻击，距离 2.5 |
| 鼠标左键点击相邻未解锁岛 | 花金币解锁 |
| B | 开关建造模式 |
| 建造模式内 1 / 2 / 3 | 熔炉 / 仓库 / 桥 |
| 建造模式内滚轮 | 切换建筑 |
| 建造模式内左键 | 放置建筑 |
| E | 开关背包 |
| 靠近熔炉后右键熔炉 | 打开冶炼菜单 |
| Esc | 关闭建造模式、背包或冶炼菜单 |

工具顺序证据：SampleScene.unity:419-423。建造顺序：场景:1793-1796。操作证据：PlayerController.cs:69-128；BuildingPlacer.cs:37-52；InventoryUI.cs:28-37；Building.cs:8-27。

建筑消耗：熔炉 5 石头；仓库 8 树木；每格桥 1 树木或 1 石头。配方：1 铜矿石 + 1 煤炭 → 1 铜锭；1 铁矿石 + 1 煤炭 → 1 铁锭。对应资产位于 Assets/Scripts/Building/BuildingData 与 Assets/Scripts/Inventory/Recipe。

## 脚本架构

| 模块 | 主要职责 |
| --- | --- |
| Core/GameManager | 单例及 DontDestroyOnLoad，目前主要负责启动日志 |
| Map/TileManager | 岛屿数据、地形、解锁、资源生成、全局格坐标、桥梁可行走集合 |
| Map/TileRenderer | 将已揭示岛逐格生成 GameObject + SpriteRenderer |
| Map/TileClickHandler | 处理鼠标购买岛屿 |
| Player/PlayerController | 移动、工具选择、射线攻击、HP、边界回滚、角色与武器显示 |
| Inventory/InventoryManager | 字典物品、容量、金币 |
| Inventory/CraftingManager | 配方材料检查、扣除和产物添加 |
| Building/BuildingPlacer | 建造模式、预览、材料校验、占格、架桥 |
| Building/Building | 近距右键熔炉交互 |
| Resources/ResourceDatabase、ResourceData | 资源配置和按地形权重抽取 |
| Resources/ResourceNode、DroppedItem | 资源受击、重生、掉落、磁吸拾取 |
| Enemy/EnemyAI、EnemySpawner | 巡逻/追击/攻击/死亡状态和各地形怪物参数 |
| UI | 背包、血量/金币、工具快捷栏、冶炼按钮 |
| Sprites/SpriteDB | 地形与建筑美术资源引用 |

配置使用 ScriptableObject 是当前架构的优点：工具伤害、可采资源、资源生成权重、配方和建造材料可在 Inspector 调整。主要耦合来自多个单例、FindObjectOfType、按中文名称识别资源/建筑和多个脚本独立监听同一输入。

## 应优先处理的问题

### 1. 满背包冶炼会扣材料却丢产物（高）

CraftingManager.Craft 只检查材料，随后依次扣除材料、调用 AddItem，再直接返回 true。InventoryManager.AddItem 在已有 8 种物品且产物为新种类时静默 return。只要冶炼前有 8 种物品、材料数量消耗后仍大于 0，制作新的锭就会丢失产物；UI 仍显示成功。

证据：Assets/Scripts/Inventory/CraftingManager.cs:20-45；Assets/Scripts/Inventory/InventoryManager.cs:32-41。

建议：在扣材料前按交易后的背包状态检查容量及堆叠上限；使用 TryAddItem/交易接口反馈失败，确保扣材料与产物入包一起成功。

### 2. 资源重生后生命值忽略资源数据（高）

首次生成时 currentHealth 使用 resourceData.maxHealth，而 RespawnRoutine 使用 ResourceNode.maxHealth 默认值 30。铜矿首次 40 血、铁矿 50 血、金矿 60 血，重生后都变 30；20 血树木/纤维和25血石头重生后反而更耐打。

证据：Assets/Scripts/Resources/ResourceNode.cs:35-40、116-120；Assets/Scripts/Resources/Resources/Res_Copper.asset:20、Res_Iron.asset:20、Res_Gold.asset:20、Res_Wood.asset:20、Res_Fiber.asset:20、Res_Stone.asset:20。

建议：统一使用资源数据初始化和重置生命值。

### 3. UI、建造、解锁和攻击共享左键（高）

PlayerController、TileClickHandler、BuildingPlacer 分别在 Update 监听同一次鼠标左键；没有发现 EventSystem.IsPointerOverGameObject 或统一交互模式检查。建造点击、冶炼按钮点击可能同时触发世界攻击或岛屿解锁。数字 1-3 在建造时也同时切换工具。

证据：Assets/Scripts/Player/PlayerController.cs:78-94、125-127；Assets/Scripts/Map/TileClickHandler.cs:12-17；Assets/Scripts/Building/BuildingPlacer.cs:37-52。

建议：统一交互状态，并在 UI 命中时阻止世界交互；建造模式分配专用输入。

### 4. 建筑预览持续创建贴图和精灵（高，静态性能风险）

建造模式每帧 UpdatePreview 都调用 MakeTexSprite，函数每次 new Texture2D 和 Sprite.Create，没有复用或释放旧对象。长时间处于建造模式会持续分配 Unity 原生资源；本次没有性能测量。

证据：Assets/Scripts/Building/BuildingPlacer.cs:49、69-85、218-227。

建议：按建筑缓存预览贴图/精灵，只有切换建筑才改变引用；适时释放自行创建的资源。

### 5. 树木/木材标识不一致，树精掉落无法匹配（中）

资源数据库登记“树木”，Res_Wood.dropName 写“木材”，但 DroppedItem.Spawn 的 itemName 使用 resourceName，不读取 dropName。因此砍树实际获得“树木”；树精死亡却用“木材”调用 GetByName，数据库不存在该名称，材料掉落被跳过。

证据：Assets/Scripts/Resources/Resources/Res_Wood.asset:15-16；Assets/Scripts/Resources/DroppedItem.cs:36-37；Assets/Scripts/Enemy/EnemySpawner.cs:51-55；Assets/Scripts/Enemy/EnemyAI.cs:191-197；Assets/Scripts/Resources/ResourceDatabase.cs:37-40。

另外 EnemyAI 用 FindObjectOfType<ResourceDatabase>() 搜索 ScriptableObject 数据，而其他模块直接绑定资源数据库，应通过明确引用访问；其运行时结果需要在 Unity 验证。

建议：使用稳定物品 ID 或直接引用资源数据，并区分世界资源与背包产物。

### 6. 仓库已能建造，但没有仓储或扩容功能（中）

StorageBox.asset.description 写“扩充背包容量”，实际 Building.Update 仅处理熔炉；InventoryManager 的 MAX_UNIQUE 固定为 8，没有仓库容量变更或存取接口。仓库目前只作为占格且有碰撞的建筑。

证据：Assets/Scripts/Building/BuildingData/StorageBox.asset:23；Assets/Scripts/Building/Building.cs:24-27；Assets/Scripts/Inventory/InventoryManager.cs:9。

### 7. 地图对象和逐帧扫描成本较高（中，静态性能风险）

场景半径 10 在启动阶段创建 441×625 = 275625 个 CellData；TileRenderer 持续每帧遍历 441 岛，已揭示岛每格一个 GameObject/SpriteRenderer。全部揭示时最多 275625 个地面对象。未解锁岛每个格子还独立创建遮罩 Texture2D/Sprite，重绘销毁 GameObject 不会明确回收这些自行创建的资源。

证据：Assets/Scripts/Map/TileManager.cs:36-52、197-204；Assets/Scripts/Map/TileRenderer.cs:20-47、57-81、114-125；场景:183-185。

建议：使用 Unity Tilemap 或分块合批、事件驱动渲染、共用遮罩精灵，并仅加载玩家附近的岛。先用 Profiler 记录对象数量、内存和帧耗时再决定优化幅度。

### 8. 怪物追击未约束可行走区域（中）

巡逻目标会检查 IsWalkable，但追击仅朝玩家设置速度，没有路径搜索或边界回滚。怪物可能直接穿过海面、未解锁岛或卡在资源/建筑上。

证据：Assets/Scripts/Enemy/EnemyAI.cs:118-133、137-143；玩家则在 PlayerController.cs:218-230 检查地面回滚。

### 9. 工具快捷栏选中颜色只显示一帧（低）

ToolHotbarUI.Update 每帧先把可用栏位设为白色，只在选中序号变化时设黄色，因此选中效果下一帧被覆盖。

证据：Assets/Scripts/UI/ToolHotbarUI.cs:36-60。

### 10. 角色有两处缺失资源引用（中，需 Unity 运行验证）

场景 Player 的 SpriteRenderer.m_Sprite 指向 GUID 311925a002f4447b3a28927169b83ea6，但 Assets 的所有 .meta 中都没有这个 GUID；编辑状态下角色精灵会缺失。Player 的 Animator 已绑定现有 Assets/Player.controller（GUID 2d1d94b736176e14b94abba74ecef45f），但控制器唯一状态引用的动画片段 GUID 42f74680326a98d43acb30bf54e8b29d 同样不存在，因此 Animator 无法播放该片段。

证据：Assets/Scenes/SampleScene.unity:329、444；Assets/Player.controller:45；全量 Assets .meta GUID 索引。

PlayerController.idleSprite 和四个 walkSprites 引用的是现有 Player.png（GUID c6ba3f0c34dee8a41832333a520fa30f），四个子精灵 internalID 均与 Player.png.meta 一致。脚本在 Update 中自行设置 SpriteRenderer.sprite，闲置时设置 idleSprite，移动时每 0.15 秒轮换 walkSprites，所以运行时角色仍可能通过脚本帧动画正常显示。这两处缺失引用不能直接证明游戏无法运行，也不能据此声称角色始终不可见；需要在干净导入的 Unity 工程中核验 Console、角色显示和动画。

证据：Assets/Scenes/SampleScene.unity:412-417；Assets/Sprites/Player.png.meta:2、154、175、196、217；Assets/Scripts/Player/PlayerController.cs:130-148。

建议：先确认采用脚本帧动画还是 Animator；在上传后的开发副本中重新绑定现有静止精灵，并补齐或整理不再使用的 Animator 状态。此次分析保留源项目原貌，没有修改或删除资源。

## 玩法改进顺序

1. 优先修复材料丢失、资源重生、输入冲突及预览资源分配。
2. 完成仓库功能、存档和地图渲染性能验证，再调整开局 100000 金币和七种矿物的调试赠送。
3. 为铜锭/铁锭添加明确用途，例如升级工具、解锁新设施，再设计岛屿目标和结束条件。
4. 补充界面反馈：解锁费用提示、材料不足原因、建造模式状态、怪物血量和玩家死亡提示。
5. 加入实际试玩和 Unity Profiler 验证；当前静态审查不能证明工程可成功编译、打包或达到目标帧率。

## 适合的验证场景

- 背包 8 种物品且矿石/煤炭消耗后仍有剩余时，尝试合成新的金属锭，确认材料与产物原子处理。
- 比较铜矿/金矿第一次生成与 15 秒重生后的采集次数。
- 在熔炉 UI 点击配方、建造放置、购买岛屿时，确认只触发当前期望的行为。
- 建造模式连续运行并观察 Profiler 原生对象与内存；逐步解锁/揭示更多岛检查帧耗时。
- 击杀树精确认材料掉落；建造仓库确认实际容量/存取变化。
- 跨岛追击确认怪物不会踏上海面；重启游戏确认存档行为（目前尚未实现）。
