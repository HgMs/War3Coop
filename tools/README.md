# 工具脚本目录

- `Initialize-ProjectStructure.ps1`：根据 `docs/campaigns.json` 创建战役、关卡和说明文档，并为排期关卡生成缺失的社区检索与来源决策模板。游戏版本目录和 `original/`、`third-party/`、`workspace/`、`release/` 制作目录不预建，在实际需要时按需创建。
- `Sync-OriginalDescriptions.ps1`：从本地官方地图读取加载画面标题、正文和来源哈希，更新 65 关 `ORIGINAL.md`、`docs/campaigns.json` 与证据清单；使用 `-Check` 可复核同步状态。
- `wc3-inspect/`：基于 War3Net 的只读 MPQ、W3N、W3M/W3X 和游戏版本解析 CLI；由仓库级 `warcraft3-campaign-inspector` skill 调用。

根 `tools/` 只存放可跨关复用的工具。单关专用构建器、补丁和回归脚本放在对应关卡的 `workspace/`；例如 Orc08 构建器位于 `output/03-兽族战役-入侵卡利姆多/08-驱逐恶魔-by-demons-be-driven/workspace/wc3-coop-build/`。

通用工具的集成测试可以用某张官方地图作为固定夹具并以关卡命名，只要测试目标是通用解析能力且不包含该关的制作或补丁逻辑。例如 `wc3-inspect/tests/Test-TwilightOfTheGods.ps1` 留在这里，因为它验证版本识别、地图格式、字符串解析和哈希，不生成《诸神黄昏》合作地图。

脚本默认只补充缺失文件，不覆盖已经人工维护的关卡说明。传入 `-Force` 才会按清单重新生成说明文件。

```powershell
pwsh -File tools/Initialize-ProjectStructure.ps1
```
