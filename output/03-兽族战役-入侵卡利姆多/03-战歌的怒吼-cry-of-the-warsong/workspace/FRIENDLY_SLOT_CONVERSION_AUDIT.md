# 战歌的怒吼：友方槽转换通用问题审计

- 审计日期：2026-09-09
- 归档位置确认：2026-09-10
- 审计对象：官方 RoC format 18 底图、P0 萨尔阵营、由电脑改为可选真人的 P3 战歌阵营
- 规则依据：[`docs/ORIGINAL_COOP_MECHANICS.md`](../../../../docs/ORIGINAL_COOP_MECHANICS.md)

## 审计结论

| 项目 | 证据与结论 | 处理 |
| --- | --- | --- |
| 目标客户端科技闭包 | 原图没有覆盖 TFT 后增科技；用户实测确认 P3 科技全开，P0 也可见新增巨魔科技。 | P0 关闭 `Oshd/ospw/otbk/otbr/ovln/Rolf/Rorb/Robk/Ropm/Robf`；仅活跃真人 P3 在一次性分类后精确镜像 P0 的全部科技与升级状态。电脑 P3 不覆盖原版 AI 科技。 |
| 中立敌对外交 | 原图未显式设置 P3 与 `PLAYER_NEUTRAL_AGGRESSIVE` 的关系；用户实测确认双方中立。 | 仅活跃真人 P3 补双向敌对；电脑 P3 保留原版关系。 |
| 模式隔离 | 真人专用科技和外交不能污染电脑回退。 | 自动化断言科技镜像位于 `udg_CoopP3Human` 分支，电脑模式仍可运行原版 `o03_grom.ai`、命令、补贴和反目。 |

## 状态

- 修复候选：`Orc03-Coop-Development.w3x`
- SHA-256：`E2599D670E69F8FA13B25E935D62CECA5ABE0366F7583008E7A65814918D424E`
- 静态状态：确定性构建、1.24e JASS、科技镜像和双向外交断言通过。
- 实机状态：按 [`TESTING.md`](TESTING.md) 重新验证；旧候选结果不得继承。
