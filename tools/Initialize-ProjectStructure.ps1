[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $projectRoot 'docs/campaigns.json'
$loadingScreensPath = Join-Path $projectRoot 'docs/original-loading-screens.json'
$outputRoot = Join-Path $projectRoot 'output'
$selectedMissions = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase
)
@(
    '03-兽族战役-入侵卡利姆多/03-战歌的怒吼-cry-of-the-warsong',
    '03-兽族战役-入侵卡利姆多/08-驱逐恶魔-by-demons-be-driven',
    '04-暗夜精灵战役-永恒的结束/07-诸神黄昏-twilight-of-the-gods',
    '05-暗夜精灵战役-恐怖之潮/05-权衡利弊-balancing-the-scales',
    '05-暗夜精灵战役-恐怖之潮/07-达拉然废墟-the-ruins-of-dalaran',
    '05-暗夜精灵战役-恐怖之潮/08-怒风兄弟-the-brothers-stormrage',
    '06-血精灵战役-血精灵的诅咒/06-外域之王-lord-of-outland',
    '07-不死族战役-诅咒者的遗物/01-阿尔萨斯国王-king-arthas',
    '07-不死族战役-诅咒者的遗物/06-洛丹伦的新势力-a-new-power-in-lordaeron'
) | ForEach-Object {
    [void]$selectedMissions.Add($_)
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$Content
    )

    if ((Test-Path -LiteralPath $Path) -and -not $Force) {
        return
    }

    $parent = Split-Path -Parent $Path
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    Set-Content -LiteralPath $Path -Value $Content.TrimEnd() -Encoding utf8
}

$campaigns = Get-Content -LiteralPath $manifestPath -Raw -Encoding utf8 |
    ConvertFrom-Json
$loadingScreens = Get-Content -LiteralPath $loadingScreensPath -Raw -Encoding utf8 |
    ConvertFrom-Json
$loadingScreenByMission = @{}
foreach ($record in $loadingScreens.missions) {
    $key = "$($record.campaignDirectory)/$($record.missionDirectory)"
    $loadingScreenByMission[$key] = $record
}

