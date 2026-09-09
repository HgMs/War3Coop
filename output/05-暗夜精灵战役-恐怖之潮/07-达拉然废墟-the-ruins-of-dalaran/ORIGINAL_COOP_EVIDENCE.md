# 《达拉然废墟》官方原版证据索引

## 证据边界

- 本文是 `ORIGINAL_COOP_MECHANICS.md` 的可提交、可复查证据索引。
- 证据只来自 `game/Warcraft3-1.24e` 官方安装与归档的只读解析；未执行 JASS，未启动游戏客户端。
- 本关没有提交官方 W3X/MPQ 二进制文件。

## 来源与完整性

| 对象 | 来源 / 标识 | SHA-256 / 结果 |
|---|---|---|
| 游戏版本 | `game/Warcraft3-1.24e` | `1.24e`, build `1.24.4.6387` |
| `Game.dll` | `game/Warcraft3-1.24e/Game.dll` | `5F7D8B25005BC12F8453839474EB37ADE8869ABA0570EAD2B76B6A7979C904CC` |
| 地图 entry | `War3Patch.mpq::Maps\\FrozenThrone\\Campaign\\NightElfX07.w3x` | `D91802ECDCA983CFA59DADFFA272B6BDA678F188CBE78067834E3FF27372926B` |
| `war3map.j` | 上述 entry 内 | `05AB3952BDBF6D7175B2030565C396F9EDC17AA8150B0AF6BB12D9616F92C068` |
| `war3map.w3u` | 上述 entry 内，2330 bytes | `EFB4C561E4B70FC599EE3B7291246515A4DE1F796C3941D37F2690B4573EE69D` |
| `war3map.wts` | 上述 entry 内 | `8D98E93BA4AA9590AED291F98E76C45477BA2B622B3E4A22897D1D00598ACE98` |
| 格式检查 | W3X format 25; map version 1151; editor 6031; JASS | warnings `[]` |
| 触发统计 | 128 trigger strings; 335147 script bytes; 444 functions; 90 `udg_` globals | inspector report |
| 场景统计 | 279 placed units; 3098 doodads; 6 special doodads; imports absent | inspector report |

## 机制证据映射

