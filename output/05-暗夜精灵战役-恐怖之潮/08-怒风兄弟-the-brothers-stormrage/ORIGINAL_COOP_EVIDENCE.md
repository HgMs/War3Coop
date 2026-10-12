# 怒风兄弟：官方原版证据索引

本文件是 `ORIGINAL_COOP_MECHANICS.md` 的第 1 阶段证据附件。只记录可复查的元数据、脚本符号和静态结论，不提交官方地图或完整 JASS。

## 来源与完整性

- 游戏：Warcraft III 1.24e，内部版本 `1.24.4.6387`
- `Game.dll` SHA-256：`5F7D8B25005BC12F8453839474EB37ADE8869ABA0570EAD2B76B6A7979C904CC`
- 来源：`game/Warcraft3-1.24e/War3Patch.mpq`
- 条目：`Maps\FrozenThrone\Campaign\NightElfX08.w3x`
- 地图：282331 字节，SHA-256 `AC942B38F7E7F4C1EDA45DE0512667FAE91E5CD36D902FCBE1C1BBFB96B363E0`
- 格式 25；地图版本 739；编辑器 v6031；JASS；116×84；推荐玩家 1
- 静态规模：107 条触发字符串、352401 字节脚本、480 个函数、67 个 `udg_` 全局量、292 个放置单位、3843 个装饰物
- `war3map.w3u`：2769 字节，SHA-256 `9EBBA8195A2B076B32A984F296B8F0C751AA3131C881926C23965653C5C4E9C7`
- 检查器警告：无
- 检查日期：2026-09-09；未执行 JASS，未启动游戏或 World Editor

复查命令：

```powershell
& '.agents/skills/warcraft3-campaign-inspector/scripts/inspect.ps1' game version 'game/Warcraft3-1.24e'
& '.agents/skills/warcraft3-campaign-inspector/scripts/inspect.ps1' game inspect 'game/Warcraft3-1.24e' --entry 'Maps\FrozenThrone\Campaign\NightElfX08.w3x'
```

## 证据映射

