# 驱逐恶魔：社区作品检索记录

- 状态：第 3 阶段完成；聚焦检索完成门已通过，未发现满足全部硬条件的可直接采用作品
- 检索负责人：开发代理
- 检索开始/结束日期：2026-09-07
- 最后复查：2026-09-07
- 实际投入时间：本轮执行时未单独计时；覆盖范围超过后续采用的默认 30 分钟时间盒
- 需求基线：[ORIGINAL_COOP_MECHANICS.md](ORIGINAL_COOP_MECHANICS.md)，已由项目负责人于 2026-09-07 确认
- 后续修订：2026-09-09 实机确认 1.24e 关闭用户槽后无法运行原版 Campaign AI；负责人接受“单人时必须把玩家 2设置为电脑、关闭模式不受支持”为发布限制。该修订放宽原检索硬条件，不改变既有候选均无法直接采用的结论。
- 记录范围：公开发布页、项目页、作者/系列页、搜索索引和两个近似候选的本地只读检查；第三方二进制仅放在 Git 忽略的 `third-party/`，未提交、未执行脚本

## 预先定义的硬条件

- 玩家 1必须为真人并保留原版完整兽人阵营（`SLOT-01`、`ASSET-01` 至 `ASSET-04`）；玩家 2接管原版 `Player(9)` 人类探险队（`SLOT-02`、`ASSET-05` 至 `ASSET-07`）。
- 搜索时的原始基线要求只在进入游戏时判断玩家 2：真人且正在游戏时停用人族原版 AI，电脑或空槽时沿用完整原版 AI；结果整局不变，不处理中途退出（`SLOT-06`、`SLOT-07`、`ALLYAI-01` 至 `ALLYAI-06`、`END-06`、`END-08`）。2026-09-09 实机和 API核对后，该要求已按上方“后续修订”收敛为电脑受支持、空槽不支持。
- 两名真人各自控制一个原版既有阵营，不采用 Archon 共享同一阵营，不新增第三合作阵营（`SLOT-03`、`SCALE-01`）。
- 除合作必需改动外保留原版地形、剧情、任务、敌军、波次和过场（`QUEST-*`、`EVENT-*`、`REWARD-*`、`CINE-*`）。
- 必须兼容 1.24e-1.27b，尽力兼容 1.31；保持格式 18 和 JASS（`META-04`）。
- 地图来源和作者可追溯，文件可编辑、脚本可审计，并有明确证据允许本项目修改和再分发。

## 来源与检索覆盖

下列“未命中”只对表中日期、入口和原样检索式负责。搜索结果会变化，底图决策超过 30 天时必须重新复查主要来源。

| 入口 | 原样检索式或筛选 | 覆盖范围与结果 |
| --- | --- | --- |
| Hive Maps 站内筛选 | `Tags: Campaign=Include, Co-op=Include`；`Match tags: All`；`Search: Orc08`；`Sort by: Relevance`；`Descending` | 实际提交筛选并检查全部结果页：共 1 页、1 个命中，仅为 Kilmaat `Warcraft III - Archon Mode`；其发布页明确是两名玩家共享同一阵营。 |
| Hive Maps | `site:hiveworkshop.com/threads ("By Demons Be Driven" OR "Orc 08" OR Orc08) (coop OR "co-op" OR "2P" OR "3P" OR multiplayer)` | 检查可见结果、资源页字段、评论、版本记录和关联条目；命中 Gismo359、Forsaken、Pyraeus、3P、8P、2020 2P 和 Archon 分支。 |
| Hive Projects | `site:hiveworkshop.com/projects (Orc08 OR "By Demons Be Driven" OR "Invasion of Kalimdor")` | 命中 `The Invasion of Kalimdor Reworked`、`Revamp` 和 2P/8P 项目；前两者为重制/增强且相关章节未完成或非本目标合作模式。 |
| Hive 作者/系列页 | `site:hiveworkshop.com ("2P Campaign: Orc 08" OR "By Demons Be Driven 2.2" OR Orc08) (Gismo359 OR Forsaken OR Horselord12)`；TheSpoon 作者资源页及 `2P Orc Campaign` 系列页 | 串联 Gismo v2.8 与旧版项目、Forsaken 2.2、TheSpoon 2020 分支；未发现作者对修改和再分发的明确授权。 |
| Hive 讨论/求助 | `site:hiveworkshop.com "By Demons Be Driven" "playing as Humans"` | 2023 求助帖确认 Gismo/2020 两人版在该关锁定为两个兽人玩家；另有完成的“人族可玩、兽人 AI”单人委托，不满足两名真人控制两个阵营。 |
| EpicWar | `site:epicwar.com/maps ("By Demons Be Driven" OR Orc08 OR "Orc 08")` | 命中 Gismo v2.8、Pyraeus/deflinok2 v0.8、Forsaken 镜像、2011 意大利语 1-2 人版、Horselord12 版本链和来源不明小文件。 |
| EpicWar 精确回溯 | `"Save Grom Hellscream.w3x"`；`"by demons by driver.w3x"`；`Gismo359 "2P Orc08"`；`Forsaken "By Demons Be Driven 2.2"` | 串联 Horselord12 的 `2P Orc08(lord)1.0/1.1`（2013）与 2022 条目；2011 文件只回到同一匿名发布页；Gismo 镜像与 Hive v2.8 一致。 |
| WC3Maps | `site:wc3maps.com/map ("By Demons Be Driven" OR Orc08)` | 命中 Kilmaat `AC Orc08`（1.29、JASS）和 Blizzard/Reforged 的原关版本；未发现符合经典版本与独立双阵营要求的新候选。 |
| WAR3 MAP ARCHIVE | 对 2026-08-15 索引的 `catalog/maps.jsonl` 全量 11,141 行检索 `By Demons Be Driven|Driven by Demons|Orc08|驱逐恶魔|驱逐魔鬼` | 仅命中 Gismo359 `2P Orc08 v2.1.w3x`，SHA-256 `A1C985D6...CF94B1E`；它是已知 Gismo 版本链，不是新独立作品。 |
| WC3Tracker | `site:wc3tracker.com "2P Orc08"`；`site:wc3tracker.com "By Demons Be Driven"` | 公开索引未返回相关条目；以 WC3Maps 和 WAR3 MAP ARCHIVE 作为独立补充索引。 |
| 中文网页 | `"驱逐恶魔" 魔兽争霸 双人 地图`；`"驱逐魔鬼" 魔兽争霸 双人 OR 合作 地图` | 命中攻略和非合作增强战役视频，未发现新的可追溯合作地图发布页。 |

