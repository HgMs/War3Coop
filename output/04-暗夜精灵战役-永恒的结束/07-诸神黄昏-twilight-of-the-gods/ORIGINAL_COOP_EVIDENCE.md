# 诸神黄昏：官方原版证据索引

本文件是 `ORIGINAL_COOP_MECHANICS.md` 的第 1 阶段证据附件。它只记录可复查的元数据、脚本符号、对象数据和静态结论，不提交官方地图或完整 JASS。行号均指向下述 SHA-256 对应的 `war3map.j`；若重新提取后哈希变化，行号不得沿用。

## 来源与检查结果

- 游戏目录：`game/Warcraft3-1.24e`
- 游戏版本：`1.24e`，内部版本 `1.24.4.6387`
- `Game.dll`：12140544 字节，SHA-256 `5F7D8B25005BC12F8453839474EB37ADE8869ABA0570EAD2B76B6A7979C904CC`
- 来源归档：`game/Warcraft3-1.24e/War3Patch.mpq`
- 精确归档条目：`Maps\Campaign\NightElf07.w3m`
- 地图：303991 字节，SHA-256 `97143C5E8F417DB033B566ED54A2359D6A2BA609F343A3DE79ABADF50A3D1C92`
- 地图格式：18；地图版本：898；编辑器版本：4448；脚本：JASS；尺寸：84×148；推荐玩家：1
- 静态规模：112 条触发字符串、432301 字节脚本、607 个函数、130 个 `udg_` 全局量、173 个放置单位、4078 个装饰物、0 个特殊装饰物
- 脚本 SHA-256：`28FC6332720635640C7CBE854D21DBCDBD64BCA6489FAAFA141D35A8F51B34CC`
- 地图单位对象数据 SHA-256：`06EC252CFCD1ED04F99AA132073BB74C5B23CF9F62210EEAA176CE931F05B73A`
- 检查器警告：`Could not parse optional war3map.imp: Value '0' is not defined for enum of type ImportedFilesFormatVersion.`；只影响可选导入清单计数，未阻止 W3I、JASS、放置对象和 `w3u` 检查
- 检查日期：2026-09-09
- 检查方式：仓库 `warcraft3-campaign-inspector` 的 `wc3-inspect` 只读定位/解析；静态文本与结构化对象表审计；未执行 JASS，未启动游戏或 World Editor

复查命令：

```powershell
& '.agents/skills/warcraft3-campaign-inspector/scripts/inspect.ps1' game version 'game/Warcraft3-1.24e'
& '.agents/skills/warcraft3-campaign-inspector/scripts/inspect.ps1' game inspect 'game/Warcraft3-1.24e' --entry 'Maps\Campaign\NightElf07.w3m'
```

## 归档与槽位事实

`wc3-inspect` 按覆盖优先级在 `War3Patch.mpq` 命中精确条目。W3I 槽位为：`P0` Computer Orc，`P1` User NightElf，`P2/P3/P6/P10` Computer Undead，`P4/P9` Computer Human，`P7/P8` Rescuable NightElf。队伍为大联盟 `P0/P1/P4/P9`、燃烧军团 `P2/P3/P6/P10`、可营救阵营 `P7/P8`。脚本另把 `P11` 绑定为第四亡灵基地变量。

## 事实、推论与未知项

- 原版事实：下表“观察结果”均来自地图元数据、放置对象、原版 JASS 或结构化对象表的直接静态读取。
- 静态推论：`P0/P9` 是完整友方阵营；依据是基地/单位计数、英雄、资源、补贴和专属战役 AI 的组合，不是多人版决定。
- 负向扫描：精确脚本中 `GetPlayerController`、`GetPlayerSlotState`、`EVENT_PLAYER_LEAVE` 均为 0 次；这只能证明原脚本无显式分支。
- 待第 6 阶段实机验证：目标客户端中真人、电脑、空槽、未加入的大厅生命周期，以及受支持大厅配置能否运行两份友方 AI。第 2 阶段只据此定义目标行为、支持范围、验收步骤和失败策略。

## 逐项证据

