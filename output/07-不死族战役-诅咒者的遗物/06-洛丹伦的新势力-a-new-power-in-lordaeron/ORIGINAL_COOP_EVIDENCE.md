# 洛丹伦的新势力：官方原版证据索引

本索引只记录 1.24e 官方地图的只读静态证据。地图、脚本、WTS 和对象数据副本位于被 Git 忽略的 `original/`，不提交暴雪资源，也不执行地图脚本。

## 来源与完整性

| 项目 | 结果 |
| --- | --- |
| 客户端 | Warcraft III 1.24e，内部版本 `1.24.4.6387` |
| `Game.dll` SHA-256 | `5F7D8B25005BC12F8453839474EB37ADE8869ABA0570EAD2B76B6A7979C904CC` |
| 来源归档 / 条目 | `game/Warcraft3-1.24e/War3Patch.mpq` / `Maps\FrozenThrone\Campaign\UndeadX06.w3x` |
| 地图大小 / SHA-256 | 355927 字节 / `42B71EB32E54FD2C980477A94195BB7FEEC3F7F3878449B0643D9C3071485C1E` |
| 地图格式 | 格式 25，地图版本 925，编辑器 v6031，JASS |
| 静态规模 | 324 个放置单位、4798 个装饰物、596 个脚本函数、90 个 `udg_` 全局量 |
| 检查器警告 | `game inspect` 无警告；地图 MPQ 的 `(listfile)` 不完整，24 个条目中 2 个无法按路径枚举 |

本地检查报告为 `original/undeadx06-1.24e-inspection.json`，脚本 SHA-256 为 `2C67DB6BAAC3B0DE0C77F3B0D5A1283348CAFD6AABC7798DC51B58308EB8C38A`。

## 脚本锚点

| 主题 | 可复查证据 |
| --- | --- |
| 槽位、联盟、资源 | `InitCustomPlayerSlots`、`InitCustomTeams`、`Trig_Init_01_Players_Actions` |
| 双阵营资产 | `CreateBuildingsForPlayer1/8`、`CreateUnitsForPlayer1/8`、`Trig_Load_Sylvanas_Actions`、`Trig_Load_Varimathras_Actions`、`Trig_Create_Garithos_Actions` |
| AI | `Trig_Start_AI_Actions` 只启动敌对 `Player(3/4/5/6)`，无加里瑟斯友方 AI |
| 主线/可选任务 | `Trig_Quest_Balnazzar_*`、`Trig_Quest_Dwarves_*`、`Trig_Ironforge_Dwarves_*` |
| 胜负 | `Trig_Victory_Balnazzar_Dead_*`、`Trig_Defeat_Units_Dead_*`、`Trig_Next_Level_*` |
| 过场 | `Trig_Intro_*`、两组 `Trig_Dwarves_*`、`Trig_Victory_*`；全部跳过事件只注册 `Player(1)` |
| 难度 | `Trig_Init_07a_Hard_Actions`、`Trig_Init_07b_Normal_Actions`、`Trig_Init_07c_Easy_Actions` |
| 跨关 | `Trig_Load_Heroes_Actions` 读取 `Campaigns.w3v`；`Trig_Next_Level_Prep_Actions` 写 `Campaigns.w3x` 并保存三名英雄 |

## 初始资产核对

| 玩家 | 建筑 rawcode（数量） | 非英雄单位 rawcode（数量） | 英雄 |
| --- | --- | --- | --- |
| `Player(1)` 西尔瓦娜斯 | 15：`uaod/ugol/ugrv/unp1/usap/uslh/utod/utom/uzig` 各 1，`usep` 2，`uzg2` 4 | 17：`uaco` 5、`uban` 3、`uabo/ucry/ugho/ushd` 各 2、`unec` 1 | 从缓存恢复 `Usyl/Uvar`，失败则各创建 8 级默认英雄 |
| `Player(8)` 加里瑟斯 | 23：`hhou` 12、`hgtw` 4、`hbar` 2，其余 `halt/hbla/hkee/hlum/hvlt` 各 1 | 15：`hpea` 7、`hfoo` 4、`hkni` 4 | 固定创建 8 级 `Hlgr` |

两套经济均为 500 金 / 150 木，开场正常或跳过清理都会分别给两边工人下达采金/伐木命令。`Player(1)` 与 `Player(8)` 双向同盟、共同胜利、共享视野、普通及高级控制；原版没有给 `Player(8)` 启动 AI。

## 英雄与人口证据

1.24e `UnitBalance.slk` 与地图 `war3map.w3u`（SHA-256 `E297A576B41715C631A81AA60D795359A030788DEB932293277DE6259F4C3A55`）叠加：

- 合作相关与首领英雄 `Usyl/Uvar/Hlgr/Ubal` 的基础 `fused` 为 `-`，地图没有对应 `ufoo` 覆盖，有效人口为 0。
- 两个敌方自定义巫妖 `U000/U001` 继承 `Ulic`；地图只覆盖名称/称谓，没有 `ufoo`，因此有效人口继承为 5。
- 西尔瓦娜斯、法理玛瑟斯存在缓存恢复和默认创建两条路径；加里瑟斯、巴那泽尔、两个巫妖为脚本创建。过场会强制复活西尔瓦娜斯、法理玛瑟斯、加里瑟斯、巴那泽尔；未发现变身或阵营转换英雄路径。

## 已确认的静态边界

- 原图不读取槽位状态/控制器，不处理玩家离开；不能由脚本可达性推断真人、电脑、空槽或未加入的运行结果。
- 原版加里瑟斯阵营是“中立控制 + 共享高级控制”，不是带 AI 的电脑盟友。若提升为可选真人槽，电脑/空槽回退路线必须在第 2 阶段另行决定，并在第 6 阶段实机验证受支持配置。
- WTG/JASS AST 尚不在检查器结构化覆盖范围内；函数和 rawcode 来自生成 JASS 的只读文本审计。
