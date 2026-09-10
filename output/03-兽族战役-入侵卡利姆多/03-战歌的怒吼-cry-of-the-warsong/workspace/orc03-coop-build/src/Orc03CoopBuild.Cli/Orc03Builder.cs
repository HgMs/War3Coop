using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using War3Net.Build;
using War3Net.Build.Extensions;
using War3Net.Build.Info;
using War3Net.Build.Object;
using War3Net.Build.Script;
using War3Net.CodeAnalysis.Jass;
using War3Net.Common.Extensions;
using War3Net.IO.Mpq;

namespace Orc03CoopBuild;

internal sealed record BuildResult(string OutputPath, string SourceSha256, string OutputSha256);

internal static class Orc03Builder
{
    private const string ExpectedSourceSha256 = "84ADDFD4D347B2A0A89947F47680CA4D968C8FB1DB3E649515C22CE3A659F239";
    private const int WarsongPlayerId = 3;

    public static BuildResult Build(string sourcePath, string outputPath)
    {
        var source = Path.GetFullPath(sourcePath);
        var output = Path.GetFullPath(outputPath);

        if (!File.Exists(source))
        {
            throw new FileNotFoundException("Official Orc03 map was not found.", source);
        }

        if (File.Exists(output))
        {
            throw new IOException($"Output already exists: {output}");
        }

        var sourceHash = Sha256(source);
        if (!sourceHash.Equals(ExpectedSourceSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Unsupported Orc03 source. Expected {ExpectedSourceSha256}, got {sourceHash}.");
        }

        var map = Map.Open(
            source,
            MapFiles.Info | MapFiles.TriggerStrings | MapFiles.Script | MapFiles.UnitObjectData);
        PatchInfo(map.Info ?? throw new InvalidDataException("war3map.w3i is missing."));
        PatchStrings(map);
        PatchUnitObjectData(map.UnitObjectData ?? throw new InvalidDataException("war3map.w3u is missing."));
        map.Script = PatchScript(map.Script ?? throw new InvalidDataException("war3map.j is missing."));
        _ = JassSyntaxFactory.ParseCompilationUnit(map.Script);

        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        using var sourceArchive = MpqArchive.Open(source, loadListFile: true);
        var builder = new MpqArchiveBuilder(sourceArchive);
        builder.AddFile(map.GetInfoFile() ?? throw new InvalidDataException("Failed to serialize war3map.w3i."));
        builder.AddFile(map.GetScriptFile() ?? throw new InvalidDataException("Failed to serialize war3map.j."));
        builder.AddFile(
            map.GetTriggerStringsFile(new UTF8Encoding(false, true))
            ?? throw new InvalidDataException("Failed to serialize war3map.wts."));
        builder.AddFile(
            map.GetUnitObjectDataFile(new UTF8Encoding(false, true))
            ?? throw new InvalidDataException("Failed to serialize war3map.w3u."));
        builder.SaveWithPreArchiveData(output);

        return new BuildResult(output, sourceHash, Sha256(output));
    }

    private static void PatchInfo(MapInfo info)
    {
        info.MapName = "战歌的怒吼 - 双人合作开发版";
        info.MapDescription = "玩家1控制萨尔与兽人部落；玩家2可控制格罗姆与完整战歌部落，或设置为电脑保留原版AI。";
        info.RecommendedPlayers = "2";

        var player = info.Players.SingleOrDefault(candidate => candidate.Id == WarsongPlayerId)
            ?? throw new InvalidDataException("Player(3) was not found in Orc03 map info.");
        if (player.Race != PlayerRace.Orc)
        {
            throw new InvalidDataException("Player(3) must retain the official Orc race.");
        }

        player.Controller = PlayerController.User;
    }

    private static void PatchStrings(Map map)
    {
        if (map.TriggerStrings is null)
        {
            throw new InvalidDataException("war3map.wts is missing.");
        }

        SetTriggerString(53, "战歌的怒吼 - 双人合作开发版");
        SetTriggerString(54, "2");
        SetTriggerString(55, "第三章：战歌的怒吼（双人合作开发版）");
        AddTriggerString(900, "选择难度");
        AddTriggerString(901, "普通");
        AddTriggerString(902, "困难");
        AddTriggerString(903, "正在等待玩家 1 选择难度（30 秒后默认普通）");
        AddTriggerString(904, "难度：普通");
        AddTriggerString(905, "难度：困难");
        AddTriggerString(906, "玩家 2 槽未设置为真人或电脑：该配置不受支持，战歌 AI 不会启动。单人游玩请在大厅将玩家 2 设置为电脑。");

        void SetTriggerString(uint key, string value)
        {
            var matches = map.TriggerStrings.Strings.Where(candidate => candidate.Key == key).ToArray();
            if (matches.Length == 0)
            {
                throw new InvalidDataException($"TRIGSTR_{key:000} is missing.");
            }

            foreach (var triggerString in matches)
            {
                triggerString.Value = value;
            }
        }

        void AddTriggerString(uint key, string value)
        {
            if (map.TriggerStrings.Strings.Any(candidate => candidate.Key == key))
            {
                throw new InvalidDataException($"TRIGSTR_{key:000} is already in use.");
            }

            map.TriggerStrings.Strings.Add(new TriggerString { Key = key, Value = value });
        }
    }

    private static void PatchUnitObjectData(UnitObjectData data)
    {
        SetBaseUnitInt(data, "Hamg", "ufoo", 0);
        SetBaseUnitInt(data, "Hpal", "ufoo", 0);
        SetBaseUnitInt(data, "Hmkg", "ufoo", 0);
    }

    private static void SetBaseUnitInt(UnitObjectData data, string unitId, string fieldId, int value)
    {
        var rawUnitId = unitId.FromRawcode();
        var rawFieldId = fieldId.FromRawcode();
        var unit = data.BaseUnits.SingleOrDefault(candidate => candidate.OldId == rawUnitId);
        if (unit is null)
        {
            unit = new SimpleObjectModification { OldId = rawUnitId, NewId = 0 };
            data.BaseUnits.Add(unit);
        }

        var existing = unit.Modifications.Where(candidate => candidate.Id == rawFieldId).ToArray();
        if (existing.Length > 1)
        {
            throw new InvalidDataException($"Duplicate {fieldId} modifications for {unitId}.");
        }

        var modification = existing.SingleOrDefault();
        if (modification is null)
        {
            modification = new SimpleObjectDataModification { Id = rawFieldId };
            unit.Modifications.Add(modification);
        }

        modification.Type = ObjectDataType.Int;
        modification.Value = value;
    }

    private static string PatchScript(string source)
    {
        var script = NormalizeToLf(source);

        RequireCount(script, "    call SetPlayerController( Player(3), MAP_CONTROL_COMPUTER )", 1);
        RequireCount(script, "    call StartCampaignAI( Player(3), \"o03_grom.ai\" )", 1);
        RequireCount(script, "call CommandAI( Player(3),", 15);
        RequireCount(script, "    call InitGameCacheBJ( \"Campaigns.w3v\" )", 1);
        RequireCount(script, "    call RestoreUnitLocFacingAngleBJ(", 2);
        RequireCount(script, "    call StoreUnitBJ(", 2);
        RequireCount(script, "    call SaveGameCacheBJ(", 1);
        RequireCount(script, "    call SetNextLevelBJ(", 1);
        RequireCount(script, "    call SetMissionAvailableBJ(", 1);
        RequireCount(script, "    call TriggerRegisterPlayerEventEndCinematic(", 2);
        RequireCount(script, "    call SelectUnitSingle( udg_Thrall )", 2);
        RequireCount(script, "bj_FORCE_PLAYER[0]", 8);
        RequireCount(
            script,
            "    call InitGlobals(  )\n    call InitCustomTriggers(  )\n    call RunInitializationTriggers(  )",
            1);

        script = script.Replace(
            "    boolean             udg_GameOver\n",
            "    boolean             udg_GameOver\n" +
            "    constant integer    COOP_WARSONG_PLAYER_ID    = 3\n" +
            "    boolean             udg_CoopP3Human           = false\n" +
            "    boolean             udg_CoopP3AI              = false\n" +
            "    boolean             udg_CoopResultIssued      = false\n" +
            "    dialog              udg_CoopDifficultyDialog  = null\n" +
            "    button              udg_CoopDifficultyNormal  = null\n" +
            "    button              udg_CoopDifficultyHard    = null\n" +
            "    trigger             udg_CoopDifficultyTrigger = null\n" +
            "    timer               udg_CoopDifficultyStartupTimer = null\n" +
            "    timer               udg_CoopDifficultyTimer   = null\n" +
            "    boolean             udg_CoopInitializationStarted = false\n",
            StringComparison.Ordinal);

        script = script.Replace(
            "    call SetPlayerController( Player(3), MAP_CONTROL_COMPUTER )",
            "    call SetPlayerController( Player(3), MAP_CONTROL_USER )",
            StringComparison.Ordinal);

        foreach (var playerId in new[] { 0, 1, 2, 3, 8, 9, 10, 11 })
        {
            var selectable = $"    call SetPlayerRaceSelectable( Player({playerId}), true )";
            RequireCount(script, selectable, 1);
            script = script.Replace(
                selectable,
                $"    call SetPlayerRaceSelectable( Player({playerId}), false )",
                StringComparison.Ordinal);
        }

        foreach (var playerId in new[] { 0, 2 })
        {
            var anchor = $"    call SetPlayerTechMaxAllowed( Player({playerId}), 'Hmkg', 0 )";
            RequireCount(script, anchor, 1);
            script = script.Replace(
                anchor,
                anchor + $"\n    call SetPlayerTechMaxAllowed( Player({playerId}), 'Oshd', 0 )",
                StringComparison.Ordinal);
        }

        script = AddTechRestrictions(
            script,
            "InitTechTree_Player0",
            0,
            ["ospw", "otbk", "otbr", "ovln", "Rolf", "Rorb", "Robk", "Ropm", "Robf"]);
        var humanP3TechParityFunction = BuildHumanP3TechParityFunction(script);

        script = script.Replace(
            "    call SetGamePlacement( MAP_PLACEMENT_USE_MAP_SETTINGS )",
            "    call SetGamePlacement( MAP_PLACEMENT_USE_MAP_SETTINGS )\n" +
            "    call SetMapFlag( MAP_LOCK_ALLIANCE_CHANGES, false )\n" +
            "    call SetMapFlag( MAP_ALLIANCE_CHANGES_HIDDEN, false )\n" +
            "    call SetMapFlag( MAP_LOCK_RESOURCE_TRADING, false )\n" +
            "    call SetMapFlag( MAP_RESOURCE_TRADING_ALLIES_ONLY, true )",
            StringComparison.Ordinal);

        script = script.Replace(
            "    call InitGlobals(  )\n    call InitCustomTriggers(  )\n    call RunInitializationTriggers(  )",
            "    call InitGlobals(  )\n    call CoopClassifyP3(  )\n    call CoopApplyHumanP3TechParity(  )\n    call CoopScheduleDifficultySelection(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call SetCampaignMenuRaceBJ( bj_CAMPAIGN_INDEX_O )\n",
            string.Empty,
            StringComparison.Ordinal);

        script = ReplaceFunction(
            script,
            "Trig_Load_Thrall_Actions",
            """
function Trig_Load_Thrall_Actions takes nothing returns nothing
    call CreateNUnitsAtLoc( 1, 'Othr', Player(0), GetRectCenter(gg_rct_InitialThrallLocation), 0.00 )
    set udg_Thrall = GetLastCreatedUnit()
    call SetPlayerHandicapXPBJ( Player(0), 75.00 )
    call SetHeroLevel( udg_Thrall, 3, false )
    call SelectHeroSkill( udg_Thrall, 'AOcl' )
    call SelectHeroSkill( udg_Thrall, 'AOsf' )
    call SelectHeroSkill( udg_Thrall, 'AOfs' )
    call UnitAddItemByIdSwapped( 'rin1', udg_Thrall )
    call UnitAddItemByIdSwapped( 'rst1', udg_Thrall )
    call UnitAddItemByIdSwapped( 'penr', udg_Thrall )
    call UnitAddItemByIdSwapped( 'hlst', udg_Thrall )
    call UnitAddItemByIdSwapped( 'whwd', udg_Thrall )
    call UnitAddItemByIdSwapped( 'shea', udg_Thrall )
endfunction
""");
        script = ReplaceFunction(
            script,
            "Trig_Load_Grom_Actions",
            """
function Trig_Load_Grom_Actions takes nothing returns nothing
    call CreateNUnitsAtLoc( 1, 'Ogrh', Player(3), GetRectCenter(gg_rct_InitialGromLocation), 225.00 )
    set udg_Grom = GetLastCreatedUnit()
    call SetHeroLevel( udg_Grom, 4, false )
    call SelectHeroSkill( udg_Grom, 'AOcr' )
    call SelectHeroSkill( udg_Grom, 'AOcr' )
    call SelectHeroSkill( udg_Grom, 'AOmi' )
    call SelectHeroSkill( udg_Grom, 'AOwk' )
    call SuspendHeroXPBJ( false, udg_Grom )
endfunction
""");
        script = script.Replace(
            "    call InitGameCacheBJ( \"Campaigns.w3v\" )\n",
            string.Empty,
            StringComparison.Ordinal);

        script = ReplaceFunction(
            script,
            "Trig_GromGold_Actions",
            """
function Trig_GromGold_Actions takes nothing returns nothing
    if ( not udg_CoopP3AI ) then
        return
    endif
    call AdjustPlayerStateBJ( 1000, Player(3), PLAYER_STATE_RESOURCE_GOLD )
endfunction
""");
        script = ReplaceFunction(
            script,
            "Trig_GromLumber_Actions",
            """
function Trig_GromLumber_Actions takes nothing returns nothing
    if ( not udg_CoopP3AI ) then
        return
    endif
    call AdjustPlayerStateBJ( 1000, Player(3), PLAYER_STATE_RESOURCE_LUMBER )
endfunction
""");
        script = ReplaceFunction(
            script,
            "Trig_ActivateGromAI_Actions",
            """
function Trig_ActivateGromAI_Actions takes nothing returns nothing
    if ( not udg_CoopP3AI or udg_GromAIActive ) then
        return
    endif
    if ( Trig_ActivateGromAI_Func001001() ) then
        call ReviveHeroLoc( udg_Grom, GetRectCenter(gg_rct_InitialGromLocation), true )
    else
        call DoNothing(  )
    endif
    call StartCampaignAI( Player(3), "o03_grom.ai" )
    set udg_GromAIActive = true
endfunction
""");

        script = script.Replace(
            "call CommandAI( Player(3),",
            "call CoopCommandGromAI(",
            StringComparison.Ordinal);
        script = InsertEarlyReturn(script, "Trig_Player_Attacks_Grom_Conditions", returnsBoolean: true, "not udg_CoopP3AI");
        script = InsertEarlyReturn(script, "Trig_Player_Attacks_Teal_Conditions", returnsBoolean: true, "not udg_CoopP3AI");
        script = InsertEarlyReturn(script, "Trig_GromAggrosPlayerCheck_Actions", returnsBoolean: false, "not udg_CoopP3AI");
        script = InsertEarlyReturn(script, "Trig_GromAttacksBrownHeartbeat_Actions", returnsBoolean: false, "not udg_CoopP3AI");

        script = script.Replace(
            "    call RemoveAllGuardPositions( Player(3) )",
            "    if ( not udg_CoopP3Human ) then\n" +
            "        call RemoveAllGuardPositions( Player(3) )\n" +
            "    endif",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call ForGroup( GetUnitsOfPlayerAll(Player(3)), function Trig_ExitPositionActors_Func013002 )",
            "    if ( not udg_CoopP3Human ) then\n" +
            "        call ForGroup( GetUnitsOfPlayerAll(Player(3)), function Trig_ExitPositionActors_Func013002 )\n" +
            "    endif",
            StringComparison.Ordinal);

        const string endingAlliance =
            "    call SetPlayerAllianceStateBJ( Player(0), Player(3), bj_ALLIANCE_ALLIED_VISION )\n" +
            "    call SetPlayerAllianceStateBJ( Player(0), Player(2), bj_ALLIANCE_ALLIED_VISION )\n" +
            "    call SetPlayerAllianceStateBJ( Player(3), Player(0), bj_ALLIANCE_ALLIED_VISION )\n" +
            "    call SetPlayerAllianceStateBJ( Player(2), Player(0), bj_ALLIANCE_ALLIED_VISION )";
        RequireCount(script, endingAlliance, 1);
        script = script.Replace(endingAlliance, "    call CoopRestoreEndingAlliance(  )", StringComparison.Ordinal);
        script = AppendToFunction(script, "Trig_Initial_Alliances_Actions", "    call CoopApplyHumanP3Diplomacy(  )");

        script = script.Replace("bj_FORCE_PLAYER[0]", "GetPlayersAll()", StringComparison.Ordinal);
        script = DuplicateMatchingLinesForHumanP3(
            script,
            line => CameraFunctions.Any(cameraFunction => line.Contains(cameraFunction, StringComparison.Ordinal)) &&
                    line.Contains("Player(0)", StringComparison.Ordinal));
        script = DuplicateMatchingLinesForHumanP3(
            script,
            line => line.Contains("TriggerRegisterPlayerEventEndCinematic", StringComparison.Ordinal) &&
                    line.Contains("Player(0)", StringComparison.Ordinal));
        script = script.Replace(
            "    call SelectUnitSingle( udg_Thrall )",
            "    call CoopSelectStartingHeroes(  )",
            StringComparison.Ordinal);

        for (var index = 1; index <= 6; index++)
        {
            var rect = $"gg_rct_VisibilityRoad{index:00}";
            var vision =
                $"    call CreateFogModifierRadiusLocBJ( true, Player(0), FOG_OF_WAR_VISIBLE, GetRectCenter({rect}), 896.00 )\n" +
                "    call DestroyFogModifier( GetLastCreatedFogModifier() )";
            RequireCount(script, vision, 1);
            script = script.Replace(
                vision,
                vision + "\n" +
                "    if ( udg_CoopP3Human ) then\n" +
                $"        call CreateFogModifierRadiusLocBJ( true, Player(3), FOG_OF_WAR_VISIBLE, GetRectCenter({rect}), 896.00 )\n" +
                "        call DestroyFogModifier( GetLastCreatedFogModifier() )\n" +
                "    endif",
                StringComparison.Ordinal);
        }

        const string laboratoryVision =
            "    call CreateFogModifierRadiusLocBJ( true, Player(0), FOG_OF_WAR_VISIBLE, GetRectCenter(gg_rct_PingGoblinLab), 1536.00 )\n" +
            "    set udg_LaboratoryVis = GetLastCreatedFogModifier()\n" +
            "    call DestroyFogModifier( udg_LaboratoryVis )";
        RequireCount(script, laboratoryVision, 1);
        script = script.Replace(
            laboratoryVision,
            laboratoryVision + "\n" +
            "    if ( udg_CoopP3Human ) then\n" +
            "        call CreateFogModifierRadiusLocBJ( true, Player(3), FOG_OF_WAR_VISIBLE, GetRectCenter(gg_rct_PingGoblinLab), 1536.00 )\n" +
            "        set udg_LaboratoryVis = GetLastCreatedFogModifier()\n" +
            "        call DestroyFogModifier( udg_LaboratoryVis )\n" +
            "    endif",
            StringComparison.Ordinal);

        script = ReplaceFunction(
            script,
            "Trig_Level_Enable_Actions",
            """
function Trig_Level_Enable_Actions takes nothing returns nothing
endfunction
""");
        script = ReplaceFunction(
            script,
            "Trig_Level_Victory_Actions",
            """
function Trig_Level_Victory_Actions takes nothing returns nothing
    call CoopVictory(  )
endfunction
""");
        script = ReplaceFunction(
            script,
            "Trig_Level_Defeat_Actions",
            """
function Trig_Level_Defeat_Actions takes nothing returns nothing
    call CoopDefeat( "TRIGSTR_010" )
endfunction
""");

        script = script.Replace(
            "function InitGlobals takes nothing returns nothing\n",
            NormalizeToLf(CoopFunctions) + "\n" + humanP3TechParityFunction + "\nfunction InitGlobals takes nothing returns nothing\n",
            StringComparison.Ordinal);
        script = script.Replace(
            "function main takes nothing returns nothing\n",
            NormalizeToLf(CoopDifficultyFunctions) + "\nfunction main takes nothing returns nothing\n",
            StringComparison.Ordinal);

        RequireCount(script, "GetPlayerController(warsongPlayer)", 1);
        RequireCount(script, "GetPlayerSlotState(warsongPlayer)", 1);
        RequireCount(script, "call SetPlayerController( Player(3), MAP_CONTROL_USER )", 1);
        RequireCount(script, "call StartCampaignAI( Player(3), \"o03_grom.ai\" )", 1);
        RequireCount(script, "call CommandAI( Player(3), command, data )", 1);
        RequireCount(script, "call CoopCommandGromAI(", 15);
        RequireCount(script, "call CoopClassifyP3(  )", 1);
        RequireCount(script, "call CoopApplyHumanP3TechParity(  )", 1);
        RequireCount(script, "call CoopScheduleDifficultySelection(  )", 1);
        RequireCount(script, "call InitCustomTriggers(  )", 1);
        RequireCount(script, "call RunInitializationTriggers(  )", 1);
        RequireCount(script, "call CoopVictory(  )", 1);
        RequireCount(script, "call CoopDefeat( \"TRIGSTR_010\" )", 1);
        RequireCount(script, "EVENT_PLAYER_LEAVE", 0);
        RequireCount(script, "InitGameCacheBJ(", 0);
        RequireCount(script, "RestoreUnitLocFacingAngleBJ(", 0);
        RequireCount(script, "StoreUnitBJ(", 0);
        RequireCount(script, "SaveGameCacheBJ(", 0);
        RequireCount(script, "SetMissionAvailableBJ(", 0);
        RequireCount(script, "SetNextLevelBJ(", 0);
        RequireCount(script, "bj_FORCE_PLAYER[0]", 0);
        foreach (var playerId in new[] { 0, 1, 2, 3, 8, 9, 10, 11 })
        {
            RequireCount(script, $"call SetPlayerRaceSelectable( Player({playerId}), false )", 1);
            RequireCount(script, $"call SetPlayerRaceSelectable( Player({playerId}), true )", 0);
        }
        foreach (var playerId in new[] { 0, 2 })
        {
            RequireCount(script, $"call SetPlayerTechMaxAllowed( Player({playerId}), 'Oshd', 0 )", 1);
        }
        foreach (var rawcode in new[] { "ospw", "otbk", "otbr", "ovln", "Rolf", "Rorb", "Robk", "Ropm", "Robf" })
        {
            RequireCount(script, $"call SetPlayerTechMaxAllowed( Player(0), '{rawcode}', 0 )", 1);
            RequireCount(script, $"call SetPlayerTechMaxAllowed( Player(3), '{rawcode}', 0 )", 1);
        }
        foreach (var rawcode in new[] { "rin1", "rst1", "penr", "hlst", "whwd", "shea" })
        {
            RequireCount(script, $"call UnitAddItemByIdSwapped( '{rawcode}', udg_Thrall )", 1);
        }
        RequireCount(script, "SetItemCharges(", 0);
        RequireCount(script, "function InitUpgrades_Player3 takes nothing returns nothing", 0);
        RequireCount(script, "call SetPlayerAllianceStateBJ( Player(3), Player(PLAYER_NEUTRAL_AGGRESSIVE), bj_ALLIANCE_UNALLIED )", 1);
        RequireCount(script, "call SetPlayerAllianceStateBJ( Player(PLAYER_NEUTRAL_AGGRESSIVE), Player(3), bj_ALLIANCE_UNALLIED )", 1);

        return NormalizeToLf(script).Replace("\n", "\r\n", StringComparison.Ordinal);
    }

    private static string ReplaceFunction(string script, string functionName, string replacement)
    {
        var pattern = $@"function {Regex.Escape(functionName)} takes nothing returns nothing\n.*?\nendfunction";
        var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.CultureInvariant);
        var count = regex.Matches(script).Count;
        if (count != 1)
        {
            throw new InvalidDataException($"Expected one function named '{functionName}', found {count}.");
        }

        return regex.Replace(script, replacement.TrimEnd());
    }