foreach ($campaign in $campaigns) {
    $campaignDirectory = Join-Path $outputRoot $campaign.directory
    New-Item -ItemType Directory -Force -Path $campaignDirectory | Out-Null

    $missionList = ($campaign.missions | ForEach-Object {
        "- [$($_.number) $($_.titleZh)]($($_.directory)/README.md) - $($_.titleEn)"
    }) -join [Environment]::NewLine

    $campaignReadme = @(
        "# $($campaign.faction)战役：$($campaign.titleZh)",
        '',
        "- 原版：$($campaign.game)",
        "- 英文名：$($campaign.titleEn)",
        "- 可游玩关卡数：$($campaign.missions.Count)",
        "- 清单依据：[$($campaign.sourceLabel)]($($campaign.sourceUrl))",
        '',
        $campaign.summary,
        '',
        '## 关卡',
        '',
        $missionList
    ) -join [Environment]::NewLine
    Write-Utf8File -Path (Join-Path $campaignDirectory 'README.md') -Content $campaignReadme

    foreach ($mission in $campaign.missions) {
        $missionDirectory = Join-Path $campaignDirectory $mission.directory
        New-Item -ItemType Directory -Force -Path $missionDirectory | Out-Null
        $missionKey = "$($campaign.directory)/$($mission.directory)"
        $isSelected = $selectedMissions.Contains($missionKey)

        $loadingScreen = $loadingScreenByMission[$missionKey]
        $warnings = if ($loadingScreen.warnings.Count -eq 0) {
            '无'
        } else {
            $loadingScreen.warnings -join '；'
        }

        $originalReadme = @(
            "# 原版关卡说明：$($mission.titleZh)",
            '',
            "- 所属游戏：$($campaign.game)",
            "- 所属战役：$($campaign.faction)战役《$($campaign.titleZh)》",
            "- 章节编号：$($mission.number)",
            "- 英文名：$($mission.titleEn)",
            "- 类型：$($mission.type)",
            '',
            '## 本地加载界面原文',
            '',
            $loadingScreen.loadingScreen.text.TrimEnd(),
            '',
            '## 核对依据',
            '',
            "- 核对版本：Warcraft III $($loadingScreens.gameVersion.label)（build $($loadingScreens.gameVersion.build)，简体中文本地化）",
            "- 加载画面标题：$($loadingScreen.loadingScreen.title) / $($loadingScreen.loadingScreen.subtitle)",
            "- 本地来源：``$($loadingScreen.sourceContainer) / $($loadingScreen.sourceEntry)``",
            "- 地图 SHA-256：``$($loadingScreen.sourceSha256)``",
            "- 地图格式版本：$($loadingScreen.formatVersion)",
            "- 解析警告：$warnings"
        ) -join [Environment]::NewLine
        Write-Utf8File -Path (Join-Path $missionDirectory 'ORIGINAL.md') -Content $originalReadme

        if ($isSelected) {
            $missionReadme = @(
                "# $($mission.number) $($mission.titleZh)",
                '',
                '原版信息见 [ORIGINAL.md](ORIGINAL.md)。',
                '',
                '## 多人制作状态',
                '',
                '- 是否纳入多人化：是',
                '- 当前阶段：待检索候选底图',
                '- 支持人数：待评估',
                '- 兼容目标：1.24e-1.27b 必须支持；1.31 尽力支持',
                '- 许可要求：底图必须来源可追溯、可审计，并明确允许修改和分发',
                '',
                '## 目录',
                '',
                '- original/：原版地图和本地对照材料；不提交外部二进制。',
                '- third-party/：互联网候选作品、哈希和授权证据。',
                '- workspace/：实际修改内容和过程记录。',
                '- release/：最终成品、版本说明和校验值。',
                '',
                '## 验收状态',
                '',
                '- [ ] 原版数据核对完成',
                '- [ ] `COMMUNITY_SEARCH.md` 通过轻量检索完成门',
                '- [ ] 轻改候选完成必要核验，或已确认无轻改候选',
                '- [ ] `SOURCE_EVALUATION.md` 完成必要的路线成本评估',
                '- [ ] 底图路线有证据支持并完成复核',
                '- [ ] 多人化方案确定并开始制作',
                '- [ ] 全部可达战役英雄已核对为 0 人口占用',
                '- [ ] 1.24e-1.27b 完整回归通过',
                '- [ ] 1.31 兼容性验证完成',
                '- [ ] 成品与变更说明进入 release/'
            ) -join [Environment]::NewLine

            $communitySearch = @(
                "# $($mission.titleZh)：社区作品检索记录",
                '',
                '- 状态：检索未完成',
                '- 检索负责人：待填写',
                '- 检索日期：待填写',
                '- 实际投入时间：待填写（默认总上限 15 分钟，不延长）',
                '',
                '## 预先定义的硬条件',
                '',
                '- 合作模式与人数：待填写',
                '- 原版还原要求：遵循本项目 `docs/SCOPE.md`，该关特殊要求待填写',
                '- 战役英雄人口：全部可达战役英雄的有效人口占用必须为 0',
                '- 兼容目标：1.24e-1.27b 必须支持；1.31 尽力支持',
                '- 许可与维护：来源可追溯、可审计，并明确允许修改和分发',
                '',
                '## 最小覆盖',
                '',
                '| 类型 | 站点/入口 | 原样检索式 | 是否发现轻改候选 |',
                '| --- | --- | --- | --- |',
                '| 主要社区来源 | 待填写 | 待填写 | 待填写 |',
                '| 独立跨站搜索 | 待填写 | 待填写 | 待填写 |',
                '',
                '## 轻改候选',
                '',
                '轻改候选及首发页：待填写（没有时写“无”）',
                '',
                '合并排除摘要：待填写',
                '',
                '下载核验：未触发；只有一个具体文件级未知决定作品是否仍属于轻改候选时才填写。',
                '',
                '## 轻量检索完成门',
                '',
                '- [ ] 第 2 阶段目标与验收标准已确认',
                '- [ ] 一个主要社区来源已检查',
                '- [ ] 一次独立跨站搜索已完成',
                '- [ ] 轻改候选已完成必要核验，或已明确记录无轻改候选',
                '- [ ] 结论与停止原因已记录',
                '',
                '## 结论',
                '',
                '结论：待填写（直接采用候选 / 轻改候选 / 本次轻量检索未发现轻改候选）',
                '',
                '停止原因：待填写'
            ) -join [Environment]::NewLine
            Write-Utf8File -Path (Join-Path $missionDirectory 'COMMUNITY_SEARCH.md') -Content $communitySearch

            $sourceEvaluation = @(
                "# $($mission.titleZh)：底图来源与成本决策",
                '',
                '前置条件：`COMMUNITY_SEARCH.md` 尚未通过轻量检索完成门。完成前不得作出底图决策。',
                '',
                '## 社区轻改路线',
                '',
                '- 轻改候选：待填写（没有时写“社区路线不成立”）',
                '- 关键硬条件与剩余修改：待填写',
                '- 许可与可审计结论：待填写',
                '',
                '## 路线成本',
                '',
                '| 路线 | 总工时区间 | 主要风险与关键假设 |',
                '| --- | --- | --- |',
                '| 社区作品轻改 | 待评估；没有轻改候选时写“不成立” | 待填写 |',
                '| 官方原图修改 | 待评估 | 待填写 |',
                '',
                '## 决策',
                '',
                '- 状态：未决策',
                '- 选择路线：待填写',
                '- 成本结论与不确定性：待填写',
                '- 决策日期与复核人：待填写',
                '- 重新评估触发条件：目标变化、出现具体的新轻改线索、已入围项目更新或实际成本越界'
            ) -join [Environment]::NewLine
            Write-Utf8File -Path (Join-Path $missionDirectory 'SOURCE_EVALUATION.md') -Content $sourceEvaluation
        } else {
            $missionReadme = @(
                "# $($mission.number) $($mission.titleZh)",
                '',
                '原版信息见 [ORIGINAL.md](ORIGINAL.md)。',
                '',
                '## 多人制作状态',
                '',
                '- 是否纳入多人化：暂不制作',
                '- 当前阶段：仅保留关卡说明',
                '',
                '本关未列入当前制作批次。制作资料目录将在关卡排期后按需创建。'
            ) -join [Environment]::NewLine
        }

        Write-Utf8File -Path (Join-Path $missionDirectory 'README.md') -Content $missionReadme
    }
}

$missionCount = ($campaigns | ForEach-Object {
    $_.missions.Count
} | Measure-Object -Sum).Sum

Write-Host "Initialized $($campaigns.Count) campaigns and $missionCount missions."