本轮实际覆盖了 Hive Maps、Projects、作者/系列页，EpicWar、WC3Maps、WAR3 MAP ARCHIVE 三个独立补充索引，以及英文名、章节代号、中文译名、战役名和玩法词，超过现行聚焦检索的最低要求。这些覆盖作为已经取得的历史证据保留，不构成未来关卡必须照做的清单。

## 独立候选、版本链与排除证据

| 代号 | 独立作品 / 版本关系 | 页面或文件证据 | 硬条件结论 |
| --- | --- | --- | --- |
| C1 | Gismo359 / TheSpoon `2P Campaign: Orc 08`，Hive v2.8；EpicWar v2.8 镜像；WAR3 MAP ARCHIVE v2.1 旧版 | [Hive](https://www.hiveworkshop.com/threads/2p-campaign-orc-08.253195/)列出纳兹戈林、计分、强化战歌 AI、交换系统和疯狂难度；[2023 讨论](https://www.hiveworkshop.com/threads/help-request-by-demons-be-driven-playing-as-humans-jaina.347639/)实测反馈该关两个玩家只能选兽人；推荐版本 1.19-1.21b；未见修改/再分发许可 | `SLOT-02`、`SCALE-01`、原版还原和许可失败；1.24e-1.27b 未验证；不下载即可排除直接采用。 |
| C2 | Forsaken `By Demons Be Driven 2.2` | [Hive](https://www.hiveworkshop.com/threads/by-demons-be-driven-2-2.226900/)列出兽人与人类探险队，但增加 Lilatha Swiftarrow、前置新任务和 Jaina 新皮肤；推荐 1.24a-1.28c；作者在同帖认为改编他人项目需要许可，却未对本作给出许可，且部分素材来源不清 | `ASSET-07`、`QUEST-01`、`SCALE-01`、原版还原和许可失败。 |
| C3 | Pyraeus `By Demons Be Driven` | [Hive](https://www.hiveworkshop.com/threads/by-demons-be-driven.232496/)列出 3 个玩家、新军队/英雄/任务/Boss/金矿/地形/对象和难度重做 | 两人槽位、`SCALE-01`、任务/地形还原和许可失败。 |
| C4 | Pyraeus / deflinok2 `3P Campaign: Orc 08`，EpicWar v0.8；Hive 当前包 v0.9 | [EpicWar](https://www.epicwar.com/maps/293414/)为 3 人且声明 1.30；Hive 当前版推荐 1.31 | 两人槽位、`META-04` 和许可失败。 |
| C5 | Horselord12 `Orc08`，EpicWar `2P Orc08(lord)1.0/1.1`（2013）至 `by demons by driver.w3x`（2022） | [EpicWar](https://www.epicwar.com/maps/319704/)标称 1-5 人；只读报告 [community-horselord12-2022.inspection.json](workspace/community-horselord12-2022.inspection.json) 显示 7 个用户槽、120x148 地图、738 个放置单位、10 个导入文件，SHA-256 `B757C514227AB82536435C1AF9A30196BFBACF121F060BF2977FF68FA7C27CC1` | 两人槽位、`SCALE-01`、`META-04`、原版还原和许可失败。 |
| C6 | 匿名 `Chapter Eight: Driven by Demons` / `Save Grom Hellscream.w3x`（2011） | [EpicWar](https://www.epicwar.com/maps/159616/)只署名 `Blizzard Entertainment(multy)`；只读报告 [community-save-grom-2011.inspection.json](workspace/community-save-grom-2011.inspection.json) 显示 `Player(0)` 兽人和 `Player(9)` 人族两个用户槽，SHA-256 `E685BC285F1B80FED5A8D06C557B74D7DAC201A573959801AB6AB41E36EA8053`。脚本没有控制器/槽位判定，两条开场路径都无条件 `StartCampaignAI(Player(9), "o08_Jaina.ai")`，且双方初始化为 2000 金/2000 木 | `SLOT-06`、`ASSET-04`、`ECON-01`、`ALLYAI-01`、`CINE-03/04`、`END-04`、`META-04` 和来源/许可失败。 |
| C7 | Kilmaat `Warcraft III Archon Mode` / `AC Orc08` | [Hive](https://www.hiveworkshop.com/threads/warcraft-iii-archon-mode.298286/)明确为“两名玩家控制同一支部队”；[WC3Maps](https://wc3maps.com/map/36755)记录游戏版本 1.29、JASS | 独立双阵营和 `META-04` 失败。 |
| C8 | Ryan `8-Player Co-op Campaign: The Invasion of Kalimdor` / `8p_orc08 v106.w3m` | [Hive](https://www.hiveworkshop.com/threads/8-player-co-op-campaign-the-invasion-of-kalimdor-roc-orc.339159/)记录 8 玩家、Reforged、推荐 1.36 | 两人槽位、`META-04` 和原版规模失败。 |
| C9 | TheSpoon `2 Player Campaign 2020 - HD & SD` | [Hive](https://www.hiveworkshop.com/threads/2-player-campaign-2020-hd-sd.322390/)明确每个基地关增加第二基地、加入难度/英雄/法术/存档系统，当前版不兼容 1.32 及以下；2023 讨论还记录 Orc08 两槽锁定兽人 | `SLOT-02`、`SCALE-01`、`META-04`、原版还原和许可失败。 |
| C10 | EpicWar 多个 `ByDemonsBeDriven.w3x` 小文件 | 作者为 Unknown、分类/说明互相冲突，无法回溯到可确认作者和首发页 | 来源与许可硬条件失败；视为无法归并的镜像，不作为独立可采用作品。 |

`The Invasion of Kalimdor Reworked`、`Revamp` 和 2023“只玩人族、兽人交给 AI”的委托在发布说明层面已经证明不是本关的两人双阵营目标，因此只作为排除线索，不计入十个独立候选。

## 稳定编号核验说明

上表列出的编号为已有肯定证据的失败项；某候选未列出的稳定编号均记为“未知”，不是“满足”。一项硬条件失败即足以排除直接采用，工作流不要求为明显偏离的作品下载文件来把其余 63 项逐一反编译。C6 是唯一同时满足 `SLOT-01`、`SLOT-02`、`SLOT-04`、`SLOT-05` 的近似候选，但已由文件证据证明其失败项；C5 的文件检查是流程收敛前已经完成的历史证据，按现行规则不会仅因网页信息不足而下载。其余候选均已有网页级硬失败证据。

完整稳定编号集合为 `SLOT-01..08`、`ASSET-01..07`、`ECON-01..03`、`TECH-01..02`、`SCALE-01`、`ALLYAI-01..07`、`QUEST-01..09`、`EVENT-01..04`、`REWARD-01..02`、`CINE-01..08`、`END-01..08`、`META-01..04`。没有任何候选达到“全部编号满足”。

## 下载与只读检查记录

| 本地文件 | 结果 |
| --- | --- |
| `third-party/Save-Grom-Hellscream-2011.w3x` | 278,529 字节；SHA-256 `E685BC...A8053`；有效 MPQ/W3X；只读解析无警告。 |
| `third-party/Horselord12-Orc08-2022.w3x` | 415,273 字节；SHA-256 `B757C5...27CC1`；有效 MPQ/W3X；只读解析无警告。 |
| `third-party/Gismo359-2P-Orc08-v2.8.w3x` | 取得到的是 HTML 响应而非地图；SHA-256 `1BE37A...9DA30`；没有 MPQ 头，不作为地图证据。 |
| `third-party/Forsaken-By-Demons-Be-Driven-2.2.w3x` | 取得到的是 HTML 响应而非地图；SHA-256 `817341...CC45B`；没有 MPQ 头，不作为地图证据。 |

前两份地图只用于本地静态审计；提取的 JASS 也留在被忽略目录，不执行、不提交。后两份失败响应保留用于证明取得失败，不把网页伪装成地图。

## 第 3 阶段结论

截至 2026-09-07，在上述 Hive Maps/Projects/作者与系列页、EpicWar、WC3Maps、WC3Tracker、WAR3 MAP ARCHIVE 全量元数据以及中英文关键词范围内，发现 10 个独立候选或无法归并的候选组，但没有作品同时满足已确认的两人阵营映射、开局 AI 回退、原版还原、1.24e-1.27b、可审计性和修改/再分发许可硬条件。

- 未解决的可能合格候选：0。
- 可直接采用作品：0。
- 结论：聚焦检索完成门通过，进入第 4 阶段成本比较；不要求继续搜索或下载其他候选。
- 时效边界：若在 2026-10-07 之后才确认底图路线，或期间出现新候选/授权变化，必须回到第 3 阶段复查。
