using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using War3Net.Build;
using War3Net.Build.Extensions;
using War3Net.Build.Info;
using War3Net.Build.Script;
using War3Net.CodeAnalysis.Jass;
using War3Net.IO.Mpq;

namespace Wc3CoopBuild;

internal sealed record BuildResult(string OutputPath, string SourceSha256, string OutputSha256);

internal static partial class Orc08Builder
{
    private const string ExpectedSourceSha256 = "DE602748F1F24CE5137790A50BDB2805527622272879723AEBDADD759FEF9AB7";
    private const int HumanExpeditionPlayerId = 9;

    public static BuildResult Build(string sourcePath, string outputPath)
    {
        var source = Path.GetFullPath(sourcePath);
        var output = Path.GetFullPath(outputPath);

        if (!File.Exists(source))
        {
            throw new FileNotFoundException("Original Orc08 map was not found.", source);
        }

        if (File.Exists(output))
        {
            throw new IOException($"Output already exists: {output}");
        }

        var sourceHash = Sha256(source);
        if (!sourceHash.Equals(ExpectedSourceSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Unsupported Orc08 source. Expected {ExpectedSourceSha256}, got {sourceHash}.");
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
        builder.AddFile(
            map.GetTriggerStringsFile(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true))
            ?? throw new InvalidDataException("Failed to serialize war3map.wts."));
        builder.SaveWithPreArchiveData(output);

        return new BuildResult(output, sourceHash, Sha256(output));
    }

    private static void PatchInfo(MapInfo info)
    {
        info.MapName = "驱逐恶魔 - 双人合作开发版";
        info.MapDescription = "玩家1控制完整兽人部落；玩家2可接管吉安娜的人类探险队。";
        info.RecommendedPlayers = "2";

        var player = info.Players.SingleOrDefault(candidate => candidate.Id == HumanExpeditionPlayerId)
            ?? throw new InvalidDataException("Expected player 10 human expedition slot was not found in Orc08 map info.");
        player.Controller = PlayerController.User;

        if (player.Race != PlayerRace.Human)
        {
            throw new InvalidDataException("Expected player 10 to remain the original Human faction.");
        }
    }

    private static void PatchStrings(Map map)
    {
        if (map.TriggerStrings is null)
        {
            return;
        }

        SetTriggerString(0, "驱逐恶魔 - 双人合作开发版");
        SetTriggerString(1, "2");
        SetTriggerString(2, "第八章：驱逐恶魔（双人合作开发版）");
        AddTriggerString(900, "选择难度");
        AddTriggerString(901, "普通");
        AddTriggerString(902, "困难");
        AddTriggerString(903, "正在等待玩家 1 选择难度（30 秒后默认普通）");
        AddTriggerString(904, "难度：普通");
        AddTriggerString(905, "难度：困难");

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

    private static string PatchScript(string source)
    {
        var script = NormalizeToLf(source);

        RequireCount(script, "function config takes nothing returns nothing", 1);
        RequireCount(script, "call SetPlayerController( Player(9), MAP_CONTROL_COMPUTER )", 1);
        RequireCount(script, "call StartCampaignAI( Player(9), \"o08_Jaina.ai\" )", 2);
        RequireCount(script, "call CommandAI( Player(9),", 4);
        RequireCount(script, "    set udg_SelectedUnits = GetUnitsSelectedAll(udg_Player)", 2);
        RequireCount(script, "    call SelectGroupBJ( udg_SelectedUnits )", 4);
        RequireCount(script, "    call SelectUnitSingle( udg_Thrall )", 2);
        RequireCount(script, "    call InitGameCacheBJ( \"Campaigns.w3v\" )", 1);
        RequireCount(script, "    call RestoreUnitLocFacingAngleBJ(", 3);
        RequireCount(script, "    call SaveGameCacheBJ( GetLastCreatedGameCacheBJ() )", 1);
        RequireCount(script, "    call TriggerRegisterPlayerEventEndCinematic(", 4);
        RequireCount(
            script,
            "    call PlayThematicMusicBJ( \"Sound\\\\Music\\\\mp3Music\\\\TragicConfrontation.mp3\" )\n" +
            "    set udg_SelectedUnits = GetUnitsSelectedAll(udg_Player)",
            1);

        script = script.Replace(
            "    group               udg_SelectedUnits          = null\n",
            "    group               udg_SelectedUnits          = null\n" +
            "    constant integer    COOP_HUMAN_EXPEDITION_PLAYER_ID = 9\n" +
            "    group               udg_CoopSelectedUnits      = null\n" +
            "    location            udg_CoopPlayerCameraPosition = null\n" +
            "    fogmodifier         udg_CoopWaterfallVis       = null\n" +
            "    boolean             udg_CoopPlayerTwoHuman     = false\n" +
            "    boolean             udg_CoopJainaAIStarted     = false\n" +
            "    boolean             udg_CoopResultIssued       = false\n" +
            "    dialog              udg_CoopDifficultyDialog   = null\n" +
            "    button              udg_CoopDifficultyNormal   = null\n" +
            "    button              udg_CoopDifficultyHard     = null\n" +
            "    trigger             udg_CoopDifficultyTrigger  = null\n" +
            "    timer               udg_CoopDifficultyStartupTimer = null\n" +
            "    timer               udg_CoopDifficultyTimer    = null\n" +
            "    boolean             udg_CoopInitializationStarted = false\n",
            StringComparison.Ordinal);

        script = script.Replace(
            "    set udg_SelectedUnits                          = CreateGroup()\n",
            "    set udg_SelectedUnits                          = CreateGroup()\n" +
            "    set udg_CoopSelectedUnits                      = CreateGroup()\n",
            StringComparison.Ordinal);

        script = script.Replace(
            "    call SetPlayerController( Player(9), MAP_CONTROL_COMPUTER )",
            "    call SetPlayerController( Player(9), MAP_CONTROL_USER )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call SetPlayerRaceSelectable( Player(0), true )",
            "    call SetPlayerRaceSelectable( Player(0), false )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call SetPlayerRaceSelectable( Player(9), true )",
            "    call SetPlayerRaceSelectable( Player(9), false )",
            StringComparison.Ordinal);

        RequireCount(script, "    call SetPlayerTechMaxAllowed( Player(0), 'Otch', 0 )", 1);
        script = script.Replace(
            "    call SetPlayerTechMaxAllowed( Player(0), 'Otch', 0 )",
            "    call SetPlayerTechMaxAllowed( Player(0), 'Otch', 0 )\n" +
            "    call SetPlayerTechMaxAllowed( Player(0), 'Oshd', 0 )",
            StringComparison.Ordinal);
        RequireCount(script, "    call SetPlayerTechMaxAllowed( Player(9), 'Hmkg', 0 )", 1);
        script = script.Replace(
            "    call SetPlayerTechMaxAllowed( Player(9), 'Hmkg', 0 )",
            "    call SetPlayerTechMaxAllowed( Player(9), 'Hmkg', 0 )\n" +
            "    call SetPlayerTechMaxAllowed( Player(9), 'Hblm', 0 )",
            StringComparison.Ordinal);

        foreach (var enemyPlayerId in new[] { 1, 3, 5, 6, 10 })
        {
            var selectable = $"    call SetPlayerRaceSelectable( Player({enemyPlayerId}), true )";
            var fixedRace = $"    call SetPlayerRaceSelectable( Player({enemyPlayerId}), false )";
            RequireCount(script, selectable, 1);
            script = script.Replace(selectable, fixedRace, StringComparison.Ordinal);
        }

        script = script.Replace(
            "    call InitGlobals(  )\n    call InitCustomTriggers(  )\n    call RunInitializationTriggers(  )",
            "    call InitGlobals(  )\n    call CoopClassifyPlayerTwo(  )\n    call CoopScheduleDifficultySelection(  )",
            StringComparison.Ordinal);

        script = script.Replace(
            "    call SetGamePlacement( MAP_PLACEMENT_USE_MAP_SETTINGS )",
            "    call SetGamePlacement( MAP_PLACEMENT_USE_MAP_SETTINGS )\n" +
            "    call SetMapFlag( MAP_LOCK_ALLIANCE_CHANGES, false )\n" +
            "    call SetMapFlag( MAP_ALLIANCE_CHANGES_HIDDEN, false )\n" +
            "    call SetMapFlag( MAP_LOCK_RESOURCE_TRADING, false )\n" +
            "    call SetMapFlag( MAP_RESOURCE_TRADING_ALLIES_ONLY, true )",
            StringComparison.Ordinal);

        script = script.Replace(
            "    call StartCampaignAI( Player(9), \"o08_Jaina.ai\" )",
            "    call CoopStartJainaAI(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "call CommandAI( Player(9),",
            "call CoopCommandJainaAI(",
            StringComparison.Ordinal);

        script = script.Replace(
            "    call SetPlayerStateBJ( Player(9), PLAYER_STATE_RESOURCE_GOLD, 10000 )\n" +
            "    call SetPlayerStateBJ( Player(9), PLAYER_STATE_RESOURCE_LUMBER, 10000 )",
            "    call CoopInitializeJainaEconomy(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call AdjustPlayerStateBJ( 200, Player(9), PLAYER_STATE_RESOURCE_GOLD )\n" +
            "    call AdjustPlayerStateBJ( 200, Player(9), PLAYER_STATE_RESOURCE_LUMBER )",
            "    call CoopGrantJainaAIBonus(  )",
            StringComparison.Ordinal);

        script = ReplaceFunction(
            script,
            "Trig_Game_Cache_Actions",
            """
function Trig_Game_Cache_Actions takes nothing returns nothing
    call ConditionalTriggerExecute( gg_trg_Game_Cache_Default_Thrall )
    call UnitAddItemByIdSwapped( 'nspi', udg_Thrall )
    call UnitAddItemByIdSwapped( 'ratc', udg_Thrall )
    call UnitAddItemByIdSwapped( 'rat9', udg_Thrall )
    call UnitAddItemByIdSwapped( 'cnob', udg_Thrall )
    call UnitAddItemByIdSwapped( 'rin1', udg_Thrall )
    call UnitAddItemByIdSwapped( 'rin1', udg_Thrall )
    call ConditionalTriggerExecute( gg_trg_Game_Cache_Default_Cairne )
    call UnitAddItemByIdSwapped( 'ofir', udg_Cairne )
    call UnitAddItemByIdSwapped( 'bgst', udg_Cairne )
    call UnitAddItemByIdSwapped( 'ckng', udg_Cairne )
    call UnitAddItemByIdSwapped( 'rwiz', udg_Cairne )
    call UnitAddItemByIdSwapped( 'pmna', udg_Cairne )
    call UnitAddItemByIdSwapped( 'rlif', udg_Cairne )
    set udg_Jaina = gg_unit_Hjai_0021
    call UnitAddItemByIdSwapped( 'ckng', udg_Jaina )
    call UnitAddItemByIdSwapped( 'hcun', udg_Jaina )
    call UnitAddItemByIdSwapped( 'ciri', udg_Jaina )
    call UnitAddItemByIdSwapped( 'cnob', udg_Jaina )
    call UnitAddItemByIdSwapped( 'gcel', udg_Jaina )
    call UnitAddItemByIdSwapped( 'bspd', udg_Jaina )
    set udg_Grom = gg_unit_Opgh_0020
endfunction
""");
        script = ReplaceFunction(
            script,
            "Trig_NextLevelPrep_Actions",
            """
function Trig_NextLevelPrep_Actions takes nothing returns nothing
endfunction
""");
        script = script.Replace(
            "        call RestoreUnitLocFacingAngleBJ( \"Grom\", \"Orc08\", GetLastCreatedGameCacheBJ(), udg_Player, GetRectCenter(gg_rct_Jaina_COP), 270.00 )",
            "        call CreateNUnitsAtLoc( 1, 'Ogrh', udg_Player, GetRectCenter(gg_rct_Jaina_COP), 270.00 )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call SaveGameCacheBJ( GetLastCreatedGameCacheBJ() )\n",
            string.Empty,
            StringComparison.Ordinal);
        script = script.Replace(
            "    call UnitRemoveItemSwapped( UnitItemInSlotBJ(udg_Thrall, 1), udg_Thrall )\n" +
            "    call CreateItemLoc( 'gsou', GetUnitLoc(udg_Thrall) )\n" +
            "    call UnitAddItemSwapped( GetLastCreatedItem(), udg_Thrall )\n" +
            "    call UnitAddItemSwapped( GetLastRemovedItem(), udg_Thrall )",
            "    call UnitAddItemByIdSwapped( 'gsou', udg_Thrall )",
            StringComparison.Ordinal);

        script = script.Replace(
            "call CustomVictoryBJ( udg_Player, false, true )",
            "call CoopVictory(  )",
            StringComparison.Ordinal);
        script = CustomDefeatRegex().Replace(script, "call CoopDefeat( $1 )");
        script = script.Replace(
            "return ( GetOwningPlayer(GetFilterUnit()) == udg_Player )\nendfunction\n\nfunction Trig_No_More_Waterfall_Func004001001002",
            "return CoopOwnsUnit(GetFilterUnit())\nendfunction\n\nfunction Trig_No_More_Waterfall_Func004001001002",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call CreateFogModifierRadiusLocBJ( true, Player(0), FOG_OF_WAR_VISIBLE, GetRectCenter(gg_rct_Waterfall_Sight_See), 1024.00 )\n" +
            "    set udg_WaterfallVis = GetLastCreatedFogModifier()",
            "    call CoopCreateWaterfallVision(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call DestroyFogModifier( udg_WaterfallVis )",
            "    call CoopDestroyWaterfallVision(  )",
            StringComparison.Ordinal);

        script = DuplicateMatchingLinesForHumanPlayer(
            script,
            line => line.Contains("TriggerRegisterPlayerEventEndCinematic", StringComparison.Ordinal) &&
                    line.Contains("Player(0)", StringComparison.Ordinal));
        script = DuplicateMatchingLinesForHumanPlayer(
            script,
            line => line.Contains("TriggerRegisterPlayerUnitEventSimple( gg_trg_No_More_Waterfall, Player(0)", StringComparison.Ordinal));

        script = DuplicateMatchingLinesForHumanPlayer(script, line =>
            CameraFunctions.Any(cameraFunction => line.Contains(cameraFunction, StringComparison.Ordinal)) &&
            line.Contains("Player(0)", StringComparison.Ordinal));
        script = DuplicateMatchingLinesForHumanPlayer(script, line =>
            CameraFunctions.Any(cameraFunction => line.Contains(cameraFunction, StringComparison.Ordinal)) &&
            (line.Contains("( udg_Player,", StringComparison.Ordinal) ||
             line.Contains("( udg_Player )", StringComparison.Ordinal)),
            "udg_Player",
            "Player(9)");

        script = script.Replace(
            "    set udg_SelectedUnits = GetUnitsSelectedAll(udg_Player)",
            "    set udg_SelectedUnits = GetUnitsSelectedAll(udg_Player)\n" +
            "    if ( udg_CoopPlayerTwoHuman ) then\n" +
            "        set udg_CoopSelectedUnits = GetUnitsSelectedAll(Player(9))\n" +
            "    endif",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call SelectGroupBJ( udg_SelectedUnits )",
            "    call CoopRestoreSelections(  )\n" +
            "    call CoopRestoreSavedCameras(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call SelectUnitSingle( udg_Thrall )",
            "    call CoopSelectStartingHeroes(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call PanCameraToTimedLocForPlayer( Player(9), GetRectCenter(gg_rct_Thrall_Town_Cam), 0 )",
            "    call PanCameraToTimedLocForPlayer( Player(9), GetRectCenter(gg_rct_JainasTown), 0 )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    set udg_PlayerCameraPosition = GetCameraTargetPositionLoc()",
            "    call CoopCaptureCameras(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call PlayThematicMusicBJ( \"Sound\\\\Music\\\\mp3Music\\\\TragicConfrontation.mp3\" )\n" +
            "    set udg_SelectedUnits = GetUnitsSelectedAll(udg_Player)",
            "    call PlayThematicMusicBJ( \"Sound\\\\Music\\\\mp3Music\\\\TragicConfrontation.mp3\" )\n" +
            "    call CoopCaptureCameras(  )\n" +
            "    set udg_SelectedUnits = GetUnitsSelectedAll(udg_Player)",
            StringComparison.Ordinal);
        script = script.Replace(
            "call PanCameraToTimedLocForPlayer( Player(9), udg_PlayerCameraPosition, 0.00 )",
            "call PanCameraToTimedLocForPlayer( Player(9), udg_CoopPlayerCameraPosition, 0.00 )",
            StringComparison.Ordinal);

        script = script.Replace(
            "    call EnableTrigger( gg_trg_Jaina_Assaults_Warsong )",
            "    call CoopEnableJainaAssaults(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call EnableTrigger( gg_trg_Jaina_Attacked_Message )",
            "    call CoopEnableJainaHelp(  )",
            StringComparison.Ordinal);
        script = InsertEarlyHumanReturn(script, "Trig_Jaina_OCO_Increment_Conditions", returnsBoolean: true);
        script = InsertEarlyHumanReturn(script, "Trig_Jaina_OCO_Decrement_Conditions", returnsBoolean: true);
        script = InsertEarlyHumanReturn(script, "Trig_Jaina_Attacked_Queue_Conditions", returnsBoolean: true);
        script = InsertEarlyHumanReturn(script, "Trig_Jaina_Attacked_Message_Actions", returnsBoolean: false);
        script = InsertEarlyHumanReturn(script, "Trig_Jaina_Attacked_Queue_Actions", returnsBoolean: false);
        script = InsertEarlyHumanReturn(script, "Trig_Jaina_Assault_Timer_Actions", returnsBoolean: false);

        script = script.Replace(
            "    call SetPlayerAllianceStateBJ( Player(9), udg_WarSong, bj_ALLIANCE_UNALLIED )",
            "    call SetPlayerAllianceStateBJ( Player(9), udg_WarSong, bj_ALLIANCE_UNALLIED )\n" +
            "    call CoopApplyHumanPlayerTwoDiplomacy(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call SetPlayerAllianceStateBJ( Player(9), Player(4), bj_ALLIANCE_UNALLIED )",
            "    call SetPlayerAllianceStateBJ( Player(9), Player(4), bj_ALLIANCE_UNALLIED )\n" +
            "    call CoopApplyHumanPlayerTwoDiplomacy(  )",
            StringComparison.Ordinal);
        script = script.Replace(
            "    call SetUnitColor( gg_unit_nbal_0010, PLAYER_COLOR_GREEN )",
            "    call CoopApplyHumanPlayerTwoDiplomacy(  )\n" +
            "    call SetUnitColor( gg_unit_nbal_0010, PLAYER_COLOR_GREEN )",
            StringComparison.Ordinal);

        script = script.Replace(
            "function InitGlobals takes nothing returns nothing\n",
            NormalizeToLf(CoopFunctions) + "\nfunction InitGlobals takes nothing returns nothing\n",
            StringComparison.Ordinal);
        script = script.Replace(
            "function main takes nothing returns nothing\n",
            NormalizeToLf(CoopDifficultyFunctions) + "\nfunction main takes nothing returns nothing\n",
            StringComparison.Ordinal);

        RequireCount(script, "call SetPlayerController( Player(9), MAP_CONTROL_USER )", 1);
        RequireCount(script, "call SetPlayerController( Player(COOP_HUMAN_EXPEDITION_PLAYER_ID), MAP_CONTROL_COMPUTER )", 1);
        RequireCount(script, "call SetPlayerTechMaxAllowed( Player(0), 'Oshd', 0 )", 1);
        RequireCount(script, "call SetPlayerTechMaxAllowed( Player(9), 'Hblm', 0 )", 1);
        foreach (var fixedRacePlayerId in new[] { 0, 1, 3, 5, 6, 9, 10 })
        {
            RequireCount(script, $"call SetPlayerRaceSelectable( Player({fixedRacePlayerId}), false )", 1);
            RequireCount(script, $"call SetPlayerRaceSelectable( Player({fixedRacePlayerId}), true )", 0);
        }
        RequireCount(script, "call CoopStartJainaAI(  )", 2);
        RequireCount(script, "call CoopCommandJainaAI(", 4);
        RequireCount(script, "call StartCampaignAI( Player(9), \"o08_Jaina.ai\" )", 1);
        RequireCount(script, "call CommandAI( Player(9), command, data )", 1);
        RequireCount(script, "call CoopInitializeJainaEconomy(  )", 1);
        RequireCount(script, "call CoopGrantJainaAIBonus(  )", 1);
        RequireCount(script, "call CoopVictory(  )", 2);
        RequireCount(script, "call CoopDefeat( ", 3);
        RequireCount(script, "call CoopClassifyPlayerTwo(  )", 1);
        RequireCount(script, "call CoopConfigurePlayerTwoSystems(  )", 1);
        RequireCount(script, "call CoopScheduleDifficultySelection(  )", 1);
        RequireCount(script, "call CoopBeginDifficultySelection(  )", 0);
        RequireCount(script, "call InitCustomTriggers(  )", 1);
        RequireCount(script, "call RunInitializationTriggers(  )", 1);
        RequireCount(script, "call SetGameDifficulty( difficulty )", 1);
        RequireCount(script, "call SetMapFlag( MAP_LOCK_ALLIANCE_CHANGES, false )", 1);
        RequireCount(script, "call SetMapFlag( MAP_ALLIANCE_CHANGES_HIDDEN, false )", 1);
        RequireCount(script, "function CoopApplyHumanPlayerTwoDiplomacy takes nothing returns nothing", 1);
        RequireCount(script, "call SetPlayerAllianceStateAllyBJ( Player(0), Player(9), true )", 1);
        RequireCount(script, "call SetPlayerAllianceStateAllyBJ( Player(9), Player(0), true )", 1);
        RequireCount(script, "call SetPlayerAllianceStateBJ( Player(0), Player(9), bj_ALLIANCE_ALLIED )", 0);
        RequireCount(script, "call SetPlayerAllianceStateBJ( Player(9), Player(0), bj_ALLIANCE_ALLIED )", 0);
        RequireCount(script, "call CoopApplyHumanPlayerTwoDiplomacy(  )", 8);
        RequireCount(script, "CoopRestoreEnemyDiplomacy", 0);
        RequireCount(script, "call CoopCaptureCameras(  )", 2);
        RequireCount(script, "call CoopRestoreSavedCameras(  )", 4);
        RequireCount(script, "call SetUnitOwner( udg_Cairne", 0);
        RequireCount(script, "SetPlayerController( Player(7), MAP_CONTROL_USER", 0);
        RequireCount(script, "EVENT_PLAYER_LEAVE", 0);
        RequireCount(script, "InitGameCacheBJ(", 0);
        RequireCount(script, "RestoreUnitLocFacingAngleBJ(", 0);
        RequireCount(script, "SaveGameCacheBJ(", 0);
        RequireCount(script, "SetCinematicAvailableBJ(", 0);
        RequireCount(script, "SetCampaignAvailableBJ(", 0);
        RequireCount(script, "SetMissionAvailableBJ(", 0);
        RequireCount(script, "GetPlayerStructureCount(udg_Player, true) <= 0", 1);

        return NormalizeToLf(script).Replace("\n", "\r\n", StringComparison.Ordinal);
    }

    private static string DuplicateMatchingLinesForHumanPlayer(
        string script,
        Func<string, bool> predicate,
        string sourcePlayer = "Player(0)",
        string coopPlayer = "Player(9)")
    {
        var lines = script.Split('\n');
        var output = new List<string>(lines.Length + 64);
        foreach (var line in lines)
        {
            output.Add(line);
            if (predicate(line))
            {
                var indent = line[..(line.Length - line.TrimStart().Length)];
                output.Add(indent + "if ( udg_CoopPlayerTwoHuman ) then");
                output.Add("    " + ReplaceFirst(line, sourcePlayer, coopPlayer));
                output.Add(indent + "endif");
            }
        }

        return string.Join("\n", output);
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

    private static string InsertEarlyHumanReturn(string script, string functionName, bool returnsBoolean)
    {
        var signature = $"function {functionName} takes nothing returns {(returnsBoolean ? "boolean" : "nothing")}\n";
        RequireCount(script, signature, 1);
        var earlyReturn = returnsBoolean
            ? "    if ( udg_CoopPlayerTwoHuman ) then\n        return false\n    endif\n"
            : "    if ( udg_CoopPlayerTwoHuman ) then\n        return\n    endif\n";
        return script.Replace(signature, signature + earlyReturn, StringComparison.Ordinal);
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
// Cooperative helpers injected by wc3-coop-build.
//===========================================================================
function CoopClassifyPlayerTwo takes nothing returns nothing
    set udg_CoopPlayerTwoHuman = ( GetPlayerController(Player(COOP_HUMAN_EXPEDITION_PLAYER_ID)) == MAP_CONTROL_USER and GetPlayerSlotState(Player(COOP_HUMAN_EXPEDITION_PLAYER_ID)) == PLAYER_SLOT_STATE_PLAYING )
    if ( not udg_CoopPlayerTwoHuman ) then
        call SetPlayerController( Player(COOP_HUMAN_EXPEDITION_PLAYER_ID), MAP_CONTROL_COMPUTER )
    endif
endfunction

function CoopConfigurePlayerTwoSystems takes nothing returns nothing
    if ( udg_CoopPlayerTwoHuman ) then
        call DisableTrigger( gg_trg_Jaina_Bonus_Cash )
        call DisableTrigger( gg_trg_Jaina_Assaults_Warsong )
        call DisableTrigger( gg_trg_Jaina_Assault_Timer )
        call DisableTrigger( gg_trg_Jaina_OCO_Increment )
        call DisableTrigger( gg_trg_Jaina_OCO_Decrement )
        call DisableTrigger( gg_trg_Jaina_Attacked_Message )
        call DisableTrigger( gg_trg_Jaina_Attacked_Queue )
    endif
endfunction

function CoopStartJainaAI takes nothing returns nothing
    if ( not udg_CoopPlayerTwoHuman and not udg_CoopJainaAIStarted ) then
        call StartCampaignAI( Player(9), "o08_Jaina.ai" )
        set udg_CoopJainaAIStarted = true
    endif
endfunction

function CoopCommandJainaAI takes integer command, integer data returns nothing
    if ( not udg_CoopPlayerTwoHuman ) then
        call CommandAI( Player(9), command, data )
    endif
endfunction

function CoopEnableJainaAssaults takes nothing returns nothing
    if ( not udg_CoopPlayerTwoHuman ) then
        call EnableTrigger( gg_trg_Jaina_Assaults_Warsong )
    endif
endfunction

function CoopEnableJainaHelp takes nothing returns nothing
    if ( not udg_CoopPlayerTwoHuman ) then
        call EnableTrigger( gg_trg_Jaina_Attacked_Message )
    endif
endfunction

function CoopInitializeJainaEconomy takes nothing returns nothing
    if ( udg_CoopPlayerTwoHuman ) then
        call SetPlayerStateBJ( Player(9), PLAYER_STATE_RESOURCE_GOLD, 1000 )
        call SetPlayerStateBJ( Player(9), PLAYER_STATE_RESOURCE_LUMBER, 400 )
    else
        call SetPlayerStateBJ( Player(9), PLAYER_STATE_RESOURCE_GOLD, 10000 )
        call SetPlayerStateBJ( Player(9), PLAYER_STATE_RESOURCE_LUMBER, 10000 )
    endif
endfunction

function CoopGrantJainaAIBonus takes nothing returns nothing
    if ( not udg_CoopPlayerTwoHuman ) then
        call AdjustPlayerStateBJ( 200, Player(9), PLAYER_STATE_RESOURCE_GOLD )
        call AdjustPlayerStateBJ( 200, Player(9), PLAYER_STATE_RESOURCE_LUMBER )
    endif
endfunction

function CoopSetEnemyPair takes player enemy returns nothing
    call SetPlayerAllianceStateBJ( Player(0), enemy, bj_ALLIANCE_UNALLIED )
    call SetPlayerAllianceStateBJ( enemy, Player(0), bj_ALLIANCE_UNALLIED )
    call SetPlayerAllianceStateBJ( Player(9), enemy, bj_ALLIANCE_UNALLIED )
    call SetPlayerAllianceStateBJ( enemy, Player(9), bj_ALLIANCE_UNALLIED )
endfunction

function CoopApplyHumanPlayerTwoDiplomacy takes nothing returns nothing
    if ( udg_CoopPlayerTwoHuman ) then
        call SetPlayerAllianceStateAllyBJ( Player(0), Player(9), true )
        call SetPlayerAllianceStateAllyBJ( Player(9), Player(0), true )
        call SetPlayerAlliance( Player(0), Player(9), ALLIANCE_SHARED_VISION, true )
        call SetPlayerAlliance( Player(9), Player(0), ALLIANCE_SHARED_VISION, true )
        call SetPlayerState( Player(0), PLAYER_STATE_ALLIED_VICTORY, 1 )
        call SetPlayerState( Player(9), PLAYER_STATE_ALLIED_VICTORY, 1 )
        call CoopSetEnemyPair( Player(1) )
        call CoopSetEnemyPair( Player(3) )
        call CoopSetEnemyPair( Player(4) )
        call CoopSetEnemyPair( Player(5) )
        call CoopSetEnemyPair( Player(6) )
        call CoopSetEnemyPair( Player(10) )
        call CoopSetEnemyPair( Player(PLAYER_NEUTRAL_AGGRESSIVE) )
    endif
endfunction

function CoopVictory takes nothing returns nothing
    if ( not udg_CoopResultIssued ) then
        set udg_CoopResultIssued = true
        set udg_GAMEOVER = true
        call CoopApplyHumanPlayerTwoDiplomacy(  )
        call CustomVictoryBJ( Player(0), false, true )
        if ( udg_CoopPlayerTwoHuman ) then
            call CustomVictoryBJ( Player(9), false, true )
        endif
    endif
endfunction

function CoopDefeat takes string message returns nothing
    if ( not udg_CoopResultIssued ) then
        set udg_CoopResultIssued = true
        set udg_GAMEOVER = true
        call CoopApplyHumanPlayerTwoDiplomacy(  )
        call CustomDefeatBJ( Player(0), message )
        if ( udg_CoopPlayerTwoHuman ) then
            call CustomDefeatBJ( Player(9), message )
        endif
    endif
endfunction

function CoopOwnsUnit takes unit whichUnit returns boolean
    return ( GetOwningPlayer(whichUnit) == Player(0) or ( udg_CoopPlayerTwoHuman and GetOwningPlayer(whichUnit) == Player(9) ) )
endfunction

function CoopCreateWaterfallVision takes nothing returns nothing
    call CreateFogModifierRadiusLocBJ( true, Player(0), FOG_OF_WAR_VISIBLE, GetRectCenter(gg_rct_Waterfall_Sight_See), 1024.00 )
    set udg_WaterfallVis = GetLastCreatedFogModifier()
    if ( udg_CoopPlayerTwoHuman ) then
        call CreateFogModifierRadiusLocBJ( true, Player(9), FOG_OF_WAR_VISIBLE, GetRectCenter(gg_rct_Waterfall_Sight_See), 1024.00 )
        set udg_CoopWaterfallVis = GetLastCreatedFogModifier()
    endif
endfunction

function CoopDestroyWaterfallVision takes nothing returns nothing
    call DestroyFogModifier( udg_WaterfallVis )
    if ( udg_CoopPlayerTwoHuman ) then
        call DestroyFogModifier( udg_CoopWaterfallVis )
    endif
endfunction

function CoopRestoreSelections takes nothing returns nothing
    call SelectGroupForPlayerBJ( udg_SelectedUnits, Player(0) )
    if ( udg_CoopPlayerTwoHuman ) then
        call SelectGroupForPlayerBJ( udg_CoopSelectedUnits, Player(9) )
    endif
endfunction

function CoopCaptureCameras takes nothing returns nothing
    if ( udg_PlayerCameraPosition != null ) then
        call RemoveLocation( udg_PlayerCameraPosition )
    endif
    set udg_PlayerCameraPosition = GetCameraTargetPositionLoc()
    if ( udg_CoopPlayerTwoHuman ) then
        if ( udg_CoopPlayerCameraPosition != null ) then
            call RemoveLocation( udg_CoopPlayerCameraPosition )
        endif
        set udg_CoopPlayerCameraPosition = GetCameraTargetPositionLoc()
    endif
endfunction

function CoopRestoreSavedCameras takes nothing returns nothing
    if ( udg_PlayerCameraPosition != null ) then
        call PanCameraToTimedLocForPlayer( Player(0), udg_PlayerCameraPosition, 0.00 )
        call RemoveLocation( udg_PlayerCameraPosition )
        set udg_PlayerCameraPosition = null
    endif
    if ( udg_CoopPlayerTwoHuman and udg_CoopPlayerCameraPosition != null ) then
        call PanCameraToTimedLocForPlayer( Player(9), udg_CoopPlayerCameraPosition, 0.00 )
        call RemoveLocation( udg_CoopPlayerCameraPosition )
        set udg_CoopPlayerCameraPosition = null
    endif
endfunction

function CoopSelectStartingHeroes takes nothing returns nothing
    call SelectUnitForPlayerSingle( udg_Thrall, Player(0) )
    if ( udg_CoopPlayerTwoHuman ) then
        call SelectUnitForPlayerSingle( udg_Jaina, Player(9) )
    endif
endfunction
""";

    private const string CoopDifficultyFunctions = """
//===========================================================================
// Difficulty selection follows the generated initialization functions.
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
    call CoopConfigurePlayerTwoSystems(  )
    call RunInitializationTriggers(  )
endfunction

function CoopChooseDifficulty takes nothing returns nothing
    if ( GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED ) then
        call DisplayTimedTextToPlayer( Player(0), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_904") )
        if ( udg_CoopPlayerTwoHuman ) then
            call DisplayTimedTextToPlayer( Player(9), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_904") )
        endif
        call CoopFinishDifficultySelection( MAP_DIFFICULTY_NORMAL )
    elseif ( GetClickedButton() == udg_CoopDifficultyHard ) then
        call DisplayTimedTextToPlayer( Player(0), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_905") )
        if ( udg_CoopPlayerTwoHuman ) then
            call DisplayTimedTextToPlayer( Player(9), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_905") )
        endif
        call CoopFinishDifficultySelection( MAP_DIFFICULTY_HARD )
    else
        call DisplayTimedTextToPlayer( Player(0), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_904") )
        if ( udg_CoopPlayerTwoHuman ) then
            call DisplayTimedTextToPlayer( Player(9), 0.00, 0.00, 5.00, GetLocalizedString("TRIGSTR_904") )
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
    if ( udg_CoopPlayerTwoHuman ) then
        call DisplayTimedTextToPlayer( Player(9), 0.00, 0.00, 30.00, GetLocalizedString("TRIGSTR_903") )
    endif
    call TimerStart( udg_CoopDifficultyTimer, 30.00, false, null )
endfunction

function CoopScheduleDifficultySelection takes nothing returns nothing
    set udg_CoopDifficultyStartupTimer = CreateTimer()
    call TimerStart( udg_CoopDifficultyStartupTimer, 0.01, false, function CoopBeginDifficultySelection )
endfunction
""";

    [GeneratedRegex(@"call CustomDefeatBJ\( udg_Player, ([^)]+) \)")]
    private static partial Regex CustomDefeatRegex();
}
