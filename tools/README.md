# 工具脚本目录

- `Initialize-ProjectStructure.ps1`：根据 `docs/campaigns.json` 创建战役、关卡和说明文档，并为排期关卡生成缺失的社区检索与来源决策模板。游戏版本目录和 `original/`、`third-party/`、`workspace/`、`release/` 制作目录不预建，在实际需要时按需创建。
- `Sync-OriginalDescriptions.ps1`：从本地官方地图读取加载画面标题、正文和来源哈希，更新 65 关 `ORIGINAL.md`、`docs/campaigns.json` 与证据清单；使用 `-Check` 可复核同步状态。
- `wc3-inspect/`：基于 War3Net 的只读 MPQ、W3N、W3M/W3X 和游戏版本解析 CLI；由仓库级 `warcraft3-campaign-inspector` skill 调用。
- `wc3-coop-build/`：从已核验 SHA-256 的官方关卡生成可测试的合作地图；当前实现 `Orc08` 兽人玩家 1 + 原版人类友军玩家 2 / AI 回退技术原型。

脚本默认只补充缺失文件，不覆盖已经人工维护的关卡说明。传入 `-Force` 才会按清单重新生成说明文件。

```powershell
pwsh -File tools/Initialize-ProjectStructure.ps1
```
