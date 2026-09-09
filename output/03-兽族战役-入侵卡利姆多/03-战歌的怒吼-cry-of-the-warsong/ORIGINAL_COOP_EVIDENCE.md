# 战歌的怒吼：官方原版证据索引

本文件是 `ORIGINAL_COOP_MECHANICS.md` 的第 1 阶段证据附件。它只记录可复查的元数据、脚本符号、对象数据和静态结论，不提交官方地图或完整 JASS。行号均指向下述 SHA-256 对应的 `war3map.j`；若重新提取后哈希变化，行号不得沿用。

## 来源与检查结果

- 游戏目录：`game/Warcraft3-1.24e`
- 游戏版本：`1.24e`，内部版本 `1.24.4.6387`
- `Game.dll`：12140544 字节，SHA-256 `5F7D8B25005BC12F8453839474EB37ADE8869ABA0570EAD2B76B6A7979C904CC`
- 来源归档：`game/Warcraft3-1.24e/War3Patch.mpq`
- 精确归档条目：`Maps\Campaign\Orc03.w3m`
- 地图：254468 字节，SHA-256 `84ADDFD4D347B2A0A89947F47680CA4D968C8FB1DB3E649515C22CE3A659F239`
- 地图格式：18；地图版本：308；编辑器版本：4448；脚本：JASS；尺寸：92×124；推荐玩家：1
- 静态规模：94 条触发字符串、246249 字节脚本、422 个函数、54 个 `udg_` 全局量、230 个放置单位、3875 个装饰物、1 个特殊装饰物
- 脚本 SHA-256：`F4B5821EE2807E2DEA08DE13DAC90680420E7E8A8F467189079AA804A09BF913`
- 地图单位对象数据 SHA-256：`9C58426E626C1E9055E9A2B132A153498B01FD1E15A1CFE2395C394B459B39E9`
- 检查器警告：`Could not parse optional war3map.imp: Value '0' is not defined for enum of type ImportedFilesFormatVersion.`；只影响可选导入清单计数，未阻止 W3I、JASS、放置对象和 `w3u` 检查
- 检查日期：2026-09-09
- 检查方式：仓库 `warcraft3-campaign-inspector` 的 `wc3-inspect` 只读定位/解析；静态文本与结构化对象表审计；未执行 JASS，未启动游戏或 World Editor

复查命令：

```powershell
& '.agents/skills/warcraft3-campaign-inspector/scripts/inspect.ps1' game version 'game/Warcraft3-1.24e'
& '.agents/skills/warcraft3-campaign-inspector/scripts/inspect.ps1' game inspect 'game/Warcraft3-1.24e' --entry 'Maps\Campaign\Orc03.w3m'
```

## 归档与槽位事实

`wc3-inspect` 按覆盖优先级在 `War3Patch.mpq` 命中精确条目。W3I 槽位为：`P0` User Orc；`P1` Computer Human；`P2` Computer Orc；`P3` Computer Orc；`P8/P9/P10/P11` Computer Human。三个自定义队伍分别是 `P0`、人族 `P1/P8/P9/P10/P11`、战歌 `P2/P3`。

## 事实、推论与未知项

- 原版事实：下表“观察结果”均来自地图元数据、放置对象、原版 JASS 或结构化对象表的直接静态读取。
- 静态推论：`P3` 是完整友方阵营、`P2` 是辅助阵营；依据是基地/单位计数、格罗姆归属、资源保底和 AI 入口组合，不是多人版决定。
- 负向扫描：精确脚本中 `GetPlayerController`、`GetPlayerSlotState`、`EVENT_PLAYER_LEAVE` 均为 0 次；这只能证明原脚本无显式分支。
- 待核实：目标客户端中真人、电脑、空槽、未加入的大厅生命周期，以及空槽是否可运行 `o03_grom.ai`，必须由第 2 阶段最小隔离实机测试回答。

## 逐项证据

