# Warcraft III Campaign Inspector 实现说明

## 1. 文档目的

本文说明仓库内 `warcraft3-campaign-inspector` skill 如何读取《魔兽争霸 III》经典版游戏归档、战役和地图文件，并澄清标准 MPQ 解密、地图结构解析以及地图编辑之间的边界。

该 skill 是只读调查流程。它不直接实现二进制解析算法，也不负责修改或重新封装地图；实际解析由仓库内的 `wc3-inspect` CLI 和 War3Net 库完成。仓库中的地图修改由独立的 `wc3-coop-build` 工具承担。

## 2. 组件与调用链

```text
warcraft3-campaign-inspector skill
  -> .agents/skills/warcraft3-campaign-inspector/scripts/inspect.ps1
  -> tools/wc3-inspect/wc3-inspect.ps1
  -> wc3-inspect.dll
  -> War3Net.IO.Mpq
  -> War3Net.Build.Core
```

各组件职责如下：

| 组件 | 职责 |
| --- | --- |
| `SKILL.md` | 规定输入路由、只读约束、证据记录方式和安全边界 |
| `scripts/inspect.ps1` | 将 skill 参数转发给仓库内 CLI |
| `wc3-inspect` | 提供检测、版本识别、归档检查、条目提取、地图和战役检查命令 |
| `War3Net.IO.Mpq` | 解析 MPQ，处理标准 Storm 加密、压缩和文件流 |
| `War3Net.Build.Core` | 解析 W3I、WTS、DOO、IMP 等 Warcraft III 数据格式 |
| `wc3-coop-build` | 在独立的、显式调用的流程中修改对象模型和 JASS，并重建地图 |

## 3. 支持的输入

`wc3-inspect` 当前识别以下输入：

- 经典版 Warcraft III 游戏目录；
- 普通 MPQ 归档；
- `.w3m` 和 `.w3x` 地图；
- `.w3n` 自定义战役。

对于经典游戏目录，查找同名条目时使用以下覆盖顺序：

1. `War3Patch.mpq`
2. `War3xlocal.mpq`
3. `War3x.mpq`
4. `War3.mpq`

报告会保留最终提供条目的归档路径、内部路径、文件大小和 SHA-256。当前实现不会跨归档物化 Blizzard 增量补丁差异，也尚未实现 1.31 数据的 CASC 安装发现。

## 4. 标准 MPQ 解密

### 4.1 “解密”的含义

这里的解密指读取 MPQ 格式规定的 Storm 加密数据。它不是密码爆破、DRM 绕过，也不是通用的地图保护移除。

调用方通过 `MpqArchive.Open()` 打开归档，再以内部文件名调用 `TryOpenFile()`。返回的流由 War3Net 在读取过程中完成定位、标准解密和解压。

### 4.2 归档表解密

MPQ 头记录 Hash Table 和 Block Table 的位置与大小：

- Hash Table 保存文件名散列、区域和 Block 索引；
- Block Table 保存文件偏移、压缩大小、展开大小和标志位。

这两个表本身使用固定名称派生密钥：

```text
Hash Table key  = HashString("(hash table)",  0x300)
Block Table key = HashString("(block table)", 0x300)
```

解密后，工具才能取得各内部文件的偏移、大小、压缩和加密状态。

### 4.3 文件查找与 seed 计算

MPQ 主要通过文件名散列查找条目，而不是依赖普通目录树。文件名在散列前转换为大写；已知名称可以直接用于定位对应条目。

加密文件通常使用文件名的 basename 派生基础 seed：

```text
baseSeed = HashString(fileName, 0x300)
```

如果条目带有 `BlockOffsetAdjustedKey` 标志，还会结合条目偏移和原始大小调整：

```text
seed = (baseSeed + fileOffset) XOR fileSize
```

### 4.4 数据块解密与解压

Storm 算法先生成一个 `0x500` 项的查找表，然后以两个滚动 seed 对数据进行处理。数据按 32 位字解密；不足 4 字节的尾部不参与该轮变换。

对于分扇区文件：

1. 读取扇区偏移表；
2. 若偏移表被加密，使用 `seed - 1` 解密；
3. 第 `n` 个数据扇区使用 `seed + n` 解密；
4. 校验扇区偏移和压缩类型；
5. 对解密结果执行对应的 MPQ 解压；
6. 向调用方暴露展开后的普通字节流。

如果文件名未知，War3Net 可以根据前两个加密扇区偏移和预期的偏移表结构尝试反推 seed。该能力只适用于满足特定结构假设的标准 MPQ 条目，不能视为通用解密或地图保护恢复。

### 4.5 `(listfile)` 的作用

`(listfile)` 是可选的内部文件名清单：

- 存在时，可以为更多 Hash Table 条目恢复可读名称；
- 缺失或不完整时，不能完整枚举所有内部路径；
- 已知常规路径仍可直接通过散列查找，因此“不能枚举”不等于“不能读取”。

## 5. 地图与战役解析

`.w3m`、`.w3x` 和 `.w3n` 本身也是 MPQ 容器。归档层得到内部文件流后，War3Net 按各自的 Warcraft III 二进制格式反序列化。