| 机制 ID | 可复查入口 | 关键参数 / 计数 |
|---|---|---|
| SLOT-01 | `InitCustomPlayerSlots`, `config`, W3I player table | `SetPlayers(7)`；P0 computer Human；P1 user NightElf；P2/P3/P4 computer Orc；P5/P6 computer Undead |
| SLOT-02 | `InitCustomTeams` | P0/P1 allied + shared vision；无 shared control/resources/common victory flag |
| SLOT-03 | `InitCustomTeams`, 运行时 alliance 初始化 | P2/P3/P4 allied + shared vision；P5/P6 custom team 仅 shared vision |
| ASSET-01 | `CreateBuildingsForPlayer0` | 19: `halt`1,`hars`1,`hbla`1,`hcas`1,`hgra`1,`hgtw`4,`hhou`8,`hlum`1,`hvlt`1 |
| ASSET-02 | `CreateUnitsForPlayer0`, Kael 创建函数 | 16 nonhero: `hdhw`3,`hhes`2,`hmpr`2,`hsor`1,`hspt`1,`nhea`1,`nhew`6；`Hkal` level 2 |
| ASSET-03 | 资源初始化，P0 资源维护触发 | initial 1000/1000; gold<1000 => +1351; lumber<1000 => +1273 |
| ASSET-04 | `InitTechTree` | P0 unavailable `Hpal/Hblm/Hamg/Hmkg`；无 P0 `SetPlayerTechResearched` |
| ASSET-05 | `CreateBuildingsForPlayer1`, `Trig_Intro_Cleanup_Actions` | 14 buildings；过场后 replace six uprooted ancients with same rawcodes |
| ASSET-06 | `CreateUnitsForPlayer1`, 英雄载入函数 | 20 nonhero: `earc`1,`echm`1,`edoc`2,`edot`2,`efdr`2,`emtg`1,`esen`2,`ewsp`9 |
| ASSET-07 | P1 资源/科技/采集初始化 | 500/150; `Renb`1 then 0; `Repm` disabled; `Ekee/Emoo/Edem/Ewar` disabled; harvest + entangle orders |
| HERO-01 | 玛维载入函数 | `RestoreUnit(...,"Maiev","NightElfX07")`; null => `CreateUnit(P1,'Ewrd',...)`, level 8 |
| HERO-02 | 法里奥载入函数 | `RestoreUnit(...,"Furion","NightElfX07")`; null => `CreateUnit(P1,'Emfr',...)`, level 10 |
| HERO-03 | Kael 初始化，胜利过场/跳过路径 | `CreateUnit(P0,'Hkal',...)`, level 2, `SuspendHeroXP`; 过场临时 `Hkal` |
| HERO-04 | Paladin 初始化，救援完成，结束存档 | `CreateUnit(Player(9),'Hmgd',...)`, level 8, paused+invulnerable; `SetUnitOwner(...,P1,true)`; cache `Magroth/FinalX` |
| HERO-05 | 放置单位与胜利过场创建函数 | `Hvsh`,`Eevi`,`E000`,`Udre`x1,`Ulic`x2 |
| HERO-06 | Tyrande 过渡函数；官方 X06 JASS 交叉检查 | X07 `RestoreUnit(...,"Tyrande","FinalX")` -> drop items -> remove; X06 default `Etyr` -> save `Tyrande/FinalX` |
| HERO-07 | `War3Patch.mpq::Units\\UnitBalance.slk`, X07 `war3map.w3u` | SLK SHA `1DC1A308826E85C03D963EA3BE750E771153BB31BC058F9D45B8F3E6F0680F27`；7 rawcodes `fused="-"`; `Udre/Ulic fused=5`; `E000` 基类 `Eevi` 且无 `ufoo`;W3U 无英雄 `ufoo` 覆盖 |
| HERO-08 | 英雄载入/创建/过场帮助函数 | restore-or-create=`Ewrd/Emfr`; create=`Hkal/Hmgd`; restore-remove=`Etyr`; `ReviveHeroLoc` 仅 `Ewrd/Emfr`; altar `halt/oalt/uaod/eate` |
| ALLYAI-01 | `Trig_Intro_Cleanup_Actions`, Intro skip cleanup | `StartMeleeAI(P0,"n07x01.ai")`; `CommandAI(P0,0,...)` 在正常/跳过收尾均可达 |
| ALLYAI-02 | 全图 P0 AI 调用检索 | 后续 P0 `CommandAI`=0；P0 `PauseCompAI`=0 |
| ALLYAI-03 | P0 资源维护触发 | thresholds 1000/1000; grants 1351/1273 |
| ALLYAI-04 | Intro cleanup AI 启动 | P3 `n07x04.ai`; P2 `n07x03.ai`; P5 `n07x06.ai`; P6 `n07x07.ai`; P4 none; all starts followed by command 0 |
| ALLYAI-05 | 全 `war3map.j` 符号检索 | `GetPlayerController`=0, `GetPlayerSlotState`=0, `EVENT_PLAYER_LEAVE`=0 |
| QUEST-01 | 主任务创建函数，`war3map.wts` | “毁灭之咒”；4 summoners；limited time；magic/spell damage |
| QUEST-02 | 任务发现与计时初始化 | 1801 main; 1741 one-minute warning; 1681 two-minute warning; timer dialog shown to P1 |
| QUEST-03 | 任务 requirement 创建/完成函数全图检索 | two initialized requirements; `udg_QuestSummoningReqStop` completion write exists, initialization absent |
| QUEST-04 | Paladin 发现/救援触发 | discover owner=P1; complete entering owner=P1, ground, non-summoned; reward `SetUnitOwner(Hmgd,P1,true)` |
| REWARD-01 | Paladin 完成与 victory cache 函数 | enable XP; `StoreUnit(...,"Magroth","FinalX")`; `StoreBoolean(...,"MagrothSaved",true)` |
| CINE-01 | Intro, Victory 1/2, skip trigger 初始化 | camera/selection P1 force; `TriggerRegisterPlayerEventEndCinematic(...,Player(1))` |
| CINE-02 | Intro normal/skip cleanup | pause/resume unit groups; both exits call friendly/enemy AI startup |
| END-01 | summoner group 初始化，召唤者死亡与 victory trigger | `nnsu` units 0033/0034/0035/0039; empty group => GameOver, timers paused/disabled, cinematic queued |
| END-02 | P1 structure-loss trigger, main timer expire trigger, cheat triggers | `CustomDefeatBJ(P1,...)`; win/loss cheat and final victory target P1 |
| END-03 | 胜利存档/切图函数 | Maiev/FinalX; Furion/NightElfX08; optional Magroth/FinalX; remove `stwp`; mission `XN09`; next `NightElfX08.w3x` |
| META-01 | hard/normal/easy 初始化函数 | hard +5 P6 Undead, +9 neutral Naga, `Udre`10, `Ulic`x2 level 8; normal no-op; easy handicap; timer unchanged |
| META-02 | 玩家/英雄初始化 | `SetPlayerHandicapXP(P1,0.50)`; `SuspendHeroXP(Hkal,true)` |
| SCOPE-01 | 全脚本 P1 条件与函数调用检索 | region owner P1/non-summoned, attacked-event P1, timer UI, camera, pings, optional quest, win/loss, cinematic skips |

