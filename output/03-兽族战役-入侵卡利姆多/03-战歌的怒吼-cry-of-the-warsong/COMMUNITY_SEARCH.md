# 战歌的怒吼：社区作品轻量检索

状态：第 3 阶段已完成；本次轻量检索未发现轻改候选

检索负责人：开发代理

检索日期：2026-09-09

实际投入时间：约 10 分钟，未超过 15 分钟上限

## 已确认的筛选基线

- 2 人独立阵营合作：P0 真人控制萨尔与兽人部落；P3 由玩家 2真人或正在游戏的电脑控制格罗姆与完整战歌基地；P2 保持隐藏剧情辅助电脑。
- P3 真人模式完整停用战歌 AI、阶段命令、补贴、AI 复活和自动反目；电脑模式完整保留官方 `o03_grom.ai` 及反目机制。
- 保留官方建镇、鹰身人、飞艇、四个人族基地、过场和 P0 归属门槛；双方共享任务反馈与结算，不增加双人缩放。
- 资源、人口、科技和控制独立；不默认共享资源或单位控制。
- 必须支持 1.24e-1.27b，1.31 尽力支持；经典 JASS/W3X、可编辑、可审计，且许可满足预定采用和发布方式。
- 完整目标与验收标准见 [ORIGINAL_COOP_MECHANICS.md](ORIGINAL_COOP_MECHANICS.md)。

## 主要社区来源：Hive Workshop

检查入口：[The 2 Player Campaign 专区](https://www.hiveworkshop.com/forums/the-2-player-campaign.665/) 与 Hive 地图发布页。

实际检索式：

- `site:hiveworkshop.com "Cry of the Warsong" co-op Warcraft III`
- `site:hiveworkshop.com Orc03 "2 Player Campaign"`
- `site:hiveworkshop.com/threads "2P Campaign: Orc 03"`
- `site:hiveworkshop.com "Chapter Three: Cry of the Warsong" "The-Spoon"`

关键命中：

- [2P Campaign: Orc 03 v1.4](https://www.hiveworkshop.com/threads/2p-campaign-orc-03.190463/)，作者标为 Gismo359、McQvaBlood、TheSpoon，推荐版本 1.19-1.21b，建议人数 2。
- 页面明确写有新增纳兹格雷尔、加入序章过场、为两名玩家调整基地、存档码、共享科技、固定困难及额外疯狂难度内容。
- 这些改动表明第二名玩家采用新增英雄和双基地重平衡路线，而不是直接接管官方 P3 格罗姆/战歌阵营；其跨关、难度、科技和关卡结构也与已确认目标冲突。
- 结论：精确同关且经典版本兼容是正向信号，但需要重做槽位/阵营分工、友方 AI、经济、难度和跨关机制，不属于轻改候选；不触发下载门。

其他 Hive 命中合并排除：

- [Grand Battles: Cry of the Warsong](https://www.hiveworkshop.com/threads/grand-battles.107945/) 是只控制英雄/少量部队、依靠触发器盟军争夺据点的战斗重混，核心玩法不符。
- [Warcraft III - Archon Mode](https://www.hiveworkshop.com/threads/warcraft-iii-archon-mode.298286/) 的 Orc03 是共享控制路线且推荐 1.31，不符合独立 P0/P3 阵营和最低版本要求。
- [The Invasion of Kalimdor Reworked](https://www.hiveworkshop.com/threads/the-invasion-of-kalimdor-reworked.360145/) 明确加入新英雄、随机化 AI 进攻并重述剧情，不是原版轻改合作版。

## 独立跨站搜索

实际检索式：

- `"Cry of the Warsong" "2 player" Warcraft III map`
- `"战歌的怒吼" 双人 魔兽争霸 地图`
- `site:epicwar.com/maps/ "Cry of the Warsong" coop`
- `site:wc3maps.com/map Orc03 co-op Grom Thrall`
- `"Orc03" coop map Grom player 2`
- `"2P Orc03" Grom Nazgrel`

检查结果：

- [WC3Maps 的 2P Campaign: Orc 03](https://wc3maps.com/map/3549) 和 [EpicWar 的同名条目](https://www.epicwar.com/maps/259632/) 均指向 Hive 同一 v1.4 作品，不形成独立候选。
- [EpicWar: Cry Of The Warsong](https://www.epicwar.com/maps/310854/) 标注作者未知、类别为 Melee、推荐玩家任意，没有合作人数、P0/P3 分工、任务和兼容范围证据，按规则直接排除。
- [EpicWar: Orc - Campaing Reign of Chaos 03](https://www.epicwar.com/maps/318057/) 推荐玩家 1，且没有双人阵营或 AI 回退说明，直接排除。
- 其余命中是普通战役讨论、AI 制作问答或无关地图，未达到轻改门槛。

## 下载核验

无。没有作品先由网页证据同时通过关卡、人数与阵营分工、核心玩法、兼容和局部差异门槛，因此未下载任何第三方文件。

## 结论与停止原因

结论：**本次轻量检索未发现轻改候选**。

Hive 主来源和一次独立跨站搜索均已完成。唯一精确同关双人作品 v1.4 需要结构性回滚和重做，其他结果则在玩法、人数、版本或来源证据上直接失败；按 15 分钟轻量检索规则停止，不继续追版本链、镜像或文件结构。

重新检索触发条件：第 2 阶段目标变化；出现明确支持 P0 萨尔与 P3 格罗姆独立阵营、P3 电脑完整 AI 回退且兼容 1.24e-1.27b 的具体新线索；或已有作品发布新的页面证据。
