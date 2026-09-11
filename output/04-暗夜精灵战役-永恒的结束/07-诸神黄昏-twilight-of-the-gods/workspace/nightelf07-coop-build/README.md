# NightElf07 合作地图构建器

该工具只接受 SHA-256 为 `97143C5E8F417DB033B566ED54A2359D6A2BA609F343A3DE79ABADF50A3D1C92` 的官方 1.24e `NightElf07` 地图，并只替换 `war3map.w3i`、`war3map.wts` 和 `war3map.j`。

当前规则为四名可玩英雄写入负责人确认的固定六格物品，泰兰德/玛法里奥固定为 7 级、分配已确认的 6 点技能并各保留 1 个未分配技能点；同时按开局固定 AI 模式区分 P0/P9 的真人 `1000/800` 与电脑 `6500/6500` 初始资源，原版周期补给仅保留给电脑模式。

构建：

```powershell
.\nightelf07-coop-build.ps1 nightelf07 ..\..\original\NightElf07-1.24e.w3m ..\NightElf07-Coop-Development.w3x
```

回归验证：

```powershell
.\tests\Test-NightElf07.ps1 -SourceMap ..\..\original\NightElf07-1.24e.w3m
```

构建器不会启动《魔兽争霸 III》客户端。实机测试按关卡 `workspace/TESTING.md` 由用户执行。
