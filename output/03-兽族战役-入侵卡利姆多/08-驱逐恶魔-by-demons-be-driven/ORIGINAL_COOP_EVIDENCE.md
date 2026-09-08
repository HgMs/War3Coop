# 驱逐恶魔：官方原版证据索引

本文件为 `ORIGINAL_COOP_MECHANICS.md` 的第 1 阶段证据附件。它只记录可复查的元数据、脚本符号和静态结论，不包含原版地图或完整 JASS。所有脚本行号均指向下述 SHA-256 对应的 `war3map.j`；若重新提取后哈希变化，行号不得沿用。

## 来源与检查结果

- 游戏目录：`game/Warcraft3-1.24e`
- 游戏版本：`1.24e`，内部版本 `1.24.4.6387`
- `Game.dll` SHA-256：`5F7D8B25005BC12F8453839474EB37ADE8869ABA0570EAD2B76B6A7979C904CC`
- 来源归档：`game/Warcraft3-1.24e/War3Patch.mpq`
- 归档条目：`Maps\Campaign\Orc08.w3m`
- 地图 SHA-256：`DE602748F1F24CE5137790A50BDB2805527622272879723AEBDADD759FEF9AB7`
- 地图格式：18；编辑器版本：4448；脚本：JASS；尺寸：116x148
- 静态规模：88 条触发字符串、402 个脚本函数、42 个 `udg_` 全局量、291 个放置单位、3189 个装饰物
- 检查器警告：无
- 检查日期：2026-09-07
- 检查方式：使用仓库内 `wc3-inspect` 只读解析；未执行 JASS，未启动游戏或 World Editor

复查命令：

```powershell
& '.agents/skills/warcraft3-campaign-inspector/scripts/inspect.ps1' game version 'game/Warcraft3-1.24e'
& '.agents/skills/warcraft3-campaign-inspector/scripts/inspect.ps1' game inspect 'game/Warcraft3-1.24e' --entry 'Maps\Campaign\Orc08.w3m'
```

## 事实、推论与未知项

- **原版事实**：下表“观察结果”均来自地图元数据、放置对象或原版 JASS 的直接静态读取。
- **静态推论**：`SLOT-06`、`SLOT-07` 和 `SCALE-01` 使用全脚本关键字扫描证明原版没有对应分支；这只能证明该精确脚本中没有显式实现，不能代替客户端行为验证。
- **多人版决定**：不在本文件中定义，以 `ORIGINAL_COOP_MECHANICS.md` 的“决定”和“目标行为”栏为准。
- **影响第 1 阶段退出的未知项**：无。空槽在目标版本中的实际大厅行为属于多人版实机验收，不是原版事实未知项。

## 逐项证据

