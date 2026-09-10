# Orc03 合作地图构建器

本目录只服务《战歌的怒吼》。构建器仅接受已登记 SHA-256 的 Warcraft III 1.24e 官方 `Orc03.w3m`，生成经典 JASS/W3X 双人合作候选。

```powershell
pwsh -File output/03-兽族战役-入侵卡利姆多/03-战歌的怒吼-cry-of-the-warsong/workspace/orc03-coop-build/orc03-coop-build.ps1 orc03 `
  output/03-兽族战役-入侵卡利姆多/03-战歌的怒吼-cry-of-the-warsong/original/Orc03-1.24e.w3m `
  output/03-兽族战役-入侵卡利姆多/03-战歌的怒吼-cry-of-the-warsong/workspace/Orc03-Coop-Development.w3x
```

玩家 2使用原 P3 战歌阵营。开局同时满足 `MAP_CONTROL_USER` 和 `PLAYER_SLOT_STATE_PLAYING` 时进入真人模式；明确设置为正在游戏的电脑时进入 AI 模式。关闭、空槽或未加入不受支持，也不会被改写成电脑。

真人模式停用战歌 Campaign AI、全部 P3 `CommandAI`、资源保底、AI 强制复活、周期战歌命令和自动反目；电脑模式保留这些官方路径。模式整局固定，不处理玩家中途退出。

萨尔固定携带 `rin1/rst1/penr/hlst/whwd/shea`。构建器沿用 1.24e 基础物品数据，不覆盖物品次数：治疗守卫为原生 3 次，医疗卷轴为原生 1 次。

生成地图直接使用补丁后的 `war3map.j`。World Editor 重新保存会重建脚本并丢失补丁，因此修改入口是本构建器，不要用编辑器覆盖候选。

完整回归入口：

```powershell
pwsh -File output/03-兽族战役-入侵卡利姆多/03-战歌的怒吼-cry-of-the-warsong/workspace/orc03-coop-build/tests/Test-Orc03.ps1 `
  -SourceMap output/03-兽族战役-入侵卡利姆多/03-战歌的怒吼-cry-of-the-warsong/original/Orc03-1.24e.w3m
```
