# 战歌的怒吼：第 5 阶段实施计划

状态：第 5 阶段完成；候选已进入第 6 阶段实机验收

路线确认：项目负责人于 2026-09-09 选择“基于官方原图修改”。

## 正式底图

- 文件：`../original/Orc03-1.24e.w3m`
- 来源：Warcraft III 1.24e（`1.24.4.6387`）`War3Patch.mpq/Maps\Campaign\Orc03.w3m`
- SHA-256：`84ADDFD4D347B2A0A89947F47680CA4D968C8FB1DB3E649515C22CE3A659F239`
- 许可边界：仅制作非商业、由 Warcraft III 引擎运行的自定义地图；不引入第三方地图或素材。发布前复核 Blizzard 现行条款。
- 构建入口：`orc03-coop-build/orc03-coop-build.ps1`
- 候选产物：`Orc03-Coop-Development.w3x`

## 启动模式矩阵

P3 控制模式只在初始化时分类一次；后续所有分支读取保存的 `udg_CoopP3Human` / `udg_CoopP3AI`，不再读取实时控制器或槽位状态。

| 原版机制 | 原入口 | P3 活跃真人 | P3 正在游戏的电脑 | 关闭/空槽/未加入 |
| --- | --- | --- | --- | --- |
| 战歌 Campaign AI | `Trig_ActivateGromAI_Actions` | 屏蔽 | 原时点启动一次 | 不启动；不受支持 |
| 阶段命令 | 全部 `CommandAI(Player(3), ...)` | 屏蔽 | 原参数、原顺序 | 屏蔽 |
| 金木保底 | `Trig_GromGold/Lumber_Actions` | 屏蔽 | 各低于 1000 时加 1000 | 屏蔽 |
| AI 强制复活 | `Trig_ActivateGromAI_Actions` | 屏蔽，使用本阵营祭坛 | 保留 | 屏蔽 |
| 周期战歌攻击命令 | `Trig_GromAttacksBrownHeartbeat_Actions` | 屏蔽；开场一次性站位保留 | 保留 | 屏蔽 |
| 反目 | `Player_Attacks_Grom/Teal`、`GromAggrosPlayerCheck` | 不计数、不改外交 | 完整保留 | 不执行 |
| 终场 AI 清理 | `RemoveAllGuardPositions(Player(3))`、隐藏 P3 单位 | 不执行 | 保留 | 保留原非真人路径 |

模式固定；不注册 `EVENT_PLAYER_LEAVE`，不提供中途退出后的 AI 接管、资源重置、共享控制或资产转移。

## 稳定编号实施与验收映射