| 机制 | 脚本/对象锚点 | 观察结果 |
| --- | --- | --- |
| `SLOT-01` 至 `SLOT-05` | W3I；`InitGlobals` 419-450；`InitCustomPlayerSlots/Teams` 6480-6603；`Trig_Init_01_Players_Actions` 2298-2447 | JASS `P1` User Naga，`P2` Computer Sentinel，`P3` Neutral Furion。W3I 将三者置于同队并打开共同胜利、共享视野/控制；运行时又将 `P1/P3` 设为双向 `bj_ALLIANCE_ALLIED_ADVUNITS`，而 P2 关系并不完全对称。 |
| `SLOT-05`、`ALLYAI-03`、`EXTRA-03` | 全脚本负向扫描 | `GetPlayerController`、`GetPlayerSlotState`、`EVENT_PLAYER_LEAVE` 均未出现。 |
| `ASSET-01`、`HERO-01` | `CreateBuildingsForPlayer1` 1000；`CreateUnitsForPlayer1` 1020；`Trig_Load_Illidan_Actions` 2858-2881；初始化 2487 | `P1` 10 建筑、17 非英雄单位；尤迪安 `Eevi` 缓存或 10 级兜底，技能 `AEmb/AEim/AEev` 各 3 级、`AEvi` 1 级，并取得 `IllidansOrb` 区域的预置 `gldo`；500/500。 |
| `ASSET-02`、`HERO-02` | `CreateBuildingsForPlayer3` 1076；`CreateUnitsForPlayer3` 1100；`Trig_Load_Furion_Actions` 2814-2843 | `P3` 13 建筑、15 非英雄单位；玛尔法里奥 `Emfr` 缓存或 10 级兜底，缓存路径先移除 6 个 `stwp`；500/500。 |
| `ASSET-03`、`HERO-03` | `CreateBuildingsForPlayer2` 1047；`CreateUnitsForPlayer2` 1062；`Trig_Load_Tyrande_Actions` 2776-2799 | `P2` 5 建筑、4 非英雄单位；泰兰德 `Etyr` 缓存或 10 级兜底；5000/5000。 |
| `HERO-04` | `Trig_Load_Magroth_Actions` 2899-2912；`Trig_Load_Maiev_Actions` 2924-2937 | 两名角色只用于从 `FinalX` 恢复并把 6 格物品放到固定点，随后立即移除；马格罗斯入口另受 `MagrothSaved` 控制。 |
| `HERO-05` | 放置英雄 1155-1420；难度 2614-2647；终场复活 4898-4909 | 预置敌方英雄为 `Udea/Udre/Ulic` 各 2 个；尤迪安和玛尔法里奥可能在终场被剧情帮助器复活。 |
| `HERO-FOOD-01` | `War3Patch.mpq::Units\\UnitBalance.slk`（SHA-256 `1DC1A308826E85C03D963EA3BE750E771153BB31BC058F9D45B8F3E6F0680F27`）；地图 `war3map.w3u` | `Eevi/Emfr/Etyr/Ewrd/Hmgd` 的 `fused` 为空（有效 0）；`Udea/Udre/Ulic fused=5`；地图没有上述八种类型的 `ufoo` 覆盖。 |
| `ALLYAI-01`、`ALLYAI-02` | AI 启动 5361-5367；`N08X03.ai`；P2 资源事件 5379-5401 | `P2` 与敌方槽启动 `StartMeleeAI`；P2 AI 维持固定防守建筑/单位并允许修理。P2 金/木低于 1000 时分别设为 2109/3243；没有 `P3` AI 或补贴。 |
| `QUEST-01` | 任务创建 5053-5062；发现/完成 5074-5108；胜利条件 6029-6056 | 主任务有“消灭亡灵”和“玛尔法里奥基地存活”两项要求；`P4` 活建筑归零触发唯一胜利门禁。 |
| `QUEST-02` | 水坝任务 5126-5213；发现入口 5575-5649；完成 5655-5666 | 区域入口已接受 P1 或 P3 单位，但周期可见性入口只检查 P1；巨魔组全部死亡后只推进一次水坝过场与任务完成。 |
| `CINE-01`、`CINE-02` | 开场 3143-3693；水坝 3790-4241；胜利 4423-4987；跳过注册 3446/3896/4737 | 镜头和跳过均集中 `P1`，正常与跳过都会操作剧情资产。 |
| `END-01` 至 `END-03` | 胜利 6041-6056、4962-4987；失败 6079-6154；电影复活 2954-3014 | P4 活建筑归零后胜利；P1 或 P3 任一建筑归零、或泰兰德死亡后失败；胜负只显式结算 P1。终场帮助器会复活尤迪安/玛尔法里奥并寻找所属阵营祭坛。 |
| `DIFF-01` | 难度初始化 2607-2723 | 困难提高六个亡灵英雄等级/技能；普通给玛尔法里奥的金矿增加 3000 金；简单另降低敌方与中立敌对 handicap。无合作人数缩放。 |
| `META-01`、`META-02` | 英雄恢复 2778-2935；保存/解锁/下一图 4966-4987 | 依赖 `Campaigns.w3v`，胜利保存英雄并进入 `NightElfX08Finale.w3x`。 |

## 静态边界

- 官方原图中 `P3` 是完整友军但没有独立 AI；`P2` 有 AI 但不是完整常规基地。
- 可达英雄已按脚本创建、缓存恢复、预置实例和终场复活路径枚举。`Eevi/Emfr/Etyr/Ewrd/Hmgd` 有效人口为 0，`Udea/Udre/Ulic` 为 5；地图对象数据没有覆盖这些类型的 `ufoo`。
- 真人、电脑、空槽和未加入的大厅生命周期，以及 P3 非真人时恢复 Neutral 共享控制的实际可用性，静态 JASS 无法证明。
