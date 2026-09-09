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
| `SLOT-01` 至 `SLOT-04` | W3I；`InitGlobals` 419-450；`InitCustomPlayerSlots/Teams`；`Trig_Init_01_Players_Actions` 2298-2426 | `P1` User Naga，`P2` Computer Sentinel，`P3` Neutral Furion；运行期联盟与共享控制不完全等同于 W3I force。 |
| `SLOT-05`、`ALLYAI-03`、`EXTRA-03` | 全脚本负向扫描 | `GetPlayerController`、`GetPlayerSlotState`、`EVENT_PLAYER_LEAVE` 均未出现。 |
| `ASSET-01` | `CreateBuildingsForPlayer1` 1000；`CreateUnitsForPlayer1` 1020；尤迪安恢复 2860-2870 | `P1` 10 建筑、17 非英雄单位；尤迪安缓存或 10 级兜底；500/500。 |
| `ASSET-02` | `CreateBuildingsForPlayer3` 1076；`CreateUnitsForPlayer3` 1100；玛尔法里奥恢复 2816-2832 | `P3` 13 建筑、15 非英雄单位及玛尔法里奥；500/500。 |
| `ASSET-03` | `CreateBuildingsForPlayer2` 1047；`CreateUnitsForPlayer2` 1062；泰兰德恢复 2778-2788 | `P2` 5 建筑、4 非英雄单位及泰兰德；5000/5000。 |
| `ALLYAI-01`、`ALLYAI-02` | AI 启动 5361-5367 | `P2` 与敌方槽启动 `StartMeleeAI`；没有 `P3` AI。 |
| `QUEST-01` | 任务创建 5056；完成 5078-5105 | 主任务包含泰兰德、亡灵与玛尔法里奥基地条件。 |
| `QUEST-02` | 水坝任务 5129-5178；水坝过场与跳过 | 可选水坝任务有独立发现、世界状态改变与完成入口。 |
| `CINE-01`、`CINE-02` | 开场 3143-3693；水坝 3790-4241；胜利 4423-4987；跳过注册 3446/3896/4737 | 镜头和跳过均集中 `P1`，正常与跳过都会操作剧情资产。 |
| `END-01` 至 `END-03` | 胜利 4966-4987；失败 5000-5032、6045-6154 | 单一门禁；胜负只结算 `P1`；另有建筑/单位全灭及泰兰德死亡路径。 |
| `DIFF-01` | 难度初始化 2608-2711 | 三档难度修改敌方 handicap 与英雄/单位配置，无合作人数缩放。 |
| `META-01`、`META-02` | 英雄恢复 2778-2935；保存/解锁/下一图 4966-4987 | 依赖 `Campaigns.w3v`，胜利保存英雄并进入 `NightElfX08Finale.w3x`。 |

## 静态边界

- 官方原图中 `P3` 是完整友军但没有独立 AI；`P2` 有 AI 但不是完整常规基地。
- 基础 `UnitBalance.slk` 对所列剧情英雄的直接 `fused` 单元为空；地图自定义对象继承后的最终有效人口须在选定路线后结构化复算，本文不把空单元等同于已证明的 0。
- 真人、电脑、空槽和未加入的大厅生命周期，以及空槽 AI 可运行性，静态 JASS 无法证明。
