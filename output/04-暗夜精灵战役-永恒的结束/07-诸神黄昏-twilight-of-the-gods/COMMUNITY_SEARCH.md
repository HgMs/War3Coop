# 诸神黄昏：社区作品聚焦检索

状态：第 3 阶段已完成；发现一个近似候选，无可直接采用作品

检索日期：2026-09-09

投入时间：约 20 分钟，未超过 30 分钟默认上限

## 已冻结的筛选基线

- 支持 2-3 人；`P1` 暗夜精灵必须为真人，`P0` 兽族和 `P9` 人族各自支持活跃真人或大厅明确设置为电脑。
- 关闭、空槽和未加入不受支持；两份友方 AI 必须按槽位独立、一次性判定并完整保留或停用。
- 保留官方三段防线、45 分钟倒计时、任务、过场、基地失守和阶段推进，不按合作人数缩放玩法。
- 三方保持独立经济与控制；不自动共享资源或控制。
- 最低必须支持 1.24e-1.27b；地图必须可编辑、可审计，并有足以支持采用、修改和发布的许可。
- 英雄和跨关状态按 `ORIGINAL_COOP_MECHANICS.md` 已确认配置处理。

## 实际检索范围

主要社区来源：

- Hive Workshop Maps 与单图发布页。
- 实际检索式：`site:hiveworkshop.com "Twilight of the Gods" "2 Player Campaign" Warcraft III`
- 实际检索式：`site:hiveworkshop.com "Twilight of the Gods" "3P" campaign Warcraft III`
- 进一步核对 Hive 中 `Twilight Of The Gods v1.2`、`Twilight of the Gods 3.2` 及 TheSpoon 双人战役线索。

独立跨站搜索：

- 实际检索式：`Warcraft III Twilight of the Gods co-op map 2 player 3 player`
- 实际检索式：`NightElf07 cooperative campaign map Warcraft III`
- 实际检索式：`"Twilight Of The Gods v1.2.w3x"`
- 实际检索式：`site:epicwar.com "Twilight of the Gods" Warcraft 3 map`
- 实际检索式：`site:wc3maps.com "Twilight of the Gods" 3 player`
- 覆盖 WC3Maps、EpicWar、Reddit 讨论和普通跨站结果。

## 候选筛选

### 1. Twilight Of The Gods v1.2

- 作者：Ljarvisone；AI 协助：Turnro。
- 原始发布页：[Hive Workshop](https://www.hiveworkshop.com/threads/twilight-of-the-gods-v1-2.290376/)。
- 网页正向信号：作者声明尽量接近官方原关，支持 1-3 人、普通/困难、人类与兽族可玩、所有阵营可由 AI 控制；未占用槽必须在大厅改为电脑，否则 AI 不运行。
- 网页版本：推荐 1.24a-1.28c；2017-07-23 更新。
- 网页已知偏离：将左上金矿替换为市场；加入物品/符文；限制人类与兽族在基地外建造；为吉安娜和萨尔加入复活设施；作者仍称其为 Beta。
- 取得文件：`Twilight Of The Gods v1.2.w3x`，435042 字节，SHA-256 `BE98C21C6E5623A2C0EE452430012F74C687D16C31434C5CA5FF7DC5243D67C8`。文件仅保存在被 Git 忽略的 `third-party/`，未执行脚本。
- 只读解析：格式 25、地图版本 1269、编辑器 v6059、JASS；`P0/P1/P9` 分别为兽族/暗夜精灵/人族用户槽；793 个脚本函数、144 个 `udg_` 全局、184 个放置单位、4113 个装饰物、6 个导入文件；解析无警告。
- 相对官方原图的关键差异：候选有 347 个新增函数名和 161 个缺失函数名；新增按玩家人数调整敌方 handicap、塔楼、科技、额外基地和英雄经验的逻辑；三方初始资源改为 `P1=1000/800`、`P0=1200/1000`、`P9=1400/1200`；替换和扩充多份 AI；真人萨尔/吉安娜降为 3 级，电脑英雄升为 10 级；泰兰德新增物品；新增市场随机物品、英雄复活设施和额外资源补贴。
- 已有多人基础：按控制器决定是否启动三方 AI；三名玩家均收到胜负；多处镜头、求援和 ping 已扩展到三方。
- 仍不满足：控制模式判定未同时统一检查 controller 与 slot state；允许 `P1` 电脑而非要求真人；按人数改平衡；经济、英雄、物品、复活、AI 和场景均偏离已确认目标；未见资料片额外英雄限制；页面没有明确授予修改及再分发许可。
- 结论：最高优先级近似候选，但不是直接采用候选。技术上可审计；只有取得授权后才可作为修改底图路线。

### 2. Twilight of the Gods 3.2

- 作者：Forsaken；发布页：[Hive Workshop](https://www.hiveworkshop.com/threads/twilight-of-the-gods-3-2.225718/)。
- 正向信号：3 人、单人时电脑有 AI；网页推荐 1.19-1.21b。
- 排除依据：更新日志明确包含新地形、更多恶魔波次、半英雄改为完整英雄、新技能、新模型和大量其他改动，明显偏离官方还原目标；没有继续下载。

### 3. 2P Reforged: NightElf 07

- 作者线：TheSpoon 2 Player Campaign；索引页：[WC3Maps](https://wc3maps.com/map/305810)。
- 排除依据：仅双人，索引版本为 1.36.1，且该系列本身是双人重平衡；不满足 2-3 人弹性、三方原阵营映射和 1.24e-1.27b 硬条件。

### 4. Twilight of the Gods [Ver1.0]

- 作者：Blizzard & Red Orca；索引页：[EpicWar](https://www.epicwar.com/maps/52798/)。
- 正向信号：官方关卡多人版、3 人、版本 1.19-1.21b。
- 排除依据：页面明确标为 `3 [No AI]`，不满足两名可选友军槽的电脑回退，也不支持目标中的 2 人配置；没有下载。

## 结论与停止原因

本次聚焦检索未发现满足全部硬条件的直接采用作品。`Twilight Of The Gods v1.2` 是唯一同时命中关卡、1-3 人、三方阵营、经典版本和友军 AI 的近似候选，已完成必要的文件级只读核验，应进入第 4 阶段与官方原图比较。其他高曝光候选已被网页证据证明在人数、版本、AI 回退或原版还原度上明显偏离，因此停止扩大检索，也不下载它们凑比较对象。

重新检索触发条件：出现明确支持 2-3 人、1.24e-1.27b、三方原阵营独立控制与电脑回退的新候选，或 `Twilight Of The Gods v1.2` 的作者补充改编/再分发授权。
