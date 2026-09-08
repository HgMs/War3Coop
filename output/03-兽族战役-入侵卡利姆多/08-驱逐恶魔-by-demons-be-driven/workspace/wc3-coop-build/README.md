# Orc08 合作地图构建器

本目录是《驱逐恶魔》单关专用构建器，不是仓库级通用工具。它从已核验的官方关卡生成可测试的合作地图，只接受已登记 SHA-256 的源图，避免静默套用到不同版本。

当前支持：

- `orc08`：将 1.24e 中文版 `Maps\Campaign\Orc08.w3m` 生成“驱逐恶魔”双人合作版。玩家 1 保留完整兽人阵营，玩家 2 接管原版 `Player(9)` 人类探险队；玩家 2开局为电脑时沿用原版人类 AI，关闭/空槽不受支持。

```powershell
pwsh -File output/03-兽族战役-入侵卡利姆多/08-驱逐恶魔-by-demons-be-driven/workspace/wc3-coop-build/wc3-coop-build.ps1 orc08 `
  output/03-兽族战役-入侵卡利姆多/08-驱逐恶魔-by-demons-be-driven/original/Orc08-1.24e.w3m `
  output/03-兽族战役-入侵卡利姆多/08-驱逐恶魔-by-demons-be-driven/workspace/Orc08-Coop-Development.w3x
```

生成图直接使用补丁后的 `war3map.j`。World Editor 中重新保存会重建脚本并丢失合作补丁，因此当前开发版的修改入口是本工具；完成 GUI 触发器同步前，不要用编辑器覆盖生成图。

构建器不拆分兽人英雄、基地、单位或资源。它只开放原有人类槽位，并在游戏初始化时保存一次玩家 2控制模式；随后以该固定状态控制 `o08_Jaina.ai`、人类 `CommandAI`、AI经济、主动进攻和求援系统。开发版不注册玩家离开事件，也不提供离开后的 AI 接管。

构建时会先用 War3Net JASS解析器做结构校验。完整回归还会使用 1.24e `common.j`、`Blizzard.j` 和 `pjass` 做最低版本语义编译，并检查 WTS编码、函数声明顺序、槽位、种族、AI、外交、英雄人口和固定装备：

```powershell
pwsh -File output/03-兽族战役-入侵卡利姆多/08-驱逐恶魔-by-demons-be-driven/workspace/wc3-coop-build/tests/Test-Orc08.ps1 `
  -SourceMap output/03-兽族战役-入侵卡利姆多/08-驱逐恶魔-by-demons-be-driven/original/Orc08-1.24e.w3m
```
