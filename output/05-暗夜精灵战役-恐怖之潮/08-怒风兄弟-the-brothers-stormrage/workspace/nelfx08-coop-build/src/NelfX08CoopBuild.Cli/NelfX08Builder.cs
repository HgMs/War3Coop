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

namespace NelfX08CoopBuild;

internal sealed record BuildResult(string OutputPath, string SourceSha256, string OutputSha256);

internal static class NelfX08Builder
{
    private const string ExpectedSourceSha256 = "AC942B38F7E7F4C1EDA45DE0512667FAE91E5CD36D902FCBE1C1BBFB96B363E0";

    public static BuildResult Build(string sourcePath, string outputPath)
    {
        var source = Path.GetFullPath(sourcePath);
        var output = Path.GetFullPath(outputPath);

        if (!File.Exists(source))
        {
            throw new FileNotFoundException("Official NightElfX08 map was not found.", source);
        }

        if (File.Exists(output))
        {
            throw new IOException($"Output already exists: {output}");
        }

        var sourceHash = Sha256(source);
        if (!sourceHash.Equals(ExpectedSourceSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Unsupported NightElfX08 source. Expected {ExpectedSourceSha256}, got {sourceHash}.");
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
        info.MapName = "怒风兄弟 - 双人合作开发版";
        info.MapDescription = "玩家1控制尤迪安与娜迦；玩家2控制玛尔法里奥与暗夜精灵。第二槽无人时恢复原版单人共享控制。";
        info.RecommendedPlayers = "2";

        var player = info.Players.SingleOrDefault(candidate => candidate.Id == 3)
            ?? throw new InvalidDataException("Player(3) was not found in NightElfX08 map info.");
        if (player.Race != PlayerRace.Orc)
        {
            throw new InvalidDataException("Player(3) must retain the official fixed race metadata.");
        }

        player.Controller = PlayerController.User;
        player.Race = PlayerRace.NightElf;
    }

    private static void PatchStrings(Map map)
    {
        if (map.TriggerStrings is null)
        {
            throw new InvalidDataException("war3map.wts is missing.");
        }

        SetTriggerString(6, "怒风兄弟 - 双人合作开发版");
        SetTriggerString(7, "2");
        SetTriggerString(8, "第八章：怒风兄弟（双人合作开发版）");
        AddTriggerString(900, "选择难度");
        AddTriggerString(901, "普通");
        AddTriggerString(902, "困难");
        AddTriggerString(903, "正在等待玩家 1 选择难度（30 秒后默认普通）");
        AddTriggerString(904, "难度：普通");
        AddTriggerString(905, "难度：困难");
        AddTriggerString(906, "第二槽未由活跃真人占用：已启用原版单人共享控制回退。");

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
        SetBaseUnitInt(data, "Udea", "ufoo", 0);
        SetBaseUnitInt(data, "Udre", "ufoo", 0);
        SetBaseUnitInt(data, "Ulic", "ufoo", 0);
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

        RequireCount(script, "    call SetPlayerController( Player(3), MAP_CONTROL_NEUTRAL )", 1);
        RequireCount(script, "    call StartMeleeAI( Player(2), \"N08X03.ai\" )", 1);
        RequireCount(script, "    call TriggerRegisterPlayerEventEndCinematic(", 3);
        RequireCount(script, "    call InitGlobals(  )\n    call InitCustomTriggers(  )\n    call RunInitializationTriggers(  )", 1);
        RequireCount(script, "    call SelectUnitSingle( udg_Illidan )", 1);
        RequireCount(script, "    set udg_SelectedUnits = GetUnitsSelectedAll(udg_AP2_BluePlayer)", 2);
        RequireCount(script, "    call SelectGroupBJ( udg_SelectedUnits )", 2);

        script = script.Replace(
            "    boolean                 udg_GameOver               = false\n",
            "    boolean                 udg_GameOver               = false\n" +
            "    boolean                 udg_CoopP3Human            = false\n" +
            "    boolean                 udg_CoopResultIssued       = false\n" +
            "    boolean                 udg_CoopIntroCleanupDone   = false\n" +
            "    boolean                 udg_CoopDamCleanupDone     = false\n" +
            "    boolean                 udg_CoopDamMiniCleanupDone = false\n" +
            "    boolean                 udg_CoopInitializationStarted = false\n" +
            "    force                   udg_CoopHumanForce         = null\n" +
            "    group                   udg_CoopP3SelectedUnits    = null\n" +
            "    dialog                  udg_CoopDifficultyDialog   = null\n" +
            "    button                  udg_CoopDifficultyNormal   = null\n" +
            "    button                  udg_CoopDifficultyHard     = null\n" +
            "    trigger                 udg_CoopDifficultyTrigger  = null\n" +
            "    timer                   udg_CoopDifficultyStartupTimer = null\n" +
            "    timer                   udg_CoopDifficultyTimer    = null\n",
            StringComparison.Ordinal);

        script = script.Replace(
            "    call SetPlayerController( Player(3), MAP_CONTROL_NEUTRAL )",
            "    call SetPlayerController( Player(3), MAP_CONTROL_USER )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call SetPlayerRacePreference( Player(3), RACE_PREF_ORC )",
            "    call SetPlayerRacePreference( Player(3), RACE_PREF_NIGHTELF )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call InitGlobals(  )\n    call InitCustomTriggers(  )\n    call RunInitializationTriggers(  )",
            "    call InitGlobals(  )\n    call CoopClassifyP3(  )\n    call CoopScheduleDifficultySelection(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call SetGamePlacement( MAP_PLACEMENT_USE_MAP_SETTINGS )",
            "    call SetGamePlacement( MAP_PLACEMENT_USE_MAP_SETTINGS )\n" +
            "    call SetMapFlag( MAP_LOCK_ALLIANCE_CHANGES, false )\n" +
            "    call SetMapFlag( MAP_ALLIANCE_CHANGES_HIDDEN, false )\n" +
            "    call SetMapFlag( MAP_LOCK_RESOURCE_TRADING, false )\n" +
            "    call SetMapFlag( MAP_RESOURCE_TRADING_ALLIES_ONLY, true )",
            StringComparison.Ordinal);

        script = ReplaceFunction(script, "Trig_Load_Heroes_Actions", LoadHeroesFunction);
        script = ReplaceFunction(script, "Trig_Load_Tyrande_Actions", LoadTyrandeFunction);
        script = ReplaceFunction(script, "Trig_Load_Furion_Actions", LoadFurionFunction);
        script = ReplaceFunction(script, "Trig_Load_Illidan_Actions", LoadIllidanFunction);
        script = ReplaceFunction(script, "Trig_Load_Magroth_Actions", EmptyMagrothFunction);
        script = ReplaceFunction(script, "Trig_Load_Maiev_Actions", LoadMaievItemsFunction);
        script = ReplaceFunction(script, "Trig_Next_Level_Prep_Actions", EmptyNextLevelPrepFunction);
        script = ReplaceFunction(script, "Trig_Next_Level_Run_Actions", CoopNextLevelFunction);

        script = ReplaceFunction(script, "Trig_Dam_Trigger2_Func005C", DamVisibilityFunction);

        script = script.Replace(
            "    call CustomDefeatBJ( udg_AP2_BluePlayer, \"TRIGSTR_001\" )",
            "    call CoopDefeat( \"TRIGSTR_001\" )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call CustomDefeatBJ( udg_AP2_BluePlayer, \"TRIGSTR_243\" )",
            "    call CoopDefeat( \"TRIGSTR_243\" )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call CustomDefeatBJ( udg_AP2_BluePlayer, \"TRIGSTR_244\" )",
            "    call CoopDefeat( \"TRIGSTR_244\" )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call CustomDefeatBJ( udg_AP2_BluePlayer, \"TRIGSTR_247\" )",
            "    call CoopDefeat( \"TRIGSTR_247\" )",
            StringComparison.Ordinal);

        script = AddSkipGuard(script, "Trig_Intro_Skipped_Actions", "udg_IntroSkipped");
        script = AddSkipGuard(script, "Trig_Dam_Skipped_Actions", "udg_DamCinSkipped");
        script = AddSkipGuard(script, "Trig_Victory_Skipped_Actions", "udg_VictorySkipped");
        script = AddCleanupGuard(script, "Trig_Intro_Cleanup_Actions", "udg_CoopIntroCleanupDone");
        script = AddCleanupGuard(script, "Trig_Dam_Cleanup_Actions", "udg_CoopDamCleanupDone");
        script = AddCleanupGuard(script, "Trig_Dam_MiniCleanup_Actions", "udg_CoopDamMiniCleanupDone");

        script = script.Replace(
            "    call SelectUnitSingle( udg_Illidan )",
            "    call CoopSelectStartingHeroes(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call ResetToGameCameraForPlayer( udg_AP2_BluePlayer, 0 )\n" +
            "    call PanCameraToTimedLocForPlayer( udg_AP2_BluePlayer, GetRectCenter(gg_rct_Game_Start_Illidan), 0 )",
            "    call CoopResetIntroCameras(  )",
            StringComparison.Ordinal);

        script = script.Replace(
            "    set udg_SelectedUnits = GetUnitsSelectedAll(udg_AP2_BluePlayer)",
            "    set udg_SelectedUnits = GetUnitsSelectedAll(udg_AP2_BluePlayer)\n" +
            "    if ( udg_CoopP3Human ) then\n" +
            "        set udg_CoopP3SelectedUnits = GetUnitsSelectedAll(Player(3))\n" +
            "    endif",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call SelectGroupBJ( udg_SelectedUnits )",
            "    call SelectGroupForPlayerBJ( udg_SelectedUnits, Player(1) )\n" +
            "    if ( udg_CoopP3Human ) then\n" +
            "        call SelectGroupForPlayerBJ( udg_CoopP3SelectedUnits, Player(3) )\n" +
            "    endif",
            StringComparison.Ordinal);
        script = script.Replace(
            "    set udg_HiddenUnits = GetUnitsOfPlayerMatching(udg_AP2_BluePlayer, Condition(function Trig_Dam_MiniSetup_Func016002002))",
            "    set udg_HiddenUnits = GetUnitsOfPlayerMatching(udg_AP2_BluePlayer, Condition(function Trig_Dam_MiniSetup_Func016002002))\n" +
            "    if ( udg_CoopP3Human ) then\n" +
            "        call GroupAddGroup( GetUnitsOfPlayerMatching(Player(3), Condition(function Trig_Dam_MiniSetup_Func016002002)), udg_HiddenUnits )\n" +
            "    endif",
            StringComparison.Ordinal);

        script = DuplicateMatchingLinesForP3Human(
            script,
            line => line.Contains("TriggerRegisterPlayerEventEndCinematic", StringComparison.Ordinal) &&
                    line.Contains("Player(1)", StringComparison.Ordinal),
            "Player(1)",
            "Player(3)");
        script = DuplicateMatchingLinesForP3Human(
            script,
            line => (line.Contains("TriggerRegisterPlayerEventVictory", StringComparison.Ordinal) ||
                     line.Contains("TriggerRegisterPlayerEventDefeat", StringComparison.Ordinal)) &&
                    line.Contains("Player(1)", StringComparison.Ordinal),
            "Player(1)",
            "Player(3)");
        script = DuplicateMatchingLinesForP3Human(
            script,
            line => CameraFunctions.Any(name => line.Contains(name, StringComparison.Ordinal)) &&
                    line.Contains("udg_AP2_BluePlayer", StringComparison.Ordinal),
            "udg_AP2_BluePlayer",
            "Player(3)");
        script = DuplicateMatchingLinesForP3Human(
            script,
            line => line.Contains("UnitShareVisionBJ", StringComparison.Ordinal) &&
                    line.Contains("udg_AP2_BluePlayer", StringComparison.Ordinal),
            "udg_AP2_BluePlayer",
            "Player(3)");
        script = DuplicateFogModifiersForP3(script);

        script = script.Replace(
            "function InitGlobals takes nothing returns nothing\n",
            NormalizeToLf(CoopFunctions) + "\nfunction InitGlobals takes nothing returns nothing\n",
            StringComparison.Ordinal);
        script = script.Replace(
            "function main takes nothing returns nothing\n",
            NormalizeToLf(CoopDifficultyFunctions) + "\nfunction main takes nothing returns nothing\n",
            StringComparison.Ordinal);

        RequireCount(script, "GetPlayerController(Player(3))", 1);
        RequireCount(script, "GetPlayerSlotState(Player(3))", 1);
        RequireCount(script, "call SetPlayerAllianceStateBJ( Player(3), Player(PLAYER_NEUTRAL_AGGRESSIVE), bj_ALLIANCE_UNALLIED )", 1);
        RequireCount(script, "call SetPlayerAllianceStateBJ( Player(PLAYER_NEUTRAL_AGGRESSIVE), Player(3), bj_ALLIANCE_UNALLIED )", 1);
        RequireCount(script, "    call StartMeleeAI( Player(2), \"N08X03.ai\" )", 1);
        RequireCount(script, "StartMeleeAI( Player(3)", 0);
        RequireCount(script, "EVENT_PLAYER_LEAVE", 0);
        RequireCount(script, "InitGameCacheBJ(", 0);
        RequireCount(script, "RestoreUnitLocFacingAngleBJ(", 0);
        RequireCount(script, "SaveGameCacheBJ(", 0);
        RequireCount(script, "StoreUnitBJ(", 0);
        RequireCount(script, "SetNextLevelBJ(", 0);
        RequireCount(script, "SetMissionAvailableBJ(", 0);
        RequireCount(script, "SetCampaignAvailableBJ(", 0);
        RequireCount(script, "CustomVictoryBJ( udg_AP2_BluePlayer", 0);
        RequireCount(script, "CustomDefeatBJ( udg_AP2_BluePlayer", 0);
        RequireCount(script, "call CreateItemLoc( 'sora', GetRectCenter(gg_rct_MaievItem01) )", 1);
        RequireCount(script, "TriggerRegisterPlayerEventEndCinematic", 6);
        RequireCount(script, "if ( udg_IntroSkipped ) then", 1);
        RequireCount(script, "if ( udg_DamCinSkipped ) then", 1);
        RequireCount(script, "if ( udg_VictorySkipped ) then", 1);

        return NormalizeToLf(script).Replace("\n", "\r\n", StringComparison.Ordinal);
    }

    private static string AddSkipGuard(string script, string functionName, string flag)
    {
        var signature = $"function {functionName} takes nothing returns nothing\n";
        RequireCount(script, signature, 1);
        return script.Replace(
            signature,
            signature + $"    if ( {flag} ) then\n        return\n    endif\n    set {flag} = true\n",
            StringComparison.Ordinal);
    }

    private static string AddCleanupGuard(string script, string functionName, string flag)
    {
        var signature = $"function {functionName} takes nothing returns nothing\n";
        RequireCount(script, signature, 1);
        return script.Replace(
            signature,
            signature + $"    if ( {flag} ) then\n        return\n    endif\n    set {flag} = true\n",
            StringComparison.Ordinal);
    }

    private static string DuplicateMatchingLinesForP3Human(
        string script,
        Func<string, bool> predicate,
        string sourcePlayer,
        string coopPlayer)
    {
        var lines = script.Split('\n');
        var output = new List<string>(lines.Length + 160);
        foreach (var line in lines)
        {
            output.Add(line);
            if (!predicate(line))
            {
                continue;
            }

            var indent = line[..(line.Length - line.TrimStart().Length)];
            output.Add(indent + "if ( udg_CoopP3Human ) then");
            output.Add("    " + ReplaceFirst(line, sourcePlayer, coopPlayer));
            output.Add(indent + "endif");
        }

        return string.Join("\n", output);
    }

    private static string DuplicateFogModifiersForP3(string script)
    {
        var lines = script.Split('\n');
        var output = new List<string>(lines.Length + 200);
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            output.Add(line);
            if (!line.Contains("CreateFogModifier", StringComparison.Ordinal) ||
                !line.Contains("udg_AP2_BluePlayer", StringComparison.Ordinal))
            {
                continue;
            }

            if (index + 1 >= lines.Length)
            {
                throw new InvalidDataException("Fog modifier call has no following bookkeeping line.");
            }

            var following = lines[++index];
            output.Add(following);
            if (!following.Contains("GetLastCreatedFogModifier()", StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Unexpected fog modifier bookkeeping: {following.Trim()}");
            }

            var indent = line[..(line.Length - line.TrimStart().Length)];
            output.Add(indent + "if ( udg_CoopP3Human ) then");
            output.Add("    " + ReplaceFirst(line, "udg_AP2_BluePlayer", "Player(3)"));
            output.Add("    " + indent + "call DestroyFogModifier( GetLastCreatedFogModifier() )");
            output.Add(indent + "endif");
        }

        return string.Join("\n", output);
    }

    private static string ReplaceFunction(string script, string functionName, string replacement)
    {
        var pattern = $@"function {Regex.Escape(functionName)} takes nothing returns (?:nothing|boolean)\n.*?\nendfunction";
        var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.CultureInvariant);
        var count = regex.Matches(script).Count;
        if (count != 1)
        {
            throw new InvalidDataException($"Expected one function named '{functionName}', found {count}.");
        }

        return regex.Replace(script, replacement.TrimEnd());
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
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
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
        "CameraSetEQNoiseForPlayer",
        "CameraSetTargetNoiseForPlayer",
        "CameraSetSourceNoiseForPlayer",
        "SetCameraBoundsToRectForPlayerBJ",
        "SetCameraQuickPositionLocForPlayer",
        "ResetToGameCameraForPlayer",
        "PanCameraToTimedLocForPlayer",
        "SetCameraTargetControllerNoZForPlayer",
        "SmartCameraPanBJ",
    ];

    private const string LoadHeroesFunction = """
function Trig_Load_Heroes_Actions takes nothing returns nothing
    call ConditionalTriggerExecute( gg_trg_Load_Tyrande )
    call ConditionalTriggerExecute( gg_trg_Load_Furion )
    call ConditionalTriggerExecute( gg_trg_Load_Illidan )
    call ConditionalTriggerExecute( gg_trg_Load_Magroth )
    call ConditionalTriggerExecute( gg_trg_Load_Maiev )
endfunction
""";

    private const string LoadTyrandeFunction = """
function Trig_Load_Tyrande_Actions takes nothing returns nothing
    call CreateNUnitsAtLoc( 1, 'Etyr', Player(2), GetRectCenter(gg_rct_Create_Tyrande), 350.00 )
    set udg_Tyrande = GetLastCreatedUnit()
    call SetHeroLevelBJ( udg_Tyrande, 10, false )
    call SelectHeroSkill( udg_Tyrande, 'AEst' )
    call SelectHeroSkill( udg_Tyrande, 'AEst' )
    call SelectHeroSkill( udg_Tyrande, 'AEst' )
    call SelectHeroSkill( udg_Tyrande, 'AHfa' )
    call SelectHeroSkill( udg_Tyrande, 'AHfa' )
    call SelectHeroSkill( udg_Tyrande, 'AHfa' )
    call SelectHeroSkill( udg_Tyrande, 'AEar' )
    call SelectHeroSkill( udg_Tyrande, 'AEar' )
    call SelectHeroSkill( udg_Tyrande, 'AEar' )
    call SelectHeroSkill( udg_Tyrande, 'AEsf' )
endfunction
""";

    private const string LoadFurionFunction = """
function Trig_Load_Furion_Actions takes nothing returns nothing
    call CreateNUnitsAtLoc( 1, 'Emfr', Player(3), GetRectCenter(gg_rct_Create_Furion), 190.00 )
    set udg_Furion = GetLastCreatedUnit()
    call SetHeroLevelBJ( udg_Furion, 10, false )
    call SelectHeroSkill( udg_Furion, 'AEer' )
    call SelectHeroSkill( udg_Furion, 'AEer' )
    call SelectHeroSkill( udg_Furion, 'AEer' )
    call SelectHeroSkill( udg_Furion, 'AEfn' )
    call SelectHeroSkill( udg_Furion, 'AEfn' )
    call SelectHeroSkill( udg_Furion, 'AEfn' )
    call SelectHeroSkill( udg_Furion, 'AEah' )
    call SelectHeroSkill( udg_Furion, 'AEah' )
    call SelectHeroSkill( udg_Furion, 'AEah' )
    call SelectHeroSkill( udg_Furion, 'AEtq' )
endfunction
""";

    private const string LoadIllidanFunction = """
function Trig_Load_Illidan_Actions takes nothing returns nothing
    call CreateNUnitsAtLoc( 1, 'Eevi', udg_AP2_BluePlayer, GetRectCenter(gg_rct_Create_Illidan), 190.00 )
    set udg_Illidan = GetLastCreatedUnit()
    call SetHeroLevelBJ( udg_Illidan, 10, false )
    call SelectHeroSkill( udg_Illidan, 'AEmb' )
    call SelectHeroSkill( udg_Illidan, 'AEmb' )
    call SelectHeroSkill( udg_Illidan, 'AEmb' )
    call SelectHeroSkill( udg_Illidan, 'AEim' )
    call SelectHeroSkill( udg_Illidan, 'AEim' )
    call SelectHeroSkill( udg_Illidan, 'AEim' )
    call SelectHeroSkill( udg_Illidan, 'AEev' )
    call SelectHeroSkill( udg_Illidan, 'AEev' )
    call SelectHeroSkill( udg_Illidan, 'AEev' )
    call SelectHeroSkill( udg_Illidan, 'AEvi' )
endfunction
""";

    private const string EmptyMagrothFunction = """
function Trig_Load_Magroth_Actions takes nothing returns nothing
endfunction
""";

    private const string LoadMaievItemsFunction = """
function Trig_Load_Maiev_Actions takes nothing returns nothing
    call CreateItemLoc( 'sora', GetRectCenter(gg_rct_MaievItem01) )
endfunction
""";

    private const string EmptyNextLevelPrepFunction = """
function Trig_Next_Level_Prep_Actions takes nothing returns nothing
endfunction
""";

    private const string CoopNextLevelFunction = """
function Trig_Next_Level_Run_Actions takes nothing returns nothing
    call CoopVictory(  )
endfunction
""";

    private const string DamVisibilityFunction = """
function Trig_Dam_Trigger2_Func005C takes nothing returns boolean
    if ( IsLocationVisibleToPlayer(udg_PointDamInSight1, udg_AP2_BluePlayer) or IsLocationVisibleToPlayer(udg_PointDamInSight2, udg_AP2_BluePlayer) or IsLocationVisibleToPlayer(udg_PointDamInSight3, udg_AP2_BluePlayer) or IsLocationVisibleToPlayer(udg_PointDamInSight4, udg_AP2_BluePlayer) or IsLocationVisibleToPlayer(udg_PointDamInSight5, udg_AP2_BluePlayer) ) then
        return true
    endif
    if ( udg_CoopP3Human and ( IsLocationVisibleToPlayer(udg_PointDamInSight1, Player(3)) or IsLocationVisibleToPlayer(udg_PointDamInSight2, Player(3)) or IsLocationVisibleToPlayer(udg_PointDamInSight3, Player(3)) or IsLocationVisibleToPlayer(udg_PointDamInSight4, Player(3)) or IsLocationVisibleToPlayer(udg_PointDamInSight5, Player(3)) ) ) then
        return true
    endif
    return false
endfunction
""";

    private const string CoopFunctions = """
//===========================================================================
// Cooperative helpers injected by nelfx08-coop-build.
//===========================================================================
function CoopClassifyP3 takes nothing returns nothing
    local mapcontrol controller = GetPlayerController(Player(3))
    local playerslotstate slotState = GetPlayerSlotState(Player(3))
    set udg_CoopP3Human = ( controller == MAP_CONTROL_USER and slotState == PLAYER_SLOT_STATE_PLAYING )
    set udg_CoopHumanForce = CreateForce()
    call ForceAddPlayer( udg_CoopHumanForce, Player(1) )
    if ( udg_CoopP3Human ) then
        call ForceAddPlayer( udg_CoopHumanForce, Player(3) )
    else
        call SetPlayerController( Player(3), MAP_CONTROL_NEUTRAL )
    endif
    set udg_APG2_BluePlayer = udg_CoopHumanForce
endfunction

function CoopApplyP3Mode takes nothing returns nothing
    call SetPlayerState( Player(1), PLAYER_STATE_ALLIED_VICTORY, 1 )
    call SetPlayerState( Player(3), PLAYER_STATE_ALLIED_VICTORY, 1 )
    if ( udg_CoopP3Human ) then
        call SetPlayerAllianceStateAllyBJ( Player(1), Player(3), true )
        call SetPlayerAllianceStateAllyBJ( Player(3), Player(1), true )
        call SetPlayerAlliance( Player(1), Player(3), ALLIANCE_SHARED_VISION, true )
        call SetPlayerAlliance( Player(3), Player(1), ALLIANCE_SHARED_VISION, true )
        call SetPlayerAlliance( Player(1), Player(3), ALLIANCE_SHARED_CONTROL, false )
        call SetPlayerAlliance( Player(3), Player(1), ALLIANCE_SHARED_CONTROL, false )
        call SetPlayerAlliance( Player(1), Player(3), ALLIANCE_SHARED_ADVANCED_CONTROL, false )
        call SetPlayerAlliance( Player(3), Player(1), ALLIANCE_SHARED_ADVANCED_CONTROL, false )
        call SetPlayerAllianceStateBJ( Player(3), Player(PLAYER_NEUTRAL_AGGRESSIVE), bj_ALLIANCE_UNALLIED )
    else
        call SetPlayerAllianceStateBJ( Player(1), Player(3), bj_ALLIANCE_ALLIED_ADVUNITS )
        call SetPlayerAllianceStateBJ( Player(3), Player(1), bj_ALLIANCE_ALLIED_ADVUNITS )
    endif
endfunction

function CoopVictory takes nothing returns nothing
    if ( not udg_CoopResultIssued ) then
        set udg_CoopResultIssued = true
        set udg_GameOver = true
        if ( udg_CoopP3Human ) then
            call CustomVictoryBJ( Player(3), false, true )
        endif
        call CustomVictoryBJ( Player(1), false, true )
    endif
endfunction

function CoopDefeat takes string message returns nothing
    if ( not udg_CoopResultIssued ) then
        set udg_CoopResultIssued = true
        set udg_GameOver = true
        if ( udg_CoopP3Human ) then
            call CustomDefeatBJ( Player(3), message )
        endif
        call CustomDefeatBJ( Player(1), message )
    endif
endfunction

function CoopSelectStartingHeroes takes nothing returns nothing
    call SelectUnitForPlayerSingle( udg_Illidan, Player(1) )
    if ( udg_CoopP3Human ) then
        call SelectUnitForPlayerSingle( udg_Furion, Player(3) )
    endif
endfunction

function CoopResetIntroCameras takes nothing returns nothing
    call ResetToGameCameraForPlayer( Player(1), 0 )
    call PanCameraToTimedLocForPlayer( Player(1), GetRectCenter(gg_rct_Game_Start_Illidan), 0 )
    if ( udg_CoopP3Human ) then
        call ResetToGameCameraForPlayer( Player(3), 0 )
        call PanCameraToTimedLocForPlayer( Player(3), GetRectCenter(gg_rct_Game_Start_Furion), 0 )
    endif
endfunction
""";

    private const string CoopDifficultyFunctions = """
//===========================================================================
// Player 1 selects difficulty before the original trigger initialization.
//===========================================================================
function CoopFinishDifficultySelection takes gamedifficulty difficulty returns nothing
    if ( udg_CoopInitializationStarted ) then
        return
    endif
    set udg_CoopInitializationStarted = true
    call SetGameDifficulty( difficulty )
    call PauseTimer( udg_CoopDifficultyTimer )
    call DialogDisplay( Player(1), udg_CoopDifficultyDialog, false )
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
    call PauseAllUnitsBJ( false )
    call InitCustomTriggers(  )
    call RunInitializationTriggers(  )
    call CoopApplyP3Mode(  )
endfunction

function CoopChooseDifficulty takes nothing returns nothing
    if ( GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED ) then
        call DisplayTimedTextToPlayer( Player(1), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_904") )
        if ( udg_CoopP3Human ) then
            call DisplayTimedTextToPlayer( Player(3), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_904") )
        endif
        call CoopFinishDifficultySelection( MAP_DIFFICULTY_NORMAL )
    elseif ( GetClickedButton() == udg_CoopDifficultyHard ) then
        call DisplayTimedTextToPlayer( Player(1), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_905") )
        if ( udg_CoopP3Human ) then
            call DisplayTimedTextToPlayer( Player(3), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_905") )
        endif
        call CoopFinishDifficultySelection( MAP_DIFFICULTY_HARD )
    else
        call DisplayTimedTextToPlayer( Player(1), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_904") )
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
    call DialogDisplay( Player(1), udg_CoopDifficultyDialog, true )
    if ( udg_CoopP3Human ) then
        call DisplayTimedTextToPlayer( Player(3), 0.00, 0.00, 30.00, GetLocalizedString("TRIGSTR_903") )
    else
        call DisplayTimedTextToPlayer( Player(1), 0.00, 0.00, 30.00, GetLocalizedString("TRIGSTR_906") )
    endif
    call TimerStart( udg_CoopDifficultyTimer, 30.00, false, null )
endfunction

function CoopScheduleDifficultySelection takes nothing returns nothing
    call PauseAllUnitsBJ( true )
    set udg_CoopDifficultyStartupTimer = CreateTimer()
    call TimerStart( udg_CoopDifficultyStartupTimer, 0.01, false, function CoopBeginDifficultySelection )
endfunction
""";
}