    private static string AddTechRestrictions(
        string script,
        string functionName,
        int playerId,
        IReadOnlyCollection<string> rawcodes)
    {
        var pattern = $@"function {Regex.Escape(functionName)} takes nothing returns nothing\n.*?\nendfunction";
        var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.CultureInvariant);
        var matches = regex.Matches(script);
        if (matches.Count != 1)
        {
            throw new InvalidDataException($"Expected one function named '{functionName}', found {matches.Count}.");
        }

        var body = matches[0].Value;
        var additions = new StringBuilder();
        foreach (var rawcode in rawcodes)
        {
            var call = $"    call SetPlayerTechMaxAllowed( Player({playerId}), '{rawcode}', 0 )";
            if (body.Contains(call, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Tech restriction already exists in {functionName}: {rawcode}.");
            }
            additions.Append('\n').Append(call);
        }

        var replacement = body.Replace("\nendfunction", additions + "\nendfunction", StringComparison.Ordinal);
        return regex.Replace(script, replacement);
    }

    private static string BuildHumanP3TechParityFunction(string script)
    {
        var calls = new List<string>();
        foreach (var sourceFunction in new[] { "InitUpgrades_Player0", "InitTechTree_Player0" })
        {
            var pattern = $@"function {Regex.Escape(sourceFunction)} takes nothing returns nothing\n(?<body>.*?)\nendfunction";
            var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.CultureInvariant);
            var matches = regex.Matches(script);
            if (matches.Count != 1)
            {
                throw new InvalidDataException($"Expected one function named '{sourceFunction}', found {matches.Count}.");
            }

            calls.AddRange(
                matches[0].Groups["body"].Value
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.TrimStart().StartsWith("call ", StringComparison.Ordinal))
                    .Select(line => "    " + line.Replace("Player(0)", "Player(3)", StringComparison.Ordinal)));
        }

        return "function CoopApplyHumanP3TechParity takes nothing returns nothing\n" +
               "    if ( udg_CoopP3Human ) then\n" +
               string.Join("\n", calls) + "\n" +
               "    endif\n" +
               "endfunction\n";
    }

    private static string AppendToFunction(string script, string functionName, string line)
    {
        var pattern = $@"(function {Regex.Escape(functionName)} takes nothing returns nothing\n.*?)(\nendfunction)";
        var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.CultureInvariant);
        var count = regex.Matches(script).Count;
        if (count != 1)
        {
            throw new InvalidDataException($"Expected one function named '{functionName}', found {count}.");
        }

        return regex.Replace(script, match => match.Groups[1].Value + "\n" + line + match.Groups[2].Value);
    }

    private static string InsertEarlyReturn(
        string script,
        string functionName,
        bool returnsBoolean,
        string condition)
    {
        var signature = $"function {functionName} takes nothing returns {(returnsBoolean ? "boolean" : "nothing")}\n";
        RequireCount(script, signature, 1);
        var result = returnsBoolean ? "false" : string.Empty;
        var returnLine = returnsBoolean ? $"        return {result}\n" : "        return\n";
        return script.Replace(
            signature,
            signature + $"    if ( {condition} ) then\n" + returnLine + "    endif\n",
            StringComparison.Ordinal);
    }

    private static string DuplicateMatchingLinesForHumanP3(string script, Func<string, bool> predicate)
    {
        var lines = script.Split('\n');
        var output = new List<string>(lines.Length + 64);
        foreach (var line in lines)
        {
            output.Add(line);
            if (!predicate(line))
            {
                continue;
            }

            var indent = line[..(line.Length - line.TrimStart().Length)];
            output.Add(indent + "if ( udg_CoopP3Human ) then");
            output.Add("    " + ReplaceFirst(line, "Player(0)", "Player(3)"));
            output.Add(indent + "endif");
        }

        return string.Join("\n", output);
    }

    private static string ReplaceFirst(string value, string source, string replacement)
    {
        var index = value.IndexOf(source, StringComparison.Ordinal);
        return index < 0
            ? value
            : value[..index] + replacement + value[(index + source.Length)..];
    }

    private static string NormalizeToLf(string value)
    {
        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
    }

    private static void RequireCount(string text, string value, int expected)
    {
        var actual = text.Split(value, StringSplitOptions.None).Length - 1;
        if (actual != expected)
        {
            throw new InvalidDataException($"Expected {expected} occurrence(s) of '{value}', found {actual}.");
        }
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static readonly string[] CameraFunctions =
    [
        "CameraSetupApplyForPlayer",
        "CameraClearNoiseForPlayer",
        "CameraSetTargetNoiseForPlayer",
        "CameraSetSourceNoiseForPlayer",
        "SetCameraQuickPositionLocForPlayer",
        "ResetToGameCameraForPlayer",
        "PanCameraToTimedLocForPlayer",
        "SetCameraTargetControllerNoZForPlayer",
    ];

    private const string CoopFunctions = """
//===========================================================================
// Cooperative helpers injected by orc03-coop-build.
//===========================================================================
function CoopClassifyP3 takes nothing returns nothing
    local player warsongPlayer = Player(COOP_WARSONG_PLAYER_ID)
    local mapcontrol controller = GetPlayerController(warsongPlayer)
    local playerslotstate slotState = GetPlayerSlotState(warsongPlayer)
    set udg_CoopP3Human = ( controller == MAP_CONTROL_USER and slotState == PLAYER_SLOT_STATE_PLAYING )
    set udg_CoopP3AI = ( controller == MAP_CONTROL_COMPUTER and slotState == PLAYER_SLOT_STATE_PLAYING )
    set warsongPlayer = null
endfunction

function CoopApplyHumanP3Diplomacy takes nothing returns nothing
    if ( udg_CoopP3Human ) then
        call SetPlayerAllianceStateAllyBJ( Player(0), Player(3), true )
        call SetPlayerAllianceStateAllyBJ( Player(3), Player(0), true )
        call SetPlayerAlliance( Player(0), Player(3), ALLIANCE_SHARED_VISION, true )
        call SetPlayerAlliance( Player(3), Player(0), ALLIANCE_SHARED_VISION, true )
        call SetPlayerState( Player(0), PLAYER_STATE_ALLIED_VICTORY, 1 )
        call SetPlayerState( Player(3), PLAYER_STATE_ALLIED_VICTORY, 1 )
        call SetPlayerAllianceStateBJ( Player(3), Player(PLAYER_NEUTRAL_AGGRESSIVE), bj_ALLIANCE_UNALLIED )
        call SetPlayerAllianceStateBJ( Player(PLAYER_NEUTRAL_AGGRESSIVE), Player(3), bj_ALLIANCE_UNALLIED )
    endif
endfunction

function CoopRestoreEndingAlliance takes nothing returns nothing
    if ( udg_CoopP3Human ) then
        call SetPlayerAllianceStateAllyBJ( Player(0), Player(3), true )
        call SetPlayerAllianceStateAllyBJ( Player(3), Player(0), true )
        call SetPlayerAlliance( Player(0), Player(3), ALLIANCE_SHARED_VISION, true )
        call SetPlayerAlliance( Player(3), Player(0), ALLIANCE_SHARED_VISION, true )
        call SetPlayerState( Player(0), PLAYER_STATE_ALLIED_VICTORY, 1 )
        call SetPlayerState( Player(3), PLAYER_STATE_ALLIED_VICTORY, 1 )
        call SetPlayerAllianceStateBJ( Player(0), Player(2), bj_ALLIANCE_ALLIED_VISION )
        call SetPlayerAllianceStateBJ( Player(2), Player(0), bj_ALLIANCE_ALLIED_VISION )
    else
        call SetPlayerAllianceStateBJ( Player(0), Player(3), bj_ALLIANCE_ALLIED_VISION )
        call SetPlayerAllianceStateBJ( Player(0), Player(2), bj_ALLIANCE_ALLIED_VISION )
        call SetPlayerAllianceStateBJ( Player(3), Player(0), bj_ALLIANCE_ALLIED_VISION )
        call SetPlayerAllianceStateBJ( Player(2), Player(0), bj_ALLIANCE_ALLIED_VISION )
    endif
endfunction

function CoopCommandGromAI takes integer command, integer data returns nothing
    if ( udg_CoopP3AI ) then
        call CommandAI( Player(3), command, data )
    endif
endfunction

function CoopSelectStartingHeroes takes nothing returns nothing
    call SelectUnitForPlayerSingle( udg_Thrall, Player(0) )
    if ( udg_CoopP3Human ) then
        call SelectUnitForPlayerSingle( udg_Grom, Player(3) )
    endif
endfunction

function CoopVictory takes nothing returns nothing
    if ( not udg_CoopResultIssued ) then
        set udg_CoopResultIssued = true
        set udg_GameOver = true
        call CoopApplyHumanP3Diplomacy(  )
        call CustomVictoryBJ( Player(0), true, true )
        if ( udg_CoopP3Human ) then
            call CustomVictoryBJ( Player(3), true, true )
        endif
    endif
endfunction

function CoopDefeat takes string message returns nothing
    if ( not udg_CoopResultIssued ) then
        set udg_CoopResultIssued = true
        set udg_GameOver = true
        call CoopApplyHumanP3Diplomacy(  )
        call CustomDefeatBJ( Player(0), message )
        if ( udg_CoopP3Human ) then
            call CustomDefeatBJ( Player(3), message )
        endif
    endif
endfunction
""";

    private const string CoopDifficultyFunctions = """
//===========================================================================
// Difficulty selection runs after main returns and before original triggers.
//===========================================================================
function CoopFinishDifficultySelection takes gamedifficulty difficulty returns nothing
    if ( udg_CoopInitializationStarted ) then
        return
    endif
    set udg_CoopInitializationStarted = true
    call SetGameDifficulty( difficulty )
    call PauseTimer( udg_CoopDifficultyTimer )
    call DialogDisplay( Player(0), udg_CoopDifficultyDialog, false )
    call DisableTrigger( udg_CoopDifficultyTrigger )
    call DestroyTrigger( udg_CoopDifficultyTrigger )
    call DestroyTimer( udg_CoopDifficultyTimer )
    call DialogClear( udg_CoopDifficultyDialog )
    call DialogDestroy( udg_CoopDifficultyDialog )
    set udg_CoopDifficultyTrigger = null
    set udg_CoopDifficultyTimer = null
    set udg_CoopDifficultyDialog = null
    set udg_CoopDifficultyNormal = null
    set udg_CoopDifficultyHard = null
    call InitCustomTriggers(  )
    call RunInitializationTriggers(  )
    if ( not udg_CoopP3Human and not udg_CoopP3AI ) then
        call DisplayTimedTextToPlayer( Player(0), 0.00, 0.00, 20.00, GetLocalizedString("TRIGSTR_906") )
    endif
endfunction

function CoopChooseDifficulty takes nothing returns nothing
    if ( GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED ) then
        call DisplayTimedTextToPlayer( Player(0), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_904") )
        if ( udg_CoopP3Human ) then
            call DisplayTimedTextToPlayer( Player(3), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_904") )
        endif
        call CoopFinishDifficultySelection( MAP_DIFFICULTY_NORMAL )
    elseif ( GetClickedButton() == udg_CoopDifficultyHard ) then
        call DisplayTimedTextToPlayer( Player(0), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_905") )
        if ( udg_CoopP3Human ) then
            call DisplayTimedTextToPlayer( Player(3), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_905") )
        endif
        call CoopFinishDifficultySelection( MAP_DIFFICULTY_HARD )
    else
        call DisplayTimedTextToPlayer( Player(0), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_904") )
        if ( udg_CoopP3Human ) then
            call DisplayTimedTextToPlayer( Player(3), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_904") )
        endif
        call CoopFinishDifficultySelection( MAP_DIFFICULTY_NORMAL )
    endif
endfunction

function CoopBeginDifficultySelection takes nothing returns nothing
    call DestroyTimer( udg_CoopDifficultyStartupTimer )
    set udg_CoopDifficultyStartupTimer = null
    set udg_CoopDifficultyDialog = DialogCreate()
    call DialogSetMessage( udg_CoopDifficultyDialog, GetLocalizedString("TRIGSTR_900") )
    set udg_CoopDifficultyNormal = DialogAddButton( udg_CoopDifficultyDialog, GetLocalizedString("TRIGSTR_901"), 0 )
    set udg_CoopDifficultyHard = DialogAddButton( udg_CoopDifficultyDialog, GetLocalizedString("TRIGSTR_902"), 0 )
    set udg_CoopDifficultyTrigger = CreateTrigger()
    set udg_CoopDifficultyTimer = CreateTimer()
    call TriggerRegisterDialogEvent( udg_CoopDifficultyTrigger, udg_CoopDifficultyDialog )
    call TriggerRegisterTimerExpireEvent( udg_CoopDifficultyTrigger, udg_CoopDifficultyTimer )
    call TriggerAddAction( udg_CoopDifficultyTrigger, function CoopChooseDifficulty )
    call DialogDisplay( Player(0), udg_CoopDifficultyDialog, true )
    if ( udg_CoopP3Human ) then
        call DisplayTimedTextToPlayer( Player(3), 0.00, 0.00, 30.00, GetLocalizedString("TRIGSTR_903") )
    endif
    call TimerStart( udg_CoopDifficultyTimer, 30.00, false, null )
endfunction

function CoopScheduleDifficultySelection takes nothing returns nothing
    set udg_CoopDifficultyStartupTimer = CreateTimer()
    call TimerStart( udg_CoopDifficultyStartupTimer, 0.01, false, function CoopBeginDifficultySelection )
endfunction
""";
}
