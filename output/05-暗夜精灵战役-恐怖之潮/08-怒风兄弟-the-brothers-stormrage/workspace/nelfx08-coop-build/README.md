# NightElfX08 双人合作构建器

本工具只接受已登记 SHA-256 的官方 1.24e `NightElfX08.w3x`，生成第 5 阶段开发候选。它修改 `war3map.w3i`、`war3map.wts`、`war3map.j` 和 `war3map.w3u`，其余 MPQ 内容直接继承官方底图。

```powershell
.\nelfx08-coop-build.ps1 `
  ..\..\original\NightElfX08-1.24e.w3x `
  ..\NightElfX08-Coop-Development.w3x
```

完整自动验证：

```powershell
.\tests\Test-NelfX08.ps1 `
  -SourceMap ..\..\original\NightElfX08-1.24e.w3x
```

构建器不会覆盖已有输出。开发候选属于可重复生成文件，不进入 `release/`；只有第 6 阶段经负责人确认验收后，才允许以相同 SHA-256 进入第 7 阶段发布。