| 机制编号 | 原版证据锚点 | 观察结果 |
| --- | --- | --- |
| `SLOT-01`、`SLOT-02`、`SLOT-04` | `InitCustomPlayerSlots` 5157-5223；`config` 5388-5407 | `Player(0)` 为用户兽人、`Player(9)` 为电脑人族、`Player(7)` 为中立人族；出生点由 `config` 固定。 |
| `SLOT-03` | `InitCustomTeams` 5225-5248 | `Player(0)`、`Player(7)`、`Player(9)` 同队，具有结盟、共享视野和共同胜利设置。 |
| `SLOT-05` | `Trig_Initialize_Order_Actions` 1556-1594 | 初始化隐藏指定槽位的计分屏显示并调整颜色，未隐藏 `Player(9)`。 |
| `SLOT-06` | `InitCustomPlayerSlots` 5207-5213；全脚本扫描 `GetPlayerController`、`GetPlayerSlotState` 均为 0 次 | 原版把 `Player(9)` 固定为电脑且没有控制器/槽位状态分支。 |
| `SLOT-07` | 全脚本扫描 `EVENT_PLAYER_LEAVE` 为 0 次 | 原版没有显式玩家离开监听、控制权移交或 AI 接管入口。 |
| `SLOT-08` | `Trig_Friends_Forever_Actions` 1923-1948 | 人族对中立敌对和 `Player(1)` 存在不对称的友好/中立设置。 |
| `ASSET-01`、`ASSET-02` | `CreateBuildingsForPlayer0` 585-608；`CreateUnitsForPlayer0` 611-634；`Trig_Game_Cache_Actions` 1722-1752 | 玩家 1的16座放置建筑、16个非英雄单位以及萨尔/凯恩缓存恢复路径相互独立。单位 rawcode 与盘点数量一致。 |
| `ASSET-03` | `Trig_Game_Cache_Actions` 1722-1752；`Trig_Game_Cache_Default_Thrall_Actions` 1763-1775；`Trig_Game_Cache_Default_Cairne_Actions` 1786-1798 | 缓存失败时创建固定8级萨尔和凯恩；缓存成功时移除回城卷轴。 |
| `ASSET-04` | `Trig_Intro_Cancelled_Actions` 2475-2523；`Trig_Intro_Cin_Actions` 2595-2741；`Trig_HarvestGold_And_Lumber_Actions` 1700-1703 | 开场正常/跳过收尾为玩家 1发放资源并执行采集命令。 |
| `ASSET-05`、`ASSET-06` | `CreateBuildingsForPlayer9` 769-809；`CreateUnitsForPlayer9` 812-850 | `Player(9)` 的33个放置建筑对象和20个单位均直接创建给该槽位；单位表含吉安娜、6农民、4步兵、3火枪手、1女巫、2牧师、3骑士。 |
| `ASSET-07` | `CreateUnitsForPlayer9` 819-831 | 吉安娜直接创建为10级，并按原版顺序学习 `AHbz/AHab/AHwe/AHmt`。 |
| `ECON-01` | `Trig_Starting_Cash_J_Actions` 4666-4669 | 人族开局资源直接设置为10000金、10000木。 |
| `ECON-02` | `Trig_Jaina_Bonus_Cash_Actions` 4680-4683；`InitTrig_Jaina_Bonus_Cash` 4686-4689 | 每180秒无条件增加200金、200木。 |
| `ECON-03` | `InitCustomTeams` 5225-5248；全脚本资源调用扫描 | 原版建立联盟和视野，但没有自动合并两个槽位的资源或人口。 |
| `TECH-01`、`TECH-02` | `InitTechTree_Player0` 5124-5131；`InitTechTree_Player9` 5139-5143 | 玩家 1和人族槽分别禁止对应的标准英雄训练。 |
| `SCALE-01` | `Trig_Not_Hard_Difficulty_*` 1605-1633；`Trig_Handicap_*` 1639-1658；全脚本合作人数扫描 | 原版只有战役难度分支，没有合作人数缩放分支。 |
| `ALLYAI-01` | `Trig_Intro_Cancelled_Actions` 2475-2523；`Trig_Intro_Cin_Actions` 2595-2741 | 正常和跳过开场均调用 `StartCampaignAI(Player(9), "o08_Jaina.ai")`。 |
| `ALLYAI-02`、`ALLYAI-03` | `Trig_Jaina_Assaults_Warsong_Actions` 4699-4707；`Trig_Jaina_Assault_Timer_Actions` 4731-4747 | AI命令3/10每180秒交替；首次周期按难度为300或540秒。 |
| `ALLYAI-04` | `Trig_Thrall_Gets_to_COP_with_Grom_Cin_Skip_Actions` 3119-3161；`Trig_Thrall_Gets_to_COP_with_Grom_Cin_Actions` 3300-3526；恶魔入侵过场正常/跳过函数 | 剧情阶段会启停人族进攻触发并向人族发送命令0。 |
| `ALLYAI-05`、`ALLYAI-06` | `Trig_Jaina_OCO_Increment_*` 4513-4528；`Trig_Jaina_OCO_Decrement_*` 4538-4551；`Trig_Jaina_Attacked_Queue_*` 4576-4657 | 人族单位死亡累积求援值，周期衰减；吉安娜死亡或玩家 1进入基地会取消待播求援。 |
| `ALLYAI-07` | 对峙、恶魔入侵、最终净化的正常和跳过函数中的 `SetPlayerAllianceStateBJ` 调用 | 剧情流程同时改变玩家 1、人族与战歌阵营的关系，并在收尾恢复。 |
| `QUEST-01` | `Trig_Initialize_Order_Actions` 1565-1570 | 初始化创建“拯救格罗姆”主线及两项要求。 |
| `QUEST-02` | `Trig_Confront_Grom_Cin_Conditions` 2792-2797；`InitTrig_Confront_Grom_Cin` 2807-2811 | 王座区域入口条件检查进入单位为萨尔。 |
| `QUEST-03` | 开场正常/跳过函数中的 `CreateItemLoc('gsou')` 和物品栏移出/放回逻辑 | 灵魂宝石固定交给萨尔并设为不可丢弃。 |
| `QUEST-04` | `Trig_Thrall_Uses_Soul_Gem_Gets_Soul_*` 3040-3067 | 只监听玩家 1使用物品，条件锁定萨尔和 `gsou`，成功后给予 `soul` 并完成要求。 |
| `QUEST-05` | `Trig_Thrall_Dead_New_Gem_*` 1961-1998 | 萨尔复活后移除灵魂、补回宝石并重置捕获要求。 |
| `QUEST-06` | `Trig_Grom_Dead_*` 2035-2066 | 任务期格罗姆死亡后立即复活、归位并根据阶段暂停。 |
| `QUEST-07` | `Trig_Thrall_Dead_Grom_Speech_*` 2004-2029；`Trig_Grom_Taunts_*` 2095-2128 | 只监听玩家 1的萨尔复活/死亡相关反馈。 |
| `QUEST-08`、`QUEST-09` | `Trig_Thrall_Gets_to_COP_with_Grom_Cin_Conditions` 3175-3186；正常/跳过动作 3119-3161、3300-3526 | 最终入口要求携带 `soul` 的萨尔；净化后清理物品并把新格罗姆交给玩家 1。 |
| `EVENT-01` | `Trig_Warsong_Attack_Timer_*` 4035-4059 | 只监听玩家 1单位遭指定战歌阵营攻击。 |
| `EVENT-02` | `Trig_Every_XSeconds_Summon_Infernals_*` 4116-4200 | 目标筛选覆盖玩家 1与人族；镜头噪声和清除只作用于 `udg_Player`。 |
| `EVENT-03` | `Trig_Chaos_Orc_Approacheth_*` 4454-4478；`Trig_SmackTalk2_*` 4484-4507 | 首次接敌只监听玩家 1，随后开启240秒区域台词计时。 |
| `REWARD-01` | `Trig_Cage_Troll_1/3/4/5/6/7_Actions` 2134-2217 | 六个牢笼完成后固定向玩家 1创建4名猎头者和2名巫医。 |
| `REWARD-02` | `Trig_See_Waterfall_*` 2224-2242；`Trig_No_More_Waterfall_*` 2248-2282 | 视野只创建给玩家 1，关闭条件和死亡监听也只统计玩家 1。 |
| `EVENT-04` | `Trig_MagicVault_Randomizer_*` 4760-4957；触发初始化状态 | 示例触发器存在，但初始化后保持禁用且没有启用路径。 |
| `CINE-01`、`CINE-02`、`CINE-03`、`CINE-04`、`CINE-05`、`CINE-06`、`CINE-07`、`CINE-08` | 开场 2451-2745；对峙 2751-3033；恶魔入侵 3596-4028；最终净化 3099-3534 | 四条主要演出广播文本/声音并使用全体电影模式；镜头、选择、跳过入口主要写死玩家 1；隐藏组含玩家 1与人族，正常和跳过路径各自收尾。 |
| `END-01` | `Trig_No_Buildings_*` 2302-2324 | 监听玩家 1单位死亡并检查 `GetPlayerStructureCount(udg_Player, true)`，只对玩家 1失败。 |
| `END-02` | `Trig_Jaina_Killed_Defeat_*` 2330-2422 | 每2秒检查吉安娜及人族农民、民兵、主基地、祭坛等恢复资产，耗尽后只对玩家 1失败。 |
| `END-03` | `Trig_Defeat_*` 2428-2445 | 监听玩家 1引擎失败事件并走自定义失败。 |
| `END-04` | `Trig_NextLevelRun_Actions` 1679-1681；最终净化跳过动作 3159 | 正常和跳过结局均只调用玩家 1胜利。 |
| `END-05` | 全局 `udg_GAMEOVER` 57；失败与结局函数中的读写点 | 原版使用单一布尔量阻止重复结算及部分结算后副作用。 |
| `END-06`、`END-08` | 与 `SLOT-07` 相同的全脚本负向扫描 | 原版没有两名玩家中途退出后的自定义接管或结算机制。 |
| `END-07` | `Trig_Jaina_Killed_Defeat_*` 2330-2422；`Trig_Thrall_Dead_New_Gem_*` 1961-1998；全脚本 `ReviveHero`/复活事件扫描 | 地图脚本把人族工人、主基地、祭坛或吉安娜视为避免任务失败的恢复资产；萨尔复活完成后另有灵魂物品复位。除剧情强制复活外，脚本没有实现跨阵营英雄复活或共享复活条件。 |
| `META-01` | `Trig_Game_Cache_Actions` 1722-1752；最终净化跳过函数中的格罗姆恢复；结局缓存保存调用 | 原版读取、恢复并保存 `Campaigns.w3v/Orc08` 的英雄状态。 |
| `META-02` | `Trig_NextLevelPrep_Actions` 1664-1668；`Trig_VICTORY_Actions` 2287-2290 | 胜利流程解锁电影、战役和下一任务，并进入战役结算。 |
| `META-03` | `Trig_Jaina_Assault_Timer_Actions` 4731-4747 | 简单/普通为300秒，困难为540秒。 |
| `META-04` | `wc3-inspect` 地图报告；`config` 5388-5407；`main` 5362-5380 | 原图为格式18、JASS、单人推荐，使用固定玩家设置、自定义队伍/科技树/升级。 |

## 第 1 阶段结论

必查类别已经覆盖；63个稳定编号均可回溯到地图元数据、放置对象、具体原版函数或明确的全脚本负向扫描。原版事实、静态推论和多人版决定已经分开记录。第 1 阶段内容可视为完成；后续若官方底图 SHA-256 变化，必须重新生成证据索引并复核全部结论。
