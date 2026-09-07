# 社区合作战役来源与候选

最后扫描：2026-09-07

本文记录制作前用于发现、筛选和追踪 Warcraft III 社区合作战役的网站、检索方法与首批候选。结论来自公开网页初筛，尚不代表地图文件已经下载、获准改编或通过本项目的 `1.20e` / `1.31` 实机验证。

## 总体结论

1. [Hive Workshop](https://www.hiveworkshop.com/repositories/maps.564/) 应作为主索引和首发页优先来源。它能同时提供作者、版本要求、审核状态、更新日期、评论、依赖资源和地图文件。
2. [EpicWar](https://beta.epicwar.com/maps/) 与 [WC3Maps](https://www.wc3maps.com/) 适合补漏和寻找历史版本，但不能单独用于判断质量、授权或最新版。
3. [WC3Tracker](https://wc3tracker.com/) 适合验证地图是否仍在 Battle.net 被开房，不能替代内容和兼容性评测。
4. Blizzard Forums、Reddit、Discord 适合发现作者发布帖和当前补丁问题；最终仍应回到作者页或结构化地图条目核验。
5. 中文下载站主要用于寻找中文名、汉化版和老图线索。转载链、版本、下载器和授权经常不清晰，不应直接作为底图来源。
6. 原生合作、共享控制（Archon）与多人单关重混是三种不同产品形态，评估时必须分开。

## 站点优先级

| 优先级 | 站点 | 适用场景 | 局限与风险 |
| --- | --- | --- | --- |
| A | [Hive Workshop - Maps](https://www.hiveworkshop.com/repositories/maps.564/) | 建立候选池、下载、核对作者、版本、审核、评论与依赖 | 标签由上传者维护；审核徽章和推荐版本不等于满足本项目经典兼容要求 |
| A | [Hive Workshop - Projects](https://www.hiveworkshop.com/projects/browse/) | 追踪仍在开发的系列、团队、新闻和工具 | 项目页和正式地图条目可能分离，需要两边核对 |
| A- | [EpicWar](https://beta.epicwar.com/maps/) | 按标题、作者、人数或版本补漏 | 同图多版本重复、变更记录弱、无系统质量审核；首个搜索结果未必是最新版 |
| B+ | [WC3Maps](https://www.wc3maps.com/) | 搜索 Battle.net 地图、观察热门图和实时房间 | 对完整 `.w3n` 帮助较小；作者维护和评论链弱于 Hive |
| B+ | [WC3Tracker](https://wc3tracker.com/) | 验证当前活跃度、区域和版本族 | 私房、LAN、离线游玩会被低估；活跃度不代表质量 |
| B | [Blizzard Custom Games Forum](https://us.forums.blizzard.com/en/warcraft3/c/custom-games/14) | 查作者发布、补丁回归与玩家反馈 | 不是结构化地图库，版本管理有限 |
| B | [r/warcraft3](https://www.reddit.com/r/warcraft3/) | 查推荐、安装经验和冷门项目 | 信息口径不一，下载链接可能过期 |
| B- | [Campaign Creations](https://campaigncreations.org/warcraft) | 寻找经典剧情战役，作为参考或转换候选 | 主要是单人 `.w3n`，不少作品针对 1.26-1.31 |
| B- | [WAR3 MAP ARCHIVE](https://war3-archive.github.io/war3-maps/) | 中文老图保存、校验和单图下载 | 页面于 2026-08-15 标示 11,141 张地图、423 个战役包，但作者、许可和原始版本仍需回查 |
| C | [3DM 示例](https://dl.3dmgame.com/patch/181263.html)、[游侠网示例](https://patch.ali213.net/showpatch/295873.html)、[魔兽基地](https://www.ra216.com/) | 搜索中文名、汉化名和老图 | 多为二次转载，可能引导旧平台、补丁、破解或专用下载器；只记录线索，不直接引入 |
| 观察 | [War3Maps.net](https://war3maps.net/) | 未来可能形成中英双语作者社区 | 2026 年页面仍把分类、评论、评分等列为待建设功能 |

[WC3.ninja](https://wc3.ninja/about) 会持续抓取 Hive、EpicWar 和 WC3Maps，并解析地图内的模型、贴图、声音和对象数据。它适合资产来源调查，不是合作战役下载和质量筛选的主入口。

## 推荐检索方法

### Hive 主检索

在 Maps 页同时选择 `Campaign` 和 `Co-op`，启用 `Results must include all tags`，依次查看：

1. `Most downloaded` / `Highest rating`：寻找公认基准。
2. `Last updated`：寻找仍在维护的项目。
3. `Most comments`：寻找兼容性和 bug 讨论充分的项目。

不要只勾 `Only campaigns`。社区多人合作战役通常是一组可创建多人房间的 `.w3x` 地图，而不是单人“自定义战役”菜单使用的 `.w3n`；只看 Campaign 文件会漏掉关键候选。

建议轮换关键词：

- `2P Campaign`、`3P Campaign`
- `Co-op Campaign`、`Co-op Mission`
- `Archon Mode`、`Reforged Campaign`
- 章节代号：`Human05`、`BElf05`、`Rexxar01`
- 作者：`TheSpoon`、`Kilmaat`、`Zenonoth`、`Xetanth87`

### 补漏与交叉验证

1. 将 Hive 候选的精确标题、作者和文件名放到 EpicWar、WC3Maps 搜索。
2. 检查更新版、历史版、镜像、汉化版和缺失章节。
3. 在 WC3Maps Live 和 WC3Tracker 搜标题稳定词干，观察近 30 天开房、区域和常见版本。
4. 搜索 `标题 + desync/crash/bug/patch`，查看 Hive 评论、Blizzard Forums 和 Reddit。
5. 中文站只用于发现译名和老版本；下载前回查首发页，下载后计算 SHA-256 并离线检查。

## 首批候选

### 2 Player Campaign 2020 - HD & SD

- 发布页：[Hive 条目](https://www.hiveworkshop.com/threads/2-player-campaign-2020-hd-sd.322390/)
- 类型：官方主战役的双人独立阵营改编。
- 网页状态：Hive `Approved` + `Recommended`；57 个资源；条目更新于 2024-02-27；各地图推荐版本主要为 1.34-1.36。
- 可参考内容：双基地、收入拆分、额外英雄、三档难度、跨关存档、过场与任务多人化。
- 本项目判断：最重要的直接竞品。新版明确不支持 1.32 以下；旧项目虽仍可下载，但已停止维护，只能作为候选，不能直接采用。
- 风险：[2026 年评论](https://www.hiveworkshop.com/threads/2-player-campaign-2020-hd-sd.322390/page-3) 报告部分关卡多人掉线/反同步，以及补丁造成 Campaign AI 维修行为变化。

### 3P Campaign: Sentinel Pack v1-v4

- 发布页：[Hive 条目](https://www.hiveworkshop.com/threads/3p-campaign-sentinel-pack-v1-v2-v3-and-v4.354347/)
- 类型：《冰封王座》哨兵战役三人改编。
- 网页状态：2026-09-03 更新；整包含多代版本；作者声明支持 1.29-1.36/Reforged；Hive 标记 `Useful/Simple`。
- 可参考内容：三名玩家的英雄、基地和兵种分工，以及多代版本维护。
- 本项目判断：最低声明版本为 1.29，不满足 1.20e-1.27b；适合设计参考，是否存在可回退旧底图需下载检查。

### Warcraft III - Archon Mode

- 发布页：[Hive 条目](https://www.hiveworkshop.com/threads/warcraft-iii-archon-mode.298286/)
- 类型：两名玩家共享控制同一支部队。
- 网页状态：62 个资源；推荐 1.31；最后更新 2019；Hive `Approved` + `Recommended`。
- 可参考内容：低改造成本的多人控制、难度、跨关存档码和多人跳过过场。
- 本项目判断：适合作为共享控制对照组，不与独立阵营合作版合并评分；经典兼容范围仍需实测。

### Times Change (SD & HD)

- 发布页：[Hive 条目](https://www.hiveworkshop.com/threads/times-change-sd-hd-co-op-campaign.370604/)
- 类型：原创双人剧情战役。
- 网页状态：2026 年创建并更新；Hive `Approved` + `Recommended`；作者声明 SD 支持 1.26a+、HD 支持最新 Reforged；计划 14 章，仍在扩充。
- 本项目判断：仅覆盖兼容区间上端且不是原版还原，主要作为双人角色和产品体验参考。

### 3Player Prologue campaign

- 发布页：[Hive 条目](https://www.hiveworkshop.com/threads/3player-prologue-campaign.352387/)
- 类型：兽族序章三人改编。
- 网页状态：可见章节推荐 1.36，2024 年更新；玩家反馈提到存档、难度和主题化兵种分配。
- 本项目判断：版本高于兼容基线，只作三人职能设计参考。

### Dani 的 Co-op Missions

- 发布页：[Blizzard Forums](https://us.forums.blizzard.com/en/warcraft3/t/co-op-missions/36381)
- 类型：将官方单关重混为 6-19 人公共房 PvE。
- 网页状态：2026-08 可见 `Human05 v10.9`、`BElf05 v6.2`、`Rexxar01 v4.9`，文件发布在 EpicWar。
- 可参考内容：共享升级、Boss、事件、随机装备和大人数节奏。
- 本项目判断：不是连续战役且偏离原版较多；适合机制参考，不优先作为还原型底图。
- 风险：作者指出 EpicWar 旧版堆积且难以删除，部分旧版优化差并可能崩溃。

### Campaign Splitter - CO-OP Custom Campaigns

- 项目页：[Hive Project](https://www.hiveworkshop.com/projects/campaign-splitter-co-op-custom-campaigns.93/)
- 类型：将 `.w3n` 拆成独立地图并注入 Archon Mode。
- 能力：合并对象数据、导入、字符串和界面，重建多人存档，添加难度选择器。
- 本项目判断：Archon Mode 明确依赖 Reforged，不能用于成品；可参考战役数据合并和存档设计。
- 风险：次要玩家共享主玩家单位、资源和人口，并非经过合作平衡的原生作品；分支路线仍需房主手动开下一图。

## 文件形态与合作类型

- `.w3n` 是自定义战役包，可包含多个地图和共享战役数据，传统上从单人菜单进入。
- `.w3x` / `.w3m` 是可单独创建多人房间的地图。社区合作战役通常逐章发布，再用存档码、`CustomMapData` 或自定义持久化传递英雄、物品和难度。
- **独立阵营型**：每名玩家有自己的英雄、单位、基地或资源，最接近本项目目标。
- **共享控制/Archon 型**：多人控制同一阵营并共享资源、人口和单位，主要解决“能一起玩”。
- **多人单关重混型**：强调 Boss、事件、掉落和公共升级，通常没有连续跨章结构。

## 评估注意事项

- 将“条目推荐版本”“最后更新时间”“作者声明版本”“本项目实测版本”分栏记录。
- 旧图能加载不等于主线、过场、AI 和跨关数据可完成；新版地图使用经典画面也不代表兼容经典客户端。
- SD 与 HD 分开测试。社区已有 HD 崩溃而 SD 可完成、以及本地化文本或地图名异常的报告。
- Hive 审核徽章不保证当前补丁兼容，也不代表许可允许修改或再分发。
- 下载量、房间活跃度和标签都只能作为先验；最终以 `war3map.w3i`、脚本、依赖和双机实测为准。

## 候选文件取得后补充记录

除 [底图来源评估流程](SOURCE_EVALUATION.md) 已定义字段外，再记录：

```text
作品类型：独立阵营 / Archon / 多人单关
地图或章节数：
完成度与单章时长：
SD / HD：
是否允许少于标称人数开局：
跨关数据：英雄等级、属性、物品、资源、难度、分支
合作设计：角色分工、独立经济、共享科技、复活、离开处理
稳定性：开局、过场、存读档、同步、长局运行、通关跳转
外部依赖与资源许可：
最近阻塞问题及其复现步骤：
```

## 建议评测顺序

1. `2 Player Campaign 2020`：完整官方战役改编基准，重点寻找可用旧项目地图。
2. `Warcraft III - Archon Mode`：共享控制和低改造成本对照。
3. `3P Sentinel Pack`：三人独立分工对照。
4. `Times Change`：原创双人战役体验对照。
5. Dani 的 Co-op Missions：大人数公共房 PvE 对照。
6. Campaign Splitter：只评估数据合并与存档思路。

首轮下载与实检应围绕项目第一批 9 关检索对应章节，尤其是 `Orc03`、`Orc08`、`NightElf07`、`Sentinel05/07/08`、`BloodElf06`、`UndeadX01/05`，避免被整套战役完整度掩盖单关质量。

## 来源核对记录

- [Hive 地图筛选器](https://www.hiveworkshop.com/repositories/maps.564/)：Campaign、Co-op、人数、SD/HD、版本、审核状态、更新时间和下载量等筛选维度。
- [The 2 Player Campaign 专区](https://www.hiveworkshop.com/forums/the-2-player-campaign.665/)：项目结构、旧版下载和长期 bug 讨论。
- [2 Player Campaign 2020](https://www.hiveworkshop.com/threads/2-player-campaign-2020-hd-sd.322390/)：条目状态、内容数量、玩法改动和逐图版本元数据。
- [2 Player Campaign 2020 第 3 页](https://www.hiveworkshop.com/threads/2-player-campaign-2020-hd-sd.322390/page-3)：2026 年 Campaign AI 和多人反同步报告。
- [3P Sentinel Pack](https://www.hiveworkshop.com/threads/3p-campaign-sentinel-pack-v1-v2-v3-and-v4.354347/)：三人战役、整包版本和地图元数据。
- [Times Change](https://www.hiveworkshop.com/threads/times-change-sd-hd-co-op-campaign.370604/)：2026 年原创合作战役、SD/HD 与计划章节信息。
- [Warcraft III - Archon Mode](https://www.hiveworkshop.com/threads/warcraft-iii-archon-mode.298286/)：共享控制、存档码、难度和版本信息。
- [Dani 发布帖](https://us.forums.blizzard.com/en/warcraft3/t/co-op-missions/36381)：大人数合作任务版本与 EpicWar 版本管理问题。
- [WC3Tracker FAQ](https://wc3tracker.com/faq)：数据来源、区域覆盖、实时更新和版本归并方式。
- [Campaign Splitter 项目页](https://www.hiveworkshop.com/projects/campaign-splitter-co-op-custom-campaigns.93/)：`.w3n` 拆分、战役数据合并、多人存档和 Archon Mode 的能力与限制。
- [Campaign Creations](https://campaigncreations.org/warcraft)：经典单人剧情战役策展入口。
- [WAR3 MAP ARCHIVE](https://war3-archive.github.io/war3-maps/)：中文地图保存库的规模、分类、校验与数据集入口。
