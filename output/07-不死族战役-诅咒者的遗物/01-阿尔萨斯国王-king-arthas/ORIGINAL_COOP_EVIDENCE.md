# 阿尔萨斯国王：官方原版证据索引

本索引只记录 1.24e 官方地图的只读静态证据。地图、`war3map.j`、`war3map.wts` 和对象数据副本位于被 Git 忽略的 `original/`，不提交暴雪资源，也不执行地图脚本。

## 来源与完整性

| 项目 | 结果 |
| --- | --- |
| 客户端 | Warcraft III 1.24e，内部版本 `1.24.4.6387` |
| `Game.dll` SHA-256 | `5F7D8B25005BC12F8453839474EB37ADE8869ABA0570EAD2B76B6A7979C904CC` |
| 来源归档 | `game/Warcraft3-1.24e/War3Patch.mpq` |
| 归档内路径 | `Maps\FrozenThrone\Campaign\UndeadX01.w3x` |
| 地图大小 / SHA-256 | 454216 字节 / `9BC5F993FFC0DAF099CB4B1013B307962AB16176E3D5A6B17F9A255452133C40` |
| 地图格式 | 格式 25，地图版本 480，编辑器 6030，JASS |
| 静态规模 | 322 个放置单位、6485 个装饰物、1160 个脚本函数、186 个 `udg_` 全局量 |
| 检查器警告 | `game inspect` 无警告；地图 MPQ 的 `(listfile)` 不完整，25 个条目中 2 个无法按路径枚举 |

检查命令以 `.agents/skills/warcraft3-campaign-inspector/scripts/inspect.ps1` 为入口，依次使用 `game version`、`game inspect`、`archive extract` 和 `map inspect`。本地报告为 `original/undeadx01-1.24e-inspection.json`。

## 脚本锚点

| 主题 | 可复查证据 |
| --- | --- |
| 槽位与阵营 | `InitCustomPlayerSlots`、`InitCustomTeams`、`Trig_Init_01_Players_Actions` |
| 三路初始资产 | `CreateBuildingsForPlayer0/8/9`、`CreateUnitsForPlayer0/8/9`、`Trig_Create_Arthas_Actions`、`Trig_Create_Kelthuzad_Actions`、`Trig_Create_Sylvanas_Actions` |
| 友军控制 | `InitCustomTeams` 给 `Player(0/8/9)` 双向共享视野、共享控制和高级共享控制；`Trig_Start_AI_Actions` 只启动 `Player(1/2/3)` 的敌方 AI |
| 主线与可选任务 | `Trig_Quest_Slaughter_*`、`Trig_Quest_Paladins_*`、`Trig_Give_Quest_*`、`Trig_Paladins_Destroyed_*` |
| 胜负 | `Trig_Victory_Farms_Dead_*`、`Trig_Defeat_Villagers_Escape_*`、`Trig_Defeat_Units_Dead_*`、`Trig_Next_Level_*` |
| 过场 | `Trig_Intro_*`、`Trig_MidCinArthas_*`、`Trig_Victory_*`、`Trig_Failure_*`；跳过事件均只注册 `Player(0)` |
| 难度 | `Trig_Init_06a_Hard_Actions`、`Trig_Init_06b_Normal_Actions`、`Trig_Init_06c_Easy_Actions` |
| 跨关 | `Trig_Create_Heroes_Actions` 初始化 `Campaigns.w3v`；`Trig_Next_Level_Prep_Actions` 写入三名英雄并解锁任务 |

## 初始资产核对

下表直接统计生成的 `CreateBuildingsForPlayer*` / `CreateUnitsForPlayer*` 函数，不包含之后脚本创建的英雄和纯过场单位。

| 玩家 | 建筑 rawcode（数量） | 非英雄单位 rawcode（数量） | 脚本英雄 |
| --- | --- | --- | --- |
| `Player(0)` | `ncp3/uaod/usep/uslh/utom/uzig` 各 1 | `uabo` 2、`ugho` 2、`ushd` 2 | `Uear` 阿尔萨斯，10 级 |
| `Player(8)` | `ncp3/uaod/usep/utod/utom/uzig` 各 1 | `ugho` 2、`unec` 2、`ushd` 2 | `Uktl` 克尔苏加德，10 级 |
| `Player(9)` | `ncp3/uaod/usep/utom/uzig` 各 1，`utod` 2 | `uban` 4、`ugho` 2、`ushd` 2 | `Usyl` 西尔瓦娜斯，2 级、4 级经验封顶 |

三路各被直接设为 40000 金 / 40000 木。地图把三路基地分别收集到 `udg_BuildingsArthas`、`udg_BuildingsKelthuzad`、`udg_BuildingsSylvanas`，并使用三套独立的村庄波次与移动保险计时器。

## 英雄与人口证据

`War3Patch.mpq/Units/UnitBalance.slk`（SHA-256 `1DC1A308826E85C03D963EA3BE750E771153BB31BC058F9D45B8F3E6F0680F27`）的 `fused` 与地图 `war3map.w3u`（SHA-256 `EEF84A96982E93226BF4A9E6F04BDA5F712A6FCBE9CB2294F610C2B66AA151BF`）叠加核对如下：

- 三名游戏英雄 `Uear/Uktl/Usyl`，三名圣骑士 `Hhkl/Hdgo/Hmgd`，自定义游侠 `H000` 的基类 `Hvwd`，以及纯过场 `Ubal/Udth/Uvar` 的基础 `fused` 均为 `-`，且地图未对这些类型写 `ufoo`，有效人口为 0。
- 两个预置大法师 `Hamg` 与一个预置山丘之王 `Hmkg` 的基础 `fused` 为 5，地图未见对应 `ufoo` 覆盖，静态有效人口为 5。这些是敌方英雄，但仍须在第 2 阶段决定是否纳入“成品全部可达战役英雄人口为 0”的修正范围。
- 创建路径由地图生成函数与 `Trig_Create_*` 覆盖；过场会用 `Trig_Cinematic_Revival_Actions` 临时复活所需英雄。未发现变身、阵营转换或另一个英雄替换入口。

## 已确认的静态边界

- 原图没有 `GetPlayerController`、`GetPlayerSlotState`、`PLAYER_SLOT_STATE_*` 或 `EVENT_PLAYER_LEAVE` 分支。
- 原图不能证明把 `Player(8)` 或 `Player(9)` 改成真人/电脑/空槽后仍能保持其槽位生命周期、共享控制、英雄复活、过场和结算；这些属于第 2 阶段待核实项。
- `wc3-inspect` 当前不提供结构化 WTG/JASS AST 查询。本索引对函数名、玩家索引和 rawcode 的引用来自生成 JASS 的只读文本审计；未执行 JASS。