## 玩家索引和原始代码速查

| 脚本变量 / 索引 | 玩家 | 用途 |
|---|---|---|
| `udg_P1_Kael` | P0 / `Player(0)` | 人类联盟残存者，友军 AI 候选 |
| `udg_AP2_Player`, `udg_APG2_Player` | P1 / `Player(1)` | 原版用户、哨兵、任务 UI/过场/胜负 |
| `udg_P3_Naga` | P2 / `Player(2)` | 娜迦 AI `n07x03.ai` |
| `udg_P4_Naga` | P3 / `Player(3)` | 娜迦 AI `n07x04.ai` |
| `udg_P5_Naga` | P4 / `Player(4)` | 娜迦，无 AI 入口 |
| `udg_P6_Undead` | P5 / `Player(5)` | 不死 AI `n07x06.ai` |
| `udg_P7_Undead` | P6 / `Player(6)` | 不死 AI `n07x07.ai` |
| Paladin owner | P9 / `Player(9)` | 马格罗斯初始中立所有者 |

| rawcode | 对象 | 路径 | 有效人口 |
|---|---|---|---|
| `Ewrd` | 玛维 | cache restore / fallback create / cinematic revive / save | 0 |
| `Emfr` | 法里奥 | cache restore / fallback create / cinematic revive / save | 0 |
| `Hkal` | 凯尔 | P0 create / victory cinematic copy | 0 |
| `Hmgd` | 马格罗斯 | P9 create / transfer P1 / optional save | 0 |
| `Etyr` | 泰兰德 | `FinalX` restore / drop items / remove | 0 |
| `Hvsh` | 法斯琪 | enemy placed | 0 |
| `Eevi` | 尤迪安 | enemy placed / cinematic | 0 |
| `E000` | 自定义尤迪安 | victory-skip cinematic variant; inherits `Eevi` | 0 |
| `Udre` | 恐惧魔王 | enemy placed; hard level 10 | 5 |
| `Ulic` | 恐惧魔王 | enemy placed x2; hard level 8 | 5 |

## 交叉地图证据

`Etyr` 的 `FinalX` 来源通过同一官方归档中的前置地图交叉确认：

| 对象 | 值 |
|---|---|
| X06 entry | `War3Patch.mpq::Maps\\FrozenThrone\\Campaign\\NightElfX06.w3x` |
| X06 map SHA-256 | `39ECFE1891DD8C3556FB778768A85903B65A21EE25A3D42836264EFFDD71DC04` |
| X06 `war3map.j` SHA-256 | `3097DF43E3B18361579D6B13949646F79F1B24A54DAD7C2F21B0D17AC03EEBAD` |
| 路径 | 缓存缺失时创建 `Etyr`；结束时 `StoreUnit(...,"Tyrande","FinalX")` |

## 限制

- 静态 JASS 可证明已写入的分支和调用，不能单独证明 1.24e 大厅中真人/电脑/空槽/未加入的实际运行时生命周期。
- 本次没有执行 `n07x01.ai` 或任何 JASS；AI 可运行性、建造节奏和 command 0 的实际效果仍待用户实机验证。
- `udg_QuestSummoningReqStop` 结论是“存在完成写入但未找到初始化”的静态异常，不等于已证明运行时可见故障。
