# 工具脚本目录

- `Initialize-ProjectStructure.ps1`：根据 `docs/campaigns.json` 创建游戏版本、战役、关卡和制作子目录，并生成缺失的说明文件。
- `wc3-inspect/`：基于 War3Net 的只读 MPQ、W3N、W3M/W3X 和游戏版本解析 CLI；由仓库级 `warcraft3-campaign-inspector` skill 调用。

脚本默认只补充缺失文件，不覆盖已经人工维护的关卡说明。传入 `-Force` 才会按清单重新生成说明文件。

```powershell
pwsh -File tools/Initialize-ProjectStructure.ps1
```