| 机制编号 | 原版证据锚点 | 观察结果 |
| --- | --- | --- |
| `SLOT-01`、`SLOT-02`、`SLOT-03`、`SLOT-04`、`SLOT-05`、`SLOT-06` | `InitCustomPlayerSlots` 7670-7754；`InitCustomTeams` 7756-7835；`InitGlobals` 570-614 | `P1` 唯一用户；`P0/P9` 为大联盟电脑友军；`P4` 临时辅助，`P7/P8` 可营救；`P2/P3/P6/P10/P11` 绑定敌方阶段。 |
| `SLOT-07`、`ALLYAI-05`、`EXTRA-02`、`EXTRA-04` | 全脚本关键字扫描 | 控制器/槽位状态/玩家离开三类 API 均未出现；原图没有四分支、一次性模式标记或离开接管。 |
| `ASSET-01` | `CreateBuildingsForPlayer1` 902-925；`CreateUnitsForPlayer1` 928-955；`Trig_Load_Tyrande_*` 2227-2262；`Trig_Load_Furion_*` 2270-2310 | `P1` 有 16 座建筑、20 个非英雄单位；`Etyr/Efur` 另从缓存恢复或脚本创建。 |
| `ASSET-02` | `CreateBuildingsForPlayer0` 842-864；`CreateUnitsForPlayer0` 867-899 | `P0` 有 15 座建筑、24 个非英雄单位及 `Othr`。非英雄构成为 `nglm`×10、`ocat`、`opeo`×6、`ogru`×2、`ohun`×2、`okod/oshm/otau` 各 1。 |
| `ASSET-03` | `CreateBuildingsForPlayer9` 1076-1115；`CreateUnitsForPlayer9` 1118-1140 | `P9` 有 32 座人族/营帐建筑、12 个非英雄单位及 `Hjai`；非英雄为 `hpea`×5、`hfoo`×3、`hrif`×2、`hmtm`×2。 |
| `ECON-01` | `Trig_Map_Initialization_Actions` 1950-2201，资源设置 2156-2169 | `P1` 为 1000/800，`P0/P9` 为 6500/6500，主要敌方槽为 10000/10000。 |
| `ECON-02` | `Trig_Thrall_Gold/Lumber_Actions` 2560-2580；`Trig_Jaina_Gold/Lumber_Actions` 2590-2609；对应 60 秒触发启停点 | `P9` 从开局周期 +100/+100；`P0` 补贴在吉安娜失守后启用、萨尔失守后停用。 |
| `TECH-01` | `InitUpgrades_Player0` 7496-7510；`InitTechTree_Player0` 7617-7621 | `P0` 预研 `Rosp=3`、`Rome/Rora/Roar=2` 等 13 项科技，禁用 `Obla/Ofar/Otch`。 |
| `TECH-02` | `InitUpgrades_Player9` 7570-7581；`InitTechTree_Player9` 7643-7647 | `P9` 预研 11 项人族科技，禁用 `Hpal/Hmkg/Hamg`。 |
| `TECH-03` | `InitUpgrades_Player1` 7512-7516；`InitTechTree_Player1` 7623-7629 | `P1` 预研 `Renb/Reht`，禁用 `Recb/Ekee/Emoo/Edem/echm/edos`。 |
| `HERO-01` | `Trig_Load_Tyrande_Actions` 2235-2262；`Trig_Load_Furion_Actions` 2278-2310 | `Etyr/Efur` 分别从 `Tyrande/NightElf07`、`Furion/NightElf07` 恢复给 `P1`；失败时创建 7 级固定技能英雄。玛法里奥获得不可丢弃、无敌 `cnhn`；恢复英雄各加 700 XP。 |
| `HERO-02` | `CreateUnitsForPlayer0` 898；`CreateUnitsForPlayer9` 1134；`Trig_Jaina_Cinematic_Actions` 4227-4417；`Trig_Thrall_Cinematic_Actions` 5593-5784；`Trig_Next_Level_Run_Actions` 6719-6727 | `Othr/Hjai` 直接创建并设为 10 级；失守过场死亡时复活，随后隐藏并改属中立被动；计分前还原为 `P0/P9`。 |
| `HERO-03` | `CreateUnitsForPlayer2` 976-1013；`CreateUnitsForPlayer6` 1018-1042；`Trig_Start_Second_Undead_Base_Actions` 3820-3863；`Trig_Start_Third_Undead_Base_Actions` 5121-5167 | 敌方 rawcode 为 `Udre/Ulic/Npld/Uwar`，均 10 级。前三者初始化移除，随后在第二、第三亡灵基地阶段复活并改属；未发现 `ReplaceUnit`。 |
| `HERO-04` | `CreateUnitsForPlayer1` 941；`Trig_Map_Initialization_Actions` 1959；`Trig_Shandris_Line_*` 3504-3559 | 命名角色珊蒂斯以 `eshd/P1` 放置，只用于存活检查和对白；没有英雄等级、技能、缓存、替换或复活路径。 |
| `HERO-FOOD-01` | 1.24e `UnitBalance.slk` 的 `fused`；地图 `w3u` 结构化覆盖检查 | `Etyr/Efur/Othr/Hjai/Udre/Ulic/Npld/Uwar` 的基础 `fused` 均为空（有效 0），地图没有这些 rawcode 的 `ufoo` 覆盖；`eshd` 显式覆盖 `ufoo=0`。`UC13` 虽基于 `Uwar` 出现在对象表，但脚本没有创建，故不列为可达英雄。 |
| `ALLYAI-01` | `Trig_Start_AI_Actions` 3254-3266；`Trig_Intro_Cancel_Actions` 3063-3123；正常开场收尾 | 正常和跳过均依次启动 `P0/n07_red.ai`、`P9/n07_lightblue.ai` 与第一亡灵基地；两个友方 AI 间隔 1 秒。 |
| `ALLYAI-02` | `Trig_Jaina_Call_For_Help_01/02/03_*` 3587-3790；`Trig_Jainas_Castle_Destroyed_Actions` 3565-3579；`Trig_Jaina_Cinematic_Actions` 4227-4417；防区入口 4851 | `P9` 有三档求援与命令 0；城堡失守后停止第一敌 AI、撤场 `P9`、切换补贴并启动第二敌方基地。 |
| `ALLYAI-03` | `Trig_Thrall_Call_For_Help_01/02/03_*` 4888-5091；`Trig_Thralls_Fortress_Destroyed_Actions` 4865-4879；`Trig_Thrall_Cinematic_Actions` 5593-5784；兽族塔事件 6235 | `P0` 有三档求援与命令 0；堡垒失守后停止第二敌 AI、撤场 `P0`、停用补贴并启动第三敌方基地。 |
| `ALLYAI-04` | 两个失守过场及正常/跳过收尾 | 友军基地失守只推进剧情阶段；没有根据控制器动态切换 AI 的路径。 |
| `QUEST-01` | `Trig_Map_Initialization_Actions` 1950-2201；开场正常/跳过收尾 | 创建唯一必做任务及“坚持 45 分钟”“阻止阿克蒙德到达世界之树”两项要求。 |
| `QUEST-02` | `Trig_StartTimers_Actions` 3319-3330；吉安娜/萨尔过场正常与跳过动作 | `GameTimer=2700` 秒、`ArchimondeTimer=2655` 秒、初始 Wisp 计时为 1740 秒；两个失守过场暂停并恢复计时器。 |
| `QUEST-03` | 吉安娜求援函数 3587-3790；萨尔求援函数 4888-5091 | 攻击事件分别监听 `P9/P0`；消息、ping 和快速镜头使用 `P1`/暗夜精灵玩家组。 |
| `REWARD-01` | `Trig_Save_Credits_Variable_Actions` 6742-6750；`Trig_Next_Level_Run_Actions` 6719-6727 | 胜利奖励是缓存标记、结局电影解锁与下一图；未发现向合作候选发放单位、物品或资源。 |
| `CINE-01` | 地图初始化与开场函数；`Trig_Intro_Cancel_Actions` 3063-3123；跳过注册 3132 | 全体进入开场电影模式且对白广播全体；镜头/选择和唯一跳过入口为 `P1`；正常/跳过均汇合到 AI 和计时启动。 |
| `CINE-02` | `Trig_Jaina_Cinematic_Actions` 4227-4417；`Trig_Jaina_Cancel_Actions` 4462-4526；跳过注册 4539 | 过场处理 `P1` 镜头/选择，强制复活并撤场吉安娜；两条收尾均启动第二阶段并切换补贴。 |
| `CINE-03` | `Trig_Thrall_Cinematic_Actions` 5593-5784；`Trig_Thrall_Cancel_Actions` 5833-5900；跳过注册 5913 | 过场处理 `P1` 镜头/选择，强制复活并撤场萨尔；两条收尾均启动第三阶段并停用补贴。 |
| `CINE-04`、`UI-01` | `Trig_Victory_Cinematic_Actions` 6919-7120；`Trig_Victory_Cancel_Actions` 7131-7136；`Trig_Defeat_Cinematic_Actions` 7253-7334；`Trig_Defeat_Cancel_Actions` 7345-7351；跳过注册 7143/7357 | 胜负过场使用全体电影模式和全体对白，但镜头、跳过及最终结算集中于 `P1`。 |
| `END-01` | `Trig_Trigger_Victory_*` 6792-6817；胜利过场/跳过；`Trig_Next_Level_Run_Actions` | 计时到期且两个门禁为假时胜利；最终 `CustomVictoryBJ` 只给 `P1`。 |
| `END-02` | `Trig_Trigger_Defeat_Archimonde_Arrives_*` 7150-7182 | 仅当阿克蒙德进入 `DEFEAT` 区时置 `GameOver/ArchimondeHasWon` 并排队失败过场。 |
| `END-03` | `Trig_Trigger_Defeat_All_Buildings_Dead_*` 7188-7215 | 仅检查 `P1` 建筑全灭；`P0/P9` 基地失守不是直接失败条件。 |
| `END-04` | 英雄证据所列复活函数；胜利过场 7039/7044 | 脚本强制复活吉安娜、萨尔、三名阶段敌将，并可能在胜利过场复活泰兰德/玛法里奥；没有多人共享复活系统。 |
| `END-05` | `udg_GameOver`、`udg_ArchimondeHasWon` 读写点；胜负作弊注册 6770/6785 | 两个全局门禁防止重复结算；作弊入口和最终结果均写死 `P1`。 |
| `DIFF-01` | 简单 handicap 初始化 2322-2326；困难分支 | 简单降低 `P2/P3/P6/P10/P11` handicap；困难向阿纳塞隆添加 `ANrc`；无合作人数缩放。 |
| `META-01` | `Trig_Load_Tyrande/Furion_*`；地图初始化的游戏缓存入口 | 从 `Campaigns.w3v` 读取两名暗夜英雄；萨尔/吉安娜不从缓存恢复。 |
| `META-02` | `Trig_Save_Credits_Variable_Actions` 6742-6750；`Trig_Next_Level_Run_Actions` 6719-6727 | 困难存 `Credits/VictoryHard`，其他难度存 `Credits/Victory`；解锁暗夜结局，下一图为 `BonusCredits.w3m`。 |
| `COMPAT-01` | `wc3-inspect` 报告；`config`、`main` | 格式 18、JASS、编辑器 4448、推荐 1 人，固定玩家并使用自定义队伍/科技/升级。 |
| `EXTRA-01`、`EXTRA-03` | W3I 队伍、初始化联盟及全脚本资源/控制调用扫描 | 原版没有合作人数选择，也没有面向多真人的自动共享资源、普通控制或高级控制方案。 |

## 第 1 阶段结论

必查类别均有可复查锚点。`P0/P9` 在目标客户端的四类槽位生命周期与 AI 可运行性不是静态检查可以代替的结论，应由第 2 阶段确定目标与验收办法，并在第 6 阶段执行。本文形成时所有多人版取舍仍为“待决定”，尚未进入第 2 阶段。
