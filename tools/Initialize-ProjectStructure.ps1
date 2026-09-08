[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $projectRoot 'docs/campaigns.json'
$loadingScreensPath = Join-Path $projectRoot 'docs/original-loading-screens.json'
$outputRoot = Join-Path $projectRoot 'output'
$gameRoot = Join-Path $projectRoot 'game'
$versions = @(
    '1.24e',
    '1.31'
)
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

foreach ($version in $versions) {
    $versionDirectory = Join-Path $gameRoot $version
    New-Item -ItemType Directory -Force -Path $versionDirectory | Out-Null
    Write-Utf8File -Path (Join-Path $versionDirectory '.gitkeep') -Content ''
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
                '- [ ] `COMMUNITY_SEARCH.md` 通过检索充分性门',
                '- [ ] 所有疑似社区候选完成硬条件核验',
                '- [ ] `SOURCE_EVALUATION.md` 完成社区候选/官方原图成本比较',
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
                '- 检索区间：待填写',
                '- 最后复查：待填写',
                '',
                '## 预先定义的硬条件',
                '',
                '- 合作模式与人数：待填写',
                '- 原版还原要求：遵循本项目 `docs/SCOPE.md`，该关特殊要求待填写',
                '- 战役英雄人口：全部可达战役英雄的有效人口占用必须为 0',
                '- 兼容目标：1.24e-1.27b 必须支持；1.31 尽力支持',
                '- 许可与维护：来源可追溯、可审计，并明确允许修改和分发',
                '',
                '## 检索明细',
                '',
                '| 日期 | 站点/入口 | 原样检索式与筛选 | 覆盖范围 | 命中与负面结果 |',
                '| --- | --- | --- | --- | --- |',
                '| 待填写 | 待填写 | 待填写 | 待填写 | 待填写 |',
                '',
                '## 候选闭环',
                '',
                '| 独立作品/版本 | 首发页与镜像关系 | 证据 | 状态 |',
                '| --- | --- | --- | --- |',
                '| 待填写 | 待填写 | 待填写 | 待核实 |',
                '',
                '## 检索充分性',
                '',
                '- [ ] Hive Maps、Projects、相关系列/作者页已覆盖',
                '- [ ] 至少两个独立补充索引已覆盖或失败与替代来源已记录',
                '- [ ] 关卡名、章节代号、译名、战役名和合作玩法词已组合检索',
                '- [ ] 所有疑似候选的首发来源、版本链和硬条件均已闭环',
                '- [ ] 底图决策前 30 天内完成最后复查',
                '',
                '## 结论',
                '',
                '检索未完成。不得据此进入成品制作。'
            ) -join [Environment]::NewLine
            Write-Utf8File -Path (Join-Path $missionDirectory 'COMMUNITY_SEARCH.md') -Content $communitySearch

            $sourceEvaluation = @(
                "# $($mission.titleZh)：底图来源与成本决策",
                '',
                '前置条件：`COMMUNITY_SEARCH.md` 尚未通过检索充分性门。完成前不得作出底图决策。',
                '',
                '## 候选硬条件核验',
                '',
                '| 候选 | 功能与还原 | 1.24e-1.27b | 可编辑/可审计 | 修改与分发许可 | 结论 |',
                '| --- | --- | --- | --- | --- | --- |',
                '| 待填写 | 待核实 | 待核实 | 待核实 | 待核实 | 待核实 |',
                '',
                '## 同口径成本比较',
                '',
                '| 工作包 | 最佳社区候选：乐观/最可能/悲观 | 官方原图：乐观/最可能/悲观 | 证据与假设 |',
                '| --- | --- | --- | --- |',
                '| 取得与授权 | 待评估 | 待评估 | 待填写 |',
                '| 格式与版本兼容 | 待评估 | 待评估 | 待填写 |',
                '| 原版还原 | 待评估 | 待评估 | 待填写 |',
                '| 多人机制 | 待评估 | 待评估 | 待填写 |',
                '| 脚本与数据维护 | 待评估 | 待评估 | 待填写 |',
                '| 验证与修复 | 待评估 | 待评估 | 待填写 |',
                '| 工具与发布 | 待评估 | 待评估 | 待填写 |',
                '',
                '## 决策',
                '',
                '- 状态：未决策',
                '- 选择路线：待填写',
                '- 成本结论与不确定性：待填写',
                '- 决策日期与复核人：待填写',
                '- 重新评估触发条件：发现新候选、授权或兼容证据变化、实际成本越界'
            ) -join [Environment]::NewLine
            Write-Utf8File -Path (Join-Path $missionDirectory 'SOURCE_EVALUATION.md') -Content $sourceEvaluation

            foreach ($subdirectory in @(
                'original',
                'third-party',
                'workspace',
                'release'
            )) {
                $path = Join-Path $missionDirectory $subdirectory
                New-Item -ItemType Directory -Force -Path $path | Out-Null
                Write-Utf8File -Path (Join-Path $path '.gitkeep') -Content ''
            }
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
