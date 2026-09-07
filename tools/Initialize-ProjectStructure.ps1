[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $projectRoot 'docs/campaigns.json'
$outputRoot = Join-Path $projectRoot 'output'
$gameRoot = Join-Path $projectRoot 'game'
$versions = @(
    '1.20e',
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

        $originalReadme = @(
            "# 原版关卡说明：$($mission.titleZh)",
            '',
            "- 所属游戏：$($campaign.game)",
            "- 所属战役：$($campaign.faction)战役《$($campaign.titleZh)》",
            "- 章节编号：$($mission.number)",
            "- 英文名：$($mission.titleEn)",
            "- 类型：$($mission.type)",
            '',
            '## 关卡概述',
            '',
            $mission.summary,
            '',
            '## 对照状态',
            '',
            '当前说明根据公开的经典战役章节清单和剧情概述建立。解压并确认 1.20e 与 1.31 游戏数据后，需进一步核对地图内任务文本、单位配置、过场、物品、难度差异和官方中文名称。'
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
                '- 兼容目标：1.20e-1.27b 必须支持；1.31 尽力支持',
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
                '- [ ] 社区作品检索与许可核验完成',
                '- [ ] 多人化方案确定',
                '- [ ] 1.20e-1.27b 完整回归通过',
                '- [ ] 1.31 兼容性验证完成',
                '- [ ] 成品与变更说明进入 release/'
            ) -join [Environment]::NewLine

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