| 编号 | 实现位置 | 静态/自动验证 | 实机入口 |
| --- | --- | --- | --- |
| `SLOT-01..05`、`EXTRA-01..04` | W3I P3 用户槽；`CoopClassifyP3`；`CoopApplyHumanP3Diplomacy`；config 联盟菜单标志 | 精确两个用户槽；双条件各出现一次；无离开事件；无默认共享控制 | `TESTING.md` 场景 0、A、B、C |
| `ASSET-01..03` | 不修改放置单位/装饰物；只替换脚本、W3I、WTS、W3U | 放置单位 230、装饰物 3875；P0/P2/P3 原所有权与原图一致 | 场景 A、B |
| `ECON-01..02` | 原初始资源保留；`Trig_GromGold/Lumber_Actions` 按固定 AI 模式门控 | P0 1500/500、P3 1000/1000；两个补贴仅 AI 可达 | 场景 A、B |
| `TECH-01..02` | 保留原科技；P0/P2/P3 显式禁止 `Oshd`；全部有效玩家锁定种族 | 三个 `Oshd` 限制；原科技调用保留；无可选种族 | 场景 0、A |
| `HERO-01..03` | `Trig_Load_Thrall/Grom_Actions` 改为确定性兜底；萨尔按确认顺序获得六格物品；敌方英雄路径不变 | 无缓存/恢复调用；等级、技能、所有权和 `rin1/rst1/penr/hlst/whwd/shea` 顺序精确断言；萨尔函数无次数覆盖 | 场景 A、B、D |
| `HERO-FOOD-01` | W3U 为 `Hamg/Hpal/Hmkg` 写入 `ufoo=0` | 叠加 1.24e `UnitBalance.slk` 后五种英雄均为 0 | 场景 A、B、D |
| `ALLYAI-01..06` | `CoopActivateGromAI`、`CoopCommandGromAI`、补贴与周期命令门；P2脚本不变 | 枚举全部 P3 AI 入口；无裸 `StartCampaignAI/CommandAI`；无重复启动 | 场景 A、B、C、E |
| `BETRAYAL-01` | 两个死亡条件和反目动作仅 P3 AI 模式可达；终场按模式复盟 | 真人路径不可达四个转敌调用、AI启动、复活和命令4；电脑路径完整 | 场景 E |
| `QUEST-01..03`、`REWARD-01..02`、`EVENT-01` | 保留 P0 建镇/飞艇/塔门槛；P0专属消息扩展到双方；任务计数不改 | 事件玩家集合和飞艇条件与原图一致；无新奖励 | 场景 F |
| `CINE-01..03`、`UI-01` | P0 镜头调用对活跃真人 P3镜像；任一真人注册跳过；P3初始选择格罗姆；终场不隐藏真人 P3资产 | 正常/跳过入口各一个全局门；相机调用成对；真人分支不清除 P3守卫或隐藏资产 | 场景 G、H |
| `END-01..05` | 原 P0失败条件不改；`CoopVictory/CoopDefeat` 扩展双真人并加单一结果锁 | P0条件精确保留；P3单独全灭无新条件；无裸最终结算 | 场景 I |
| `DIFF-01` | `main` 返回后 0.01秒显示普通/困难对话框，30秒默认普通；选择前不初始化触发器 | `InitCustomTriggers/RunInitializationTriggers` 只从一次性完成函数调用；两档文本存在 | 场景 0、J |
| `META-01..02` | 移除缓存读取/保存、关卡解锁和下一地图；固定英雄状态 | 禁止 `InitGameCacheBJ`、`RestoreUnit*`、`StoreUnitBJ`、`SaveGameCacheBJ`、`SetMissionAvailableBJ`、`SetNextLevelBJ` | 场景 D |
| `COMPAT-01` | 保持格式 18/JASS；推荐人数 2；只使用 1.24e API | War3Net解析、1.24e `common.j`/`Blizzard.j` + pjass 语义编译、WTS UTF-8无BOM | 场景 0；版本矩阵 |

## 禁止回归项

- 不转移、复制或拆分 P0/P2/P3 的既有资产。
- 不把关闭、空槽或未加入当作正在游戏的电脑。
- 不在运行期重新读取槽位状态切换模式。
- 不在真人模式调用 P3 Campaign AI、`CommandAI`、补贴、AI复活、周期战歌命令或自动反目。
- 不删除电脑模式的外围 AI 协议；不能只保留 `.ai` 启动。
- 不扩大建镇、塔和飞艇的 P0 所有权门槛。
- 不默认共享资源、普通/高级单位控制或英雄控制。
- 不新增退出/断线处理，不使用 `Blz*`、Lua 或后期 API。
- 不执行或自动加载地图内脚本；开发代理不启动 Warcraft III 客户端。

## 完成条件

- [x] 构建器只接受已登记 SHA 的官方底图并可重复生成候选。
- [x] War3Net 结构解析与 1.24e pjass 语义编译通过。
- [x] W3I、W3U、WTS、JASS、对象/装饰物计数和所有静态断言通过。
- [x] `workspace/TESTING.md` 覆盖全部稳定编号并绑定候选 SHA。
- [x] 所有不依赖客户端的工作完成后，才请求用户执行第 6 阶段实机验证。

候选：`Orc03-Coop-Development.w3x`，258199 字节，SHA-256 `E2599D670E69F8FA13B25E935D62CECA5ABE0366F7583008E7A65814918D424E`。2026-09-10 自动验收通过。P0 已完成 TFT 新增科技闭包；真人 P3 在一次性槽位分类后精确镜像 P0 科技/升级并与中立敌对双向敌对，电脑 P3 保留原版科技与 AI 路径。萨尔固定携带 `rin1/rst1/penr/hlst/whwd/shea`；沿用 1.24e 原生次数，`whwd` 为 3 次，`shea` 为 1 次。