| 机制编号 | 原版证据锚点 | 观察结果 |
| --- | --- | --- |
| `SLOT-01`、`SLOT-02`、`SLOT-03`、`SLOT-04` | `InitCustomPlayerSlots` 4763-4829；`InitCustomTeams` 4831-4900；`Trig_Initial_Alliances_Actions` 1340-1363 | `P0` 唯一用户；`P3` 与 `P2` 为电脑兽人战歌队。运行期另令 `P0↔P2/P3` 结盟，并从 `P0` 向两者开放共享法术。 |
| `SLOT-05`、`ALLYAI-05`、`EXTRA-02`、`EXTRA-04` | 全脚本关键字扫描 | 控制器/槽位状态/玩家离开三类 API 均未出现；原图没有四分支、一次性模式标记或离开接管。 |
| `ASSET-01` | `CreateUnitsForPlayer0` 597-613；`Trig_Load_Thrall_Actions` 1707-1723 | `P0` 的 9 个非英雄单位为 `ogru`×2、`orai`×2、`opeo`×4、`okod`×1；建筑为 0；萨尔独立从缓存恢复或创建。 |
| `ASSET-02` | `CreateBuildingsForPlayer3` 686-709；`CreateUnitsForPlayer3` 712-730；`Trig_Load_Grom_Actions` 1737-1754 | `P3` 有 16 座建筑和 11 个非英雄单位，格罗姆另行恢复/创建。建筑为 `owtw`×3、`ostr`、`otrb`×8、`ofor/oalt/obar/obea`；单位为 `orai`×5、`opeo`×3、`ogru`×3。 |
| `ASSET-03` | `CreateUnitsForPlayer2` 674-683；无 `CreateBuildingsForPlayer2`；资源与升级初始化扫描 | `P2` 只有 `orai`×2、没有建筑或独立英雄；获得全玩家默认 1000/1000，无预研升级。 |
| `ECON-01` | `Trig_Initial_Resources_Actions` 1318-1323 | 所有玩家先置 1000/1000，`P0` 再置 1500 金、500 木。 |
| `ECON-02`、`ALLYAI-04` | `Trig_GromGold_Actions` 1524-1526；`Trig_GromLumber_Actions` 1538-1540 及对应玩家状态注册 | `P3` 金或木不高于 1000 时，对应资源增加 1000。 |
| `TECH-01`、`TECH-02` | `InitTechTree_Player0` 4667-4691；`InitTechTree_Player2` 4692-4699；`InitTechTree_Player3` 4701-4708；`InitUpgrades_Player0` | `P0` 有固定升级和上限；`P2/P3` 都禁止 `Obla/Ofar/Otch/Hpal/Hamg/Hmkg`，且无脚本预研升级。 |
| `HERO-01` | `Trig_Initial_Heroes_Actions` 1664-1692；`Trig_Load_Thrall_Actions` 1707-1723；萨尔等级事件 | `Othr` 从 `Thrall/Orc03` 恢复给 `P0`；兜底为 3 级并学习 `AOcl/AOsf/AOfs`；`P0` 经验 handicap 为 75%，脚本设有 5 级限制。 |
| `HERO-02` | `Trig_Load_Grom_Actions` 1737-1754；`Trig_ActivateGromAI_Actions` 2859-2867；`Trig_StartExitCinematic_Actions` 3888-4280 | `Ogrh` 从 `Grom/Orc03` 恢复给 `P3`，兜底为 4 级；AI 激活可复活原格罗姆；终场另创建 `P2` 的 `Ogrh`。 |
| `HERO-03` | `CreateUnitsForPlayer1` 651-683；`CreateUnitsForPlayer8` 762-791；`CreateUnitsForPlayer9` 811-840；`CreateUnitsForPlayer11` 895-907；困难分支 | 可达敌方英雄 rawcode 为 `Hamg/Hpal/Hmkg`；其中 `Hamg` 有两个放置实例。困难把主要 `Hamg/Hpal/Hmkg` 分别设为 4/5/6 级。 |
| `HERO-FOOD-01` | 1.24e `UnitBalance.slk` 的 `fused`；地图 `w3u` 结构化覆盖检查 | `Othr/Ogrh` 基础 `fused` 为空（有效 0）；`Hamg/Hpal/Hmkg` 为 5。地图 `w3u` 只对 `Ogrh` 改 `ubld=30`，上述英雄均无 `ufoo` 覆盖。 |
| `ALLYAI-01` | `Trig_Initial_AIs_Actions` 1418-1423；`Trig_LastBuildingDies_*` 2528-2558；`Trig_ActivateGromAI_Actions` 2859-2867 | 开局启动的都是人族敌方 AI；风暴守卫最后建筑死亡或战歌被激怒后才启动 `P3/o03_grom.ai`。 |
| `ALLYAI-02` | 战歌移动/会话函数；`Trig_ActivateGromAI_Actions` | AI 启动前地图直接命令格罗姆和战歌单位；启动入口还包含格罗姆死亡复活。 |
| `ALLYAI-03` | `Trig_TriggerBuildTownQuest_*` 2648-2722；`Trig_GromAggros*` 3015-3218；`Trig_PlayerAggros*` 3227-3399；`Trig_CheckForAllAggro_*` 3403-3430 | `P3` 收到阶段命令 1、4、3、5、2；人族槽索引 `1/8/9/10` 被作为命令数据传入。 |
| `ALLYAI-06` | `Trig_PlayerBuildsTower_*` 3594-3629；`Trig_PlayerTowerDies_*` 3635-3670；`Trig_Raider_Attack_*` 3700-3727 | `P2` 无战役 AI；两名固定劫掠者每 2 秒攻击 `P0` 在两个指定区域内建造的塔。 |
| `QUEST-01` | `Trig_Initial_Quests_Actions` 1374-1391；`Trig_TriggerBuildTownQuest_*` 2648-2722 | 建镇主线仅注册 `P0` 建造 `ogre`；完成后发现飞艇任务并推进战歌 AI。 |
| `QUEST-02`、`END-03` | `Trig_Building_Check_*` 3788-3853；`Trig_For_Every_Zeppelin_at_Home_Actions` 3859-3861；`Trig_ZeppelinVictory_*` 4415-4433 | 只统计 `P0` 的存活 `nzep`；每艘在 `P0` 存活建筑 768 范围内计 1，至少 2 艘触发任务完成和终场。 |
| `REWARD-01` | `Trig_All_Harpies_Dead_*` 3755-3782 | 鹰身人全灭后把 `ngad` 替换为无敌 `nC00`，并完成该要求；未直接增加金木。 |
| `QUEST-03`、`REWARD-02` | `Trig_All_Bases_Dead_*` 3520-3549；`Trig_All_Bases_Dead_Message_*` 3555-3574 | 可选任务要求 `P1/P8/P9/P10` 建筑均为 0；只完成任务和广播消息，`P11` 不计入，未见资源/物品/单位奖励。 |
| `EVENT-01` | `InitTrig_PlayerBuildsTower` 3625-3629；`InitTrig_PlayerTowerDies` 3665-3670；`InitTrig_Current_Target_Dies` 3689-3694；格罗姆激怒与飞艇函数 | 多个战斗/任务入口明确注册或统计 `P0`。 |
| `CINE-01`、`CINE-02`、`CINE-03`、`UI-01` | 开场函数；`Trig_StartExitCinematic_Actions` 3888-4280；`Trig_ExitCinematicEscape_Actions` 4291-4305；对应跳过注册 | 电影模式和大量对白面向全体；镜头、选择、雾区与跳过以 `P0` 为主。终场复活萨尔、创建 `P2/Ogrh` 并重排战歌资产。 |
| `END-01` | `Trig_ThrallDefeat_*` 4347-4370；`Trig_BuildingDefeat_*` 4380-4403；建镇完成函数 | 建镇前检查萨尔死亡；建镇后切换到 `P0` 建筑全灭失败。 |
| `END-02` | `Trig_All_Unit_Defeat_*` 4314-4334；`Trig_Level_Defeat_Actions` 4477-4479 | `P0` 全部单位为 0 也会失败；最终仅 `CustomDefeatBJ(Player(0), ...)`。 |
| `END-04` | `Trig_ActivateGromAI_Actions` 2859-2867；终场函数 | 脚本只在 AI 激活时复活格罗姆、终场复活萨尔；无跨阵营共享复活机制。 |
| `END-05` | `udg_GameOver` 的读写点；胜负作弊触发 | 单一布尔量阻止重复入口；胜负作弊只注册 `P0`。 |
| `DIFF-01` | 简单、非困难、困难初始化函数 | 简单降低指定敌对槽 handicap；简单/普通移除两座兵营；困难提升 `Hamg/Hpal/Hmkg` 等级；无合作人数分支。 |
| `META-01` | `Trig_Initial_Heroes_Actions` 1664-1692；`Trig_Level_Enable_Actions` 4447-4454 | 读取 `Campaigns.w3v`；胜利前把萨尔存为 `Thrall/Orc06`、格罗姆存为 `Grom/Orc04` 并保存。 |
| `META-02` | `Trig_Level_Enable_Actions` 4447-4454；`Trig_Level_Victory_Actions` 4463-4466 | 解锁兽族第 4 关，下一关为 `Maps\Campaign\Orc04.w3m`，胜利只给 `P0`。 |
| `COMPAT-01` | `wc3-inspect` 报告；`config`、`main` | 格式 18、JASS、编辑器 4448、推荐 1 人，固定玩家并使用自定义队伍/科技/升级。 |
| `EXTRA-01`、`EXTRA-03` | W3I 队伍、初始化联盟及全脚本资源/控制调用扫描 | 原版没有合作人数选择；只见共享法术，没有自动共享资源、普通控制或高级控制。 |

## 第 1 阶段结论

必查类别均有可复查锚点。官方原图的主要阻塞未知项是 `P3` 在目标客户端的四类槽位生命周期与空槽 AI 可运行性；这不是静态检查可以代替的结论。所有多人版取舍仍为“待决定”，本文件未进入第 2 阶段。