地图检查器当前读取：

| 内部文件 | 内容 |
| --- | --- |
| `war3map.w3i` | 地图信息、玩家槽位、种族、控制器、队伍和地图标志 |
| `war3map.wts` | `TRIGSTR_*` 本地化字符串 |
| `war3map.j` | JASS 地图脚本 |
| `war3map.imp` | 导入文件清单 |
| `war3mapUnits.doo` | 放置的单位和物品 |
| `war3map.doo` | 装饰物和特殊装饰物 |

战役检查器当前读取：

| 内部文件 | 内容 |
| --- | --- |
| `war3campaign.w3f` | 战役元数据、章节按钮和地图顺序 |
| `war3campaign.wts` | 战役触发字符串 |
| `war3campaign.imp` | 战役导入文件 |
| `war3campaign.w3a/.w3h/.w3b/.w3d/.w3t/.w3u/.w3q` | 各类战役对象数据是否存在 |

检查器不会执行 `war3map.j`、Lua 或 AI 脚本。当前对 JASS 只做文本统计；结构化 WTG/WCT 查询和 JASS AST 查询仍属于规划功能。

## 6. 输出与安全控制

检查命令输出 UTF-8 JSON，并记录：

- 输入类型；
- 绝对来源路径；
- 来源归档和内部条目；
- 文件大小和 SHA-256；
- 地图或战役格式版本；
- 已解析的元数据和内容规模；
- 可选组件的解析警告。

归档提取有以下保护：

- 默认拒绝覆盖已有输出；
- 只有显式传入 `--overwrite` 才允许覆盖准确目标；
- 默认展开大小上限为 512 MiB；
- 只提取调用方指定的内部条目。

## 7. 地图编辑的独立实现

`warcraft3-campaign-inspector` 不编辑地图。仓库当前的编辑示例位于 `wc3-coop-build`，其处理流程如下：

```text
核验原图 SHA-256
  -> Map.Open 读取 W3I、WTS 和 JASS
  -> 修改 MapInfo 对象
  -> 修改 TriggerStrings
  -> 修改 war3map.j
  -> 用 JASS parser 验证语法
  -> 载入原 MPQ 中的其余文件
  -> MapBuilder 序列化已修改组件并重建 W3X
  -> 计算输出 SHA-256
```

以 `Orc08Builder` 为例：

1. 只接受 SHA-256 与登记值相同的 1.24e 原图，防止补丁误用于其他版本；
2. 将 `MapInfo` 中指定玩家槽位从电脑控制改为用户控制；
3. 更新地图名称、说明、推荐人数和 WTS 文本；
4. 对 `war3map.j` 执行带出现次数断言的确定性修改；
5. 使用 War3Net JASS parser 检查修改后脚本语法；
6. `MapBuilder.AddFiles(sourceArchive)` 保留未修改的原始条目；
7. `MapBuilder.Build(output)` 将修改后的结构化文件和保留条目写入新的 MPQ 地图。

这种方式不是直接修改原图字节，而是“解析对象模型、修改、重新序列化并构建新归档”。输出文件已存在时构建器会拒绝覆盖。

当前合作原型直接修改生成后的 `war3map.j`。如果用 World Editor 重新保存，编辑器可能重新生成脚本并覆盖这些补丁；在 GUI 触发器尚未同步之前，应继续以构建器作为修改入口。

## 8. 能力边界

当前实现可以：

- 读取经典 Warcraft III MPQ；
- 透明处理标准 MPQ Storm 加密和压缩；
- 按已知路径提取游戏内地图；
- 解析常规 W3M、W3X 和 W3N 的已支持组件；
- 为官方原图调查提供来源、版本和哈希证据；
- 通过独立构建器对已核验的特定地图进行确定性修改。

当前实现不能保证：

- 绕过人为制作的地图保护；
- 恢复被删除、伪造或恶意破坏的文件表和内部结构；
- 恢复混淆后的原始 GUI 触发器；
- 解析任意非标准 MPQ 变体；
- 处理现代 CASC 安装发现；
- 将 Blizzard 跨归档增量补丁自动物化成完整文件；
- 作为通用可视化地图编辑器使用。

## 9. 关键源码位置

- Skill 说明：`.agents/skills/warcraft3-campaign-inspector/SKILL.md`
- Skill 入口：`.agents/skills/warcraft3-campaign-inspector/scripts/inspect.ps1`
- CLI 分发：`tools/wc3-inspect/src/Wc3Inspect.Cli/CommandDispatcher.cs`
- MPQ 调用封装：`tools/wc3-inspect/src/Wc3Inspect.Cli/ArchiveService.cs`
- 地图检查：`tools/wc3-inspect/src/Wc3Inspect.Cli/MapInspector.cs`
- 战役检查：`tools/wc3-inspect/src/Wc3Inspect.Cli/CampaignInspector.cs`
- 编辑构建示例：`tools/wc3-coop-build/src/Wc3CoopBuild.Cli/Orc08Builder.cs`
- War3Net MPQ 参考源码：`build/reference/War3Net/src/War3Net.IO.Mpq/`

