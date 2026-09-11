using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using War3Net.Build;
using War3Net.Build.Extensions;
using War3Net.Build.Info;
using War3Net.Build.Script;
using War3Net.CodeAnalysis.Jass;
using War3Net.IO.Mpq;

namespace NightElf07CoopBuild;

internal sealed record BuildResult(string OutputPath, string SourceSha256, string OutputSha256);

internal static class NightElf07Builder
{
    private const string ExpectedSourceSha256 = "97143C5E8F417DB033B566ED54A2359D6A2BA609F343A3DE79ABADF50A3D1C92";

    public static BuildResult Build(string sourcePath, string outputPath)
    {
        var source = Path.GetFullPath(sourcePath);
        var output = Path.GetFullPath(outputPath);
        if (!File.Exists(source))
        {
            throw new FileNotFoundException("Official NightElf07 map was not found.", source);
        }

        if (File.Exists(output))
        {
            throw new IOException($"Output already exists: {output}");
        }

        var sourceHash = Sha256(source);
        if (!sourceHash.Equals(ExpectedSourceSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Unsupported NightElf07 source. Expected {ExpectedSourceSha256}, got {sourceHash}.");
        }

        var map = Map.Open(source, MapFiles.Info | MapFiles.TriggerStrings | MapFiles.Script);
        PatchInfo(map.Info ?? throw new InvalidDataException("war3map.w3i is missing."));
        PatchStrings(map);
        map.Script = PatchScript(map.Script ?? throw new InvalidDataException("war3map.j is missing."));
        _ = JassSyntaxFactory.ParseCompilationUnit(map.Script);

        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        using var sourceArchive = MpqArchive.Open(source, loadListFile: true);
        var builder = new MpqArchiveBuilder(sourceArchive);
        builder.AddFile(map.GetInfoFile() ?? throw new InvalidDataException("Failed to serialize war3map.w3i."));
        builder.AddFile(map.GetScriptFile() ?? throw new InvalidDataException("Failed to serialize war3map.j."));
        builder.AddFile(map.GetTriggerStringsFile(new UTF8Encoding(false, true))
            ?? throw new InvalidDataException("Failed to serialize war3map.wts."));
        builder.SaveWithPreArchiveData(output);

        return new BuildResult(output, sourceHash, Sha256(output));
    }

    private static void PatchInfo(MapInfo info)
    {
        info.MapName = "诸神黄昏 - 2-3人合作开发版";
        info.MapDescription = "暗夜精灵、兽族与人类共同守卫海加尔山；兽族和人类可由真人或电脑控制。";
        info.RecommendedPlayers = "2-3";

        var orc = info.Players.SingleOrDefault(player => player.Id == 0)
            ?? throw new InvalidDataException("Player 1 Orc slot is missing.");
        var nightElf = info.Players.SingleOrDefault(player => player.Id == 1)
            ?? throw new InvalidDataException("Player 2 Night Elf slot is missing.");
        var human = info.Players.SingleOrDefault(player => player.Id == 9)
            ?? throw new InvalidDataException("Player 10 Human slot is missing.");
        if (orc.Race != PlayerRace.Orc || nightElf.Race != PlayerRace.NightElf || human.Race != PlayerRace.Human)
        {
            throw new InvalidDataException("The official allied faction races do not match the expected slots.");
        }

        orc.Controller = PlayerController.User;
        nightElf.Controller = PlayerController.User;
        human.Controller = PlayerController.User;
    }

    private static void PatchStrings(Map map)
    {
        if (map.TriggerStrings is null)
        {
            throw new InvalidDataException("war3map.wts is missing.");
        }

        Set(0, "诸神黄昏 - 2-3人合作开发版");
        Set(1, "2-3");
        Set(2, "第七章：诸神黄昏（2-3人合作开发版）");
        Add(900, "选择难度");
        Add(901, "普通");
        Add(902, "困难");
        Add(904, "正在等待玩家 2 选择难度（30 秒后默认普通）");
        Add(905, "难度：普通");
        Add(906, "难度：困难");
        Add(908, "大厅配置错误：兽族槽为空、关闭或未加入。该阵营不会启动 AI；请重新开局并将该槽设为真人或电脑。");
        Add(909, "大厅配置错误：人类槽为空、关闭或未加入。该阵营不会启动 AI；请重新开局并将该槽设为真人或电脑。");
        Add(910, "大厅配置错误：暗夜精灵玩家 2 必须为正在游戏的真人槽。");

        void Set(uint key, string value)
        {
            var matches = map.TriggerStrings.Strings.Where(item => item.Key == key).ToArray();
            if (matches.Length == 0)
            {
                throw new InvalidDataException($"TRIGSTR_{key:000} is missing.");
            }

            foreach (var item in matches)
            {
                item.Value = value;
            }
        }

        void Add(uint key, string value)
        {
            if (map.TriggerStrings.Strings.Any(item => item.Key == key))
            {
                throw new InvalidDataException($"TRIGSTR_{key:000} is already in use.");
            }

            map.TriggerStrings.Strings.Add(new TriggerString { Key = key, Value = value });
        }
    }

    private static string PatchScript(string source)
    {
        var script = NormalizeToLf(source);
        RequireCount(script, "function config takes nothing returns nothing", 1);
        RequireCount(script, "    set udg_AAPG_NightElves                        = bj_FORCE_PLAYER[1]", 1);
        RequireCount(script, "    call StartCampaignAI( udg_P_Thrall, \"n07_red.ai\" )", 1);
        RequireCount(script, "    call StartCampaignAI( udg_P_Jaina, \"n07_lightblue.ai\" )", 1);
        RequireCount(script, "    call CommandAI( Player(0), 0, 0 )", 1);
        RequireCount(script, "    call CommandAI( Player(9), 0, 0 )", 1);
        RequireCount(script, "    call TriggerRegisterPlayerEventEndCinematic(", 5);
        RequireCount(script, "    set udg_CameraPoint = GetCameraTargetPositionLoc()\n    set udg_CurrentSelection = GetUnitsSelectedAll(udg_AAAP_NightElves)", 2);
        RequireCount(script, "    call SelectGroupBJ( udg_CurrentSelection )", 4);
        RequireCount(script, "    call SelectUnitSingle( udg_Furion )", 2);

        script = script.Replace(
            "    set udg_CameraPoint = GetCameraTargetPositionLoc()\n    set udg_CurrentSelection = GetUnitsSelectedAll(udg_AAAP_NightElves)",
            "    call CoopCapturePlayerState(  )", StringComparison.Ordinal);
        script = script.Replace("    call SelectGroupBJ( udg_CurrentSelection )",
            "    call CoopRestoreSelections(  )", StringComparison.Ordinal);
        script = script.Replace(
            "    call ResetToGameCameraForPlayer( udg_AAAP_NightElves, 0 )\n    call PanCameraToTimedLocForPlayer( udg_AAAP_NightElves, udg_CameraPoint, 0 )",
            "    call CoopRestoreCameras(  )", StringComparison.Ordinal);
        script = script.Replace("    call SelectUnitSingle( udg_Furion )",
            "    call CoopSelectStartingHeroes(  )", StringComparison.Ordinal);
        script = script.Replace(
            "    call ResetToGameCameraForPlayer( udg_AAAP_NightElves, 0.00 )\n" +
            "    call PanCameraToTimedLocForPlayer( udg_AAAP_NightElves, GetRectCenter(gg_rct_FurionStart), 0 )",
            "    call CoopFocusStartingHeroes(  )", StringComparison.Ordinal);
        RequireCount(script, "call CoopCapturePlayerState(  )", 2);
        RequireCount(script, "call CoopFocusStartingHeroes(  )", 2);

        const string generatedGlobalsAnchor = "    // Generated\n";
        RequireCount(script, generatedGlobalsAnchor, 1);
        script = script.Replace(
            generatedGlobalsAnchor,
            "    boolean             udg_CoopNightElfHuman      = false\n" +
            "    boolean             udg_CoopThrallHuman        = false\n" +
            "    boolean             udg_CoopThrallAI           = false\n" +
            "    boolean             udg_CoopJainaHuman         = false\n" +
            "    boolean             udg_CoopJainaAI            = false\n" +
            "    boolean             udg_CoopThrallAIStarted     = false\n" +
            "    boolean             udg_CoopJainaAIStarted      = false\n" +
            "    boolean             udg_CoopResultIssued        = false\n" +
            "    group               udg_CoopThrallSelection     = null\n" +
            "    group               udg_CoopJainaSelection      = null\n" +
            "    real array          udg_CoopCameraX\n" +
            "    real array          udg_CoopCameraY\n" +
            "    dialog              udg_CoopDifficultyDialog    = null\n" +
            "    button              udg_CoopDifficultyNormal    = null\n" +
            "    button              udg_CoopDifficultyHard      = null\n" +
            "    trigger             udg_CoopDifficultyTrigger   = null\n" +
            "    timer               udg_CoopDifficultyStartupTimer = null\n" +
            "    timer               udg_CoopDifficultyTimer     = null\n" +
            "    boolean             udg_CoopInitializationStarted = false\n\n" +
            generatedGlobalsAnchor,
            StringComparison.Ordinal);

        script = script.Replace(
            "    set udg_AAPG_NightElves                        = bj_FORCE_PLAYER[1]",
            "    set udg_AAPG_NightElves                        = CreateForce()",
            StringComparison.Ordinal);
        script = script.Replace(
            "    set udg_CurrentSelection                       = CreateGroup()",
            "    set udg_CurrentSelection                       = CreateGroup()\n" +
            "    set udg_CoopThrallSelection                    = CreateGroup()\n" +
            "    set udg_CoopJainaSelection                     = CreateGroup()",
            StringComparison.Ordinal);

        foreach (var playerId in new[] { 0, 1, 2, 3, 4, 6, 7, 8, 9, 10 })
        {
            var selectable = $"    call SetPlayerRaceSelectable( Player({playerId}), true )";
            var fixedRace = $"    call SetPlayerRaceSelectable( Player({playerId}), false )";
            RequireCount(script, selectable, 1);
            script = script.Replace(selectable, fixedRace, StringComparison.Ordinal);
        }

        script = script.Replace("    call SetPlayerController( Player(0), MAP_CONTROL_COMPUTER )",
            "    call SetPlayerController( Player(0), MAP_CONTROL_USER )", StringComparison.Ordinal);
        script = script.Replace("    call SetPlayerController( Player(9), MAP_CONTROL_COMPUTER )",
            "    call SetPlayerController( Player(9), MAP_CONTROL_USER )", StringComparison.Ordinal);

        AddTechRestrictions(ref script, 0, "Otch",
            ["Oshd", "otbk", "otbr", "ovln", "Rolf", "Rorb", "Robk", "Ropm"]);
        AddTechRestrictions(ref script, 1, "Edem",
            ["Ewar", "Ewrd", "efdr", "emtg", "eden", "Rers", "Rehs", "Reeb", "Reec", "Rews", "Repm"]);
        AddTechRestrictions(ref script, 9, "Hamg",
            ["Hblm", "hspt", "hdhw", "hrtt", "hvlt", "Rhss", "Rhrt", "Rhpm", "Rhfc", "Rhcd"]);

        script = ReplacePlaceholderItems(script, "udg_Jaina",
            ["ckng", "hcun", "ciri", "cnob", "gcel", "bspd"]);
        script = ReplacePlaceholderItems(script, "udg_Thrall",
            ["nspi", "ratc", "rat9", "cnob", "rin1", "rin1"]);
        script = ReplaceFriendlyStartingResources(script, 0, "udg_CoopThrallAI");
        script = ReplaceFriendlyStartingResources(script, 9, "udg_CoopJainaAI");

        script = ReplaceFunction(script, "Trig_Load_Tyrande_Actions", TyrandeInitialization);
        script = ReplaceFunction(script, "Trig_Load_Furion_Actions", FurionInitialization);
        script = ReplaceFunction(script, "Trig_Save_Credits_Variable_Actions",
            "function Trig_Save_Credits_Variable_Actions takes nothing returns nothing\nendfunction");
        script = ReplaceFunction(script, "Trig_Next_Level_Run_Actions", NextLevelActions);
        script = ReplaceFunction(script, "Trig_Defeat_Cheat_Actions", DefeatActions);

        script = script.Replace("    call StartCampaignAI( udg_P_Thrall, \"n07_red.ai\" )",
            "    call CoopStartThrallAI(  )", StringComparison.Ordinal);
        script = script.Replace("    call StartCampaignAI( udg_P_Jaina, \"n07_lightblue.ai\" )",
            "    call CoopStartJainaAI(  )", StringComparison.Ordinal);
        script = script.Replace("    call CommandAI( Player(0), 0, 0 )",
            "    call CoopCommandThrallAI( 0, 0 )", StringComparison.Ordinal);
        script = script.Replace("    call CommandAI( Player(9), 0, 0 )",
            "    call CoopCommandJainaAI( 0, 0 )", StringComparison.Ordinal);

        script = GateSubsidy(script, "Trig_Thrall_Gold_Actions", "udg_CoopThrallAI");
        script = GateSubsidy(script, "Trig_Thrall_Lumber_Actions", "udg_CoopThrallAI");
        script = GateSubsidy(script, "Trig_Jaina_Gold_Actions", "udg_CoopJainaAI");
        script = GateSubsidy(script, "Trig_Jaina_Lumber_Actions", "udg_CoopJainaAI");

        script = script.Replace("    call CinematicModeBJ( true, GetPlayersAll() )",
            "    call CinematicModeBJ( true, udg_AAPG_NightElves )", StringComparison.Ordinal);
        script = script.Replace("call QuestMessageBJ( GetPlayersAll(),",
            "call QuestMessageBJ( udg_AAPG_NightElves,", StringComparison.Ordinal);
        script = DuplicatePlayerOneLines(script, line =>
            line.Contains("TriggerRegisterPlayerEventEndCinematic", StringComparison.Ordinal), "Player(1)");
        script = DuplicatePlayerOneLines(script, line =>
            CameraFunctions.Any(name => line.Contains(name, StringComparison.Ordinal)) &&
            line.Contains("udg_AAAP_NightElves", StringComparison.Ordinal));

        script = script.Replace(
            "    call InitGlobals(  )\n    call InitCustomTriggers(  )\n    call RunInitializationTriggers(  )",
            "    call InitGlobals(  )\n    call CoopClassifySlots(  )\n    call CoopScheduleDifficultySelection(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call SetGamePlacement( MAP_PLACEMENT_USE_MAP_SETTINGS )",
            "    call SetGamePlacement( MAP_PLACEMENT_USE_MAP_SETTINGS )\n" +
            "    call SetMapFlag( MAP_LOCK_ALLIANCE_CHANGES, false )\n" +
            "    call SetMapFlag( MAP_ALLIANCE_CHANGES_HIDDEN, false )\n" +
            "    call SetMapFlag( MAP_LOCK_RESOURCE_TRADING, false )\n" +
            "    call SetMapFlag( MAP_RESOURCE_TRADING_ALLIES_ONLY, true )",
            StringComparison.Ordinal);

        script = script.Replace("function InitGlobals takes nothing returns nothing\n",
            NormalizeToLf(CoopFunctions) + "\nfunction InitGlobals takes nothing returns nothing\n", StringComparison.Ordinal);
        script = script.Replace("function main takes nothing returns nothing\n",
            NormalizeToLf(DifficultyFunctions) + "\nfunction main takes nothing returns nothing\n", StringComparison.Ordinal);

        RequireCount(script, "call GetPlayerController", 0);
        RequireCount(script, "GetPlayerController(", 3);
        RequireCount(script, "GetPlayerSlotState(", 3);
        RequireCount(script, "EVENT_PLAYER_LEAVE", 0);
        RequireCount(script, "InitGameCacheBJ(", 0);
        RequireCount(script, "RestoreUnitLocFacingAngleBJ(", 0);
        RequireCount(script, "SaveGameCacheBJ(", 0);
        RequireCount(script, "SetNextLevelBJ(", 0);
        RequireCount(script, "SetCinematicAvailableBJ(", 0);
        RequireCount(script, "ALLIANCE_SHARED_CONTROL, true", 0);
        RequireCount(script, "ALLIANCE_SHARED_ADVANCED_CONTROL, true", 0);
        RequireCount(script, "QuestMessageBJ( GetPlayersAll(),", 0);
        RequireCount(script, "call CoopStartThrallAI(  )", 1);
        RequireCount(script, "call CoopStartJainaAI(  )", 1);
        RequireCount(script, "call StartCampaignAI( Player(0), \"n07_red.ai\" )", 1);
        RequireCount(script, "call StartCampaignAI( Player(9), \"n07_lightblue.ai\" )", 1);
        RequireCount(script, "call CoopCommandThrallAI( 0, 0 )", 1);
        RequireCount(script, "call CoopCommandJainaAI( 0, 0 )", 1);
        RequireCount(script, "call CoopCapturePlayerState(  )", 2);
        RequireCount(script, "call CoopRestoreSelections(  )", 4);
        RequireCount(script, "call CoopRestoreCameras(  )", 4);
        RequireCount(script, "call CoopSelectStartingHeroes(  )", 2);
        RequireCount(script, "call CoopFocusStartingHeroes(  )", 2);
        RequireCount(script, "call CoopScheduleDifficultySelection(  )", 1);
        RequireCount(script, "call InitCustomTriggers(  )", 1);
        RequireCount(script, "call RunInitializationTriggers(  )", 1);

        return NormalizeToLf(script).Replace("\n", "\r\n", StringComparison.Ordinal);
    }

    private static void AddTechRestrictions(
        ref string script,
        int playerId,
        string anchorRawcode,
        IReadOnlyCollection<string> rawcodes)
    {
        var anchor = $"    call SetPlayerTechMaxAllowed( Player({playerId}), '{anchorRawcode}', 0 )";
        RequireCount(script, anchor, 1);
        var additions = string.Join(
            "\n",
            rawcodes.Select(rawcode => $"    call SetPlayerTechMaxAllowed( Player({playerId}), '{rawcode}', 0 )"));
        script = script.Replace(anchor, anchor + "\n" + additions, StringComparison.Ordinal);
    }

    private static string GateSubsidy(string script, string functionName, string aiFlag)
    {
        var signature = $"function {functionName} takes nothing returns nothing\n";
        RequireCount(script, signature, 1);
        return script.Replace(signature, signature + $"    if ( not {aiFlag} ) then\n        return\n    endif\n", StringComparison.Ordinal);
    }

    private static string ReplacePlaceholderItems(
        string script,
        string heroVariable,
        IReadOnlyList<string> itemRawcodes)
    {
        var placeholder = $"    call UnitAddItemByIdSwapped( 'phlt', {heroVariable} )";
        RequireCount(script, placeholder, itemRawcodes.Count);
        var placeholderBlock = string.Join("\n", Enumerable.Repeat(placeholder, itemRawcodes.Count));
        RequireCount(script, placeholderBlock, 1);
        var replacement = string.Join(
            "\n",
            itemRawcodes.Select(rawcode => $"    call UnitAddItemByIdSwapped( '{rawcode}', {heroVariable} )"));
        return script.Replace(placeholderBlock, replacement, StringComparison.Ordinal);
    }

    private static string ReplaceFriendlyStartingResources(string script, int playerId, string aiFlag)
    {
        var original =
            $"    call SetPlayerStateBJ( Player({playerId}), PLAYER_STATE_RESOURCE_GOLD, 6500 )\n" +
            $"    call SetPlayerStateBJ( Player({playerId}), PLAYER_STATE_RESOURCE_LUMBER, 6500 )";
        RequireCount(script, original, 1);
        var replacement =
            $"    if ( {aiFlag} ) then\n" +
            $"        call SetPlayerStateBJ( Player({playerId}), PLAYER_STATE_RESOURCE_GOLD, 6500 )\n" +
            $"        call SetPlayerStateBJ( Player({playerId}), PLAYER_STATE_RESOURCE_LUMBER, 6500 )\n" +
            "    else\n" +
            $"        call SetPlayerStateBJ( Player({playerId}), PLAYER_STATE_RESOURCE_GOLD, 1000 )\n" +
            $"        call SetPlayerStateBJ( Player({playerId}), PLAYER_STATE_RESOURCE_LUMBER, 800 )\n" +
            "    endif";
        return script.Replace(original, replacement, StringComparison.Ordinal);
    }

    private static string DuplicatePlayerOneLines(string script, Func<string, bool> predicate, string sourcePlayer = "udg_AAAP_NightElves")
    {
        var output = new List<string>();
        foreach (var line in script.Split('\n'))
        {
            output.Add(line);
            if (!predicate(line))
            {
                continue;
            }

            var indent = line[..(line.Length - line.TrimStart().Length)];
            output.Add(indent + "if ( udg_CoopThrallHuman ) then");
            output.Add("    " + ReplaceFirst(line, sourcePlayer, "Player(0)"));
            output.Add(indent + "endif");
            output.Add(indent + "if ( udg_CoopJainaHuman ) then");
            output.Add("    " + ReplaceFirst(line, sourcePlayer, "Player(9)"));
            output.Add(indent + "endif");
        }

        return string.Join("\n", output);
    }

    private static string ReplaceFunction(string script, string functionName, string replacement)
    {
        var regex = new Regex($@"function {Regex.Escape(functionName)} takes nothing returns nothing\n.*?\nendfunction",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
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
        return index < 0 ? value : value[..index] + replacement + value[(index + source.Length)..];
    }

    private static string NormalizeToLf(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

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
        "CameraSetSmoothingFactorForPlayer",
        "ResetToGameCameraForPlayer",
        "PanCameraToTimedLocForPlayer",
        "SmartCameraPanBJ",
        "SetCameraTargetControllerNoZForPlayer",
    ];

    private const string TyrandeInitialization = """
function Trig_Load_Tyrande_Actions takes nothing returns nothing
    call CreateNUnitsAtLoc( 1, 'Etyr', udg_AAAP_NightElves, GetRectCenter(gg_rct_IntroTyrande00), 180.00 )
    set udg_Tyrande = GetLastCreatedUnit()
    call SetHeroLevel( udg_Tyrande, 7, false )
    call SelectHeroSkill( udg_Tyrande, 'AEst' )
    call SelectHeroSkill( udg_Tyrande, 'AHfa' )
    call SelectHeroSkill( udg_Tyrande, 'AEar' )
    call SelectHeroSkill( udg_Tyrande, 'AEar' )
    call SelectHeroSkill( udg_Tyrande, 'AEar' )
    call SelectHeroSkill( udg_Tyrande, 'AEsf' )
    call UnitAddItemByIdSwapped( 'gcel', udg_Tyrande )
    call UnitAddItemByIdSwapped( 'rat6', udg_Tyrande )
    call UnitAddItemByIdSwapped( 'rag1', udg_Tyrande )
    call UnitAddItemByIdSwapped( 'rnsp', udg_Tyrande )
    call UnitAddItemByIdSwapped( 'rin1', udg_Tyrande )
    call UnitAddItemByIdSwapped( 'mnst', udg_Tyrande )
endfunction
""";

    private const string FurionInitialization = """
function Trig_Load_Furion_Actions takes nothing returns nothing
    call CreateNUnitsAtLoc( 1, 'Efur', udg_AAAP_NightElves, GetRectCenter(gg_rct_IntroTyrande00), 225.00 )
    set udg_Furion = GetLastCreatedUnit()
    call SetHeroLevel( udg_Furion, 7, false )
    call SelectHeroSkill( udg_Furion, 'AEer' )
    call SelectHeroSkill( udg_Furion, 'AEer' )
    call SelectHeroSkill( udg_Furion, 'AEfn' )
    call SelectHeroSkill( udg_Furion, 'AEfn' )
    call SelectHeroSkill( udg_Furion, 'AEah' )
    call SelectHeroSkill( udg_Furion, 'AEtq' )
    call UnitAddItemByIdSwapped( 'cnhn', udg_Furion )
    call UnitAddItemByIdSwapped( 'nspi', udg_Furion )
    call UnitAddItemByIdSwapped( 'rwiz', udg_Furion )
    call UnitAddItemByIdSwapped( 'ciri', udg_Furion )
    call UnitAddItemByIdSwapped( 'rat9', udg_Furion )
    call UnitAddItemByIdSwapped( 'rin1', udg_Furion )
endfunction
""";

    private const string NextLevelActions = """
function Trig_Next_Level_Run_Actions takes nothing returns nothing
    call SetUnitOwner( udg_Jaina, udg_P_Jaina, true )
    call SetUnitOwner( udg_Thrall, udg_P_Thrall, true )
    call CoopVictory(  )
endfunction
""";

    private const string DefeatActions = """
function Trig_Defeat_Cheat_Actions takes nothing returns nothing
    set udg_GameOver = true
    call CoopDefeat( "TRIGSTR_126" )
endfunction
""";

    private const string CoopFunctions = """
//===========================================================================
// Cooperative helpers injected by nightelf07-coop-build.
//===========================================================================
function CoopClassifySlots takes nothing returns nothing
    local mapcontrol nightElfController = GetPlayerController(Player(1))
    local playerslotstate nightElfSlot = GetPlayerSlotState(Player(1))
    local mapcontrol thrallController = GetPlayerController(Player(0))
    local playerslotstate thrallSlot = GetPlayerSlotState(Player(0))
    local mapcontrol jainaController = GetPlayerController(Player(9))
    local playerslotstate jainaSlot = GetPlayerSlotState(Player(9))
    set udg_CoopNightElfHuman = ( nightElfController == MAP_CONTROL_USER and nightElfSlot == PLAYER_SLOT_STATE_PLAYING )
    set udg_CoopThrallHuman = ( thrallController == MAP_CONTROL_USER and thrallSlot == PLAYER_SLOT_STATE_PLAYING )
    set udg_CoopThrallAI = ( thrallController == MAP_CONTROL_COMPUTER and thrallSlot == PLAYER_SLOT_STATE_PLAYING )
    set udg_CoopJainaHuman = ( jainaController == MAP_CONTROL_USER and jainaSlot == PLAYER_SLOT_STATE_PLAYING )
    set udg_CoopJainaAI = ( jainaController == MAP_CONTROL_COMPUTER and jainaSlot == PLAYER_SLOT_STATE_PLAYING )
    if ( udg_CoopNightElfHuman ) then
        call ForceAddPlayer( udg_AAPG_NightElves, Player(1) )
    endif
    if ( udg_CoopThrallHuman ) then
        call ForceAddPlayer( udg_AAPG_NightElves, Player(0) )
    endif
    if ( udg_CoopJainaHuman ) then
        call ForceAddPlayer( udg_AAPG_NightElves, Player(9) )
    endif
endfunction

function CoopStartThrallAI takes nothing returns nothing
    if ( udg_CoopThrallAI and not udg_CoopThrallAIStarted ) then
        call StartCampaignAI( Player(0), "n07_red.ai" )
        set udg_CoopThrallAIStarted = true
    endif
endfunction

function CoopStartJainaAI takes nothing returns nothing
    if ( udg_CoopJainaAI and not udg_CoopJainaAIStarted ) then
        call StartCampaignAI( Player(9), "n07_lightblue.ai" )
        set udg_CoopJainaAIStarted = true
    endif
endfunction

function CoopCommandThrallAI takes integer command, integer data returns nothing
    if ( udg_CoopThrallAI ) then
        call CommandAI( Player(0), command, data )
    endif
endfunction

function CoopCommandJainaAI takes integer command, integer data returns nothing
    if ( udg_CoopJainaAI ) then
        call CommandAI( Player(9), command, data )
    endif
endfunction

function CoopCapturePlayerState takes nothing returns nothing
    local player localPlayer = GetLocalPlayer()
    local integer localId = GetPlayerId(localPlayer)
    call DestroyGroup( udg_CurrentSelection )
    set udg_CurrentSelection = GetUnitsSelectedAll(Player(1))
    if ( udg_CoopThrallHuman ) then
        call DestroyGroup( udg_CoopThrallSelection )
        set udg_CoopThrallSelection = GetUnitsSelectedAll(Player(0))
    endif
    if ( udg_CoopJainaHuman ) then
        call DestroyGroup( udg_CoopJainaSelection )
        set udg_CoopJainaSelection = GetUnitsSelectedAll(Player(9))
    endif
    if ( localPlayer == Player(1) or ( udg_CoopThrallHuman and localPlayer == Player(0) ) or ( udg_CoopJainaHuman and localPlayer == Player(9) ) ) then
        set udg_CoopCameraX[localId] = GetCameraTargetPositionX()
        set udg_CoopCameraY[localId] = GetCameraTargetPositionY()
    endif
    set localPlayer = null
endfunction

function CoopRestoreSelections takes nothing returns nothing
    call SelectGroupForPlayerBJ( udg_CurrentSelection, Player(1) )
    if ( udg_CoopThrallHuman ) then
        call SelectGroupForPlayerBJ( udg_CoopThrallSelection, Player(0) )
    endif
    if ( udg_CoopJainaHuman ) then
        call SelectGroupForPlayerBJ( udg_CoopJainaSelection, Player(9) )
    endif
endfunction

function CoopRestoreCameras takes nothing returns nothing
    local player localPlayer = GetLocalPlayer()
    local integer localId = GetPlayerId(localPlayer)
    if ( localPlayer == Player(1) or ( udg_CoopThrallHuman and localPlayer == Player(0) ) or ( udg_CoopJainaHuman and localPlayer == Player(9) ) ) then
        call ResetToGameCamera( 0.00 )
        call PanCameraToTimed( udg_CoopCameraX[localId], udg_CoopCameraY[localId], 0.00 )
    endif
    set localPlayer = null
endfunction

function CoopSelectStartingHeroes takes nothing returns nothing
    call SelectUnitForPlayerSingle( udg_Furion, Player(1) )
    if ( udg_CoopThrallHuman ) then
        call SelectUnitForPlayerSingle( udg_Thrall, Player(0) )
    endif
    if ( udg_CoopJainaHuman ) then
        call SelectUnitForPlayerSingle( udg_Jaina, Player(9) )
    endif
endfunction

function CoopFocusStartingHeroes takes nothing returns nothing
    call ResetToGameCameraForPlayer( Player(1), 0.00 )
    call PanCameraToTimedLocForPlayer( Player(1), GetRectCenter(gg_rct_FurionStart), 0.00 )
    if ( udg_CoopThrallHuman ) then
        call ResetToGameCameraForPlayer( Player(0), 0.00 )
        call PanCameraToTimedLocForPlayer( Player(0), GetRectCenter(gg_rct_ThrallStart), 0.00 )
    endif
    if ( udg_CoopJainaHuman ) then
        call ResetToGameCameraForPlayer( Player(9), 0.00 )
        call PanCameraToTimedLocForPlayer( Player(9), GetRectCenter(gg_rct_JainaStart), 0.00 )
    endif
endfunction

function CoopVictory takes nothing returns nothing
    if ( not udg_CoopResultIssued ) then
        set udg_CoopResultIssued = true
        set udg_GameOver = true
        if ( udg_CoopNightElfHuman ) then
            call CustomVictoryBJ( Player(1), false, true )
        endif
        if ( udg_CoopThrallHuman ) then
            call CustomVictoryBJ( Player(0), false, true )
        endif
        if ( udg_CoopJainaHuman ) then
            call CustomVictoryBJ( Player(9), false, true )
        endif
    endif
endfunction

function CoopDefeat takes string message returns nothing
    if ( not udg_CoopResultIssued ) then
        set udg_CoopResultIssued = true
        set udg_GameOver = true
        if ( udg_CoopNightElfHuman ) then
            call CustomDefeatBJ( Player(1), message )
        endif
        if ( udg_CoopThrallHuman ) then
            call CustomDefeatBJ( Player(0), message )
        endif
        if ( udg_CoopJainaHuman ) then
            call CustomDefeatBJ( Player(9), message )
        endif
    endif
endfunction

function CoopShowStartupWarnings takes nothing returns nothing
    if ( not udg_CoopNightElfHuman ) then
        call DisplayTimedTextToPlayer( Player(1), 0.00, 0.00, 30.00, GetLocalizedString("TRIGSTR_910") )
    endif
    if ( not udg_CoopThrallHuman and not udg_CoopThrallAI ) then
        call DisplayTimedTextToForce( udg_AAPG_NightElves, 30.00, GetLocalizedString("TRIGSTR_908") )
    endif
    if ( not udg_CoopJainaHuman and not udg_CoopJainaAI ) then
        call DisplayTimedTextToForce( udg_AAPG_NightElves, 30.00, GetLocalizedString("TRIGSTR_909") )
    endif
endfunction
""";

    private const string DifficultyFunctions = """
//===========================================================================
// Difficulty selection follows the generated trigger declarations.
//===========================================================================
function CoopFinishDifficultySelection takes gamedifficulty difficulty, string notice returns nothing
    if ( udg_CoopInitializationStarted ) then
        return
    endif
    set udg_CoopInitializationStarted = true
    call SetGameDifficulty( difficulty )
    call DisplayTimedTextToForce( udg_AAPG_NightElves, 5.00, GetLocalizedString(notice) )
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
    call InitCustomTriggers(  )
    call RunInitializationTriggers(  )
    call CoopShowStartupWarnings(  )
endfunction

function CoopChooseDifficulty takes nothing returns nothing
    if ( GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED ) then
        call CoopFinishDifficultySelection( MAP_DIFFICULTY_NORMAL, "TRIGSTR_905" )
    elseif ( GetClickedButton() == udg_CoopDifficultyHard ) then
        call CoopFinishDifficultySelection( MAP_DIFFICULTY_HARD, "TRIGSTR_906" )
    else
        call CoopFinishDifficultySelection( MAP_DIFFICULTY_NORMAL, "TRIGSTR_905" )
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
    call DisplayTimedTextToForce( udg_AAPG_NightElves, 30.00, GetLocalizedString("TRIGSTR_904") )
    call TimerStart( udg_CoopDifficultyTimer, 30.00, false, null )
endfunction

function CoopScheduleDifficultySelection takes nothing returns nothing
    set udg_CoopDifficultyStartupTimer = CreateTimer()
    call TimerStart( udg_CoopDifficultyStartupTimer, 0.01, false, function CoopBeginDifficultySelection )
endfunction
""";
}
