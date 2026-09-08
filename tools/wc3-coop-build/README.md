# wc3-coop-build

从已核验的官方关卡生成可测试的合作地图。工具只接受已登记 SHA-256 的源图，避免静默套用到不同版本。

当前支持：

- `orc08`：将 1.24e 中文版 `Maps\Campaign\Orc08.w3m` 生成“驱逐恶魔”双人合作开发版。玩家 1 保留完整兽人阵营，玩家 2 接管原版 `Player(9)` 人类探险队；玩家 2开局为电脑或空槽时沿用原版人类 AI。

```powershell
pwsh -File tools/wc3-coop-build/wc3-coop-build.ps1 orc08 `
  output/03-兽族战役-入侵卡利姆多/08-驱逐恶魔-by-demons-be-driven/original/Orc08-1.24e.w3m `
  output/03-兽族战役-入侵卡利姆多/08-驱逐恶魔-by-demons-be-driven/workspace/Orc08-Coop-Development.w3x
```

生成图直接使用补丁后的 `war3map.j`。World Editor 中重新保存会重建脚本并丢失合作补丁，因此当前开发版的修改入口是本工具；完成 GUI 触发器同步前，不要用编辑器覆盖生成图。

构建器不拆分兽人英雄、基地、单位或资源。它只开放原有人类槽位，并在游戏初始化时保存一次玩家 2控制模式；随后以该固定状态控制 `o08_Jaina.ai`、人类 `CommandAI`、AI经济、主动进攻和求援系统。开发版不注册玩家离开事件，也不提供离开后的 AI 接管。

构建时会先用 War3Net JASS 解析器做语法校验。结构与关键补丁回归：

```powershell
pwsh -File tools/wc3-coop-build/tests/Test-Orc08.ps1 `
  -SourceMap output/03-兽族战役-入侵卡利姆多/08-驱逐恶魔-by-demons-be-driven/original/Orc08-1.24e.w3m
```
