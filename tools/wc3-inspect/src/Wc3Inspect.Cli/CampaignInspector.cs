using War3Net.Build;
using War3Net.Build.Extensions;

namespace Wc3Inspect;

internal static class CampaignInspector
{
    public static object Inspect(string campaignPath)
    {
        var path = Path.GetFullPath(campaignPath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Campaign was not found.", path);
        }

        var warnings = new List<string>();
        var campaign = Campaign.Open(path, CampaignFiles.Info | CampaignFiles.TriggerStrings);
        TryLoad("war3campaign.imp", CampaignFiles.ImportedFiles, part => campaign.ImportedFiles = part.ImportedFiles);
        TryLoad("war3campaign.w3a", CampaignFiles.AbilityObjectData, part => campaign.AbilityObjectData = part.AbilityObjectData);
        TryLoad("war3campaign.w3h", CampaignFiles.BuffObjectData, part => campaign.BuffObjectData = part.BuffObjectData);
        TryLoad("war3campaign.w3b", CampaignFiles.DestructableObjectData, part => campaign.DestructableObjectData = part.DestructableObjectData);
        TryLoad("war3campaign.w3d", CampaignFiles.DoodadObjectData, part => campaign.DoodadObjectData = part.DoodadObjectData);
        TryLoad("war3campaign.w3t", CampaignFiles.ItemObjectData, part => campaign.ItemObjectData = part.ItemObjectData);
        TryLoad("war3campaign.w3u", CampaignFiles.UnitObjectData, part => campaign.UnitObjectData = part.UnitObjectData);
        TryLoad("war3campaign.w3q", CampaignFiles.UpgradeObjectData, part => campaign.UpgradeObjectData = part.UpgradeObjectData);
        var info = campaign.Info ?? throw new InvalidDataException("war3campaign.w3f is missing or unreadable.");
        var rawStrings = new
        {
            name = info.CampaignName,
            difficulty = info.CampaignDifficulty,
            author = info.CampaignAuthor,
            description = info.CampaignDescription,
        };
        campaign.LocalizeInfo();
        var file = new FileInfo(path);

        return new
        {
            schemaVersion = 1,
            kind = "campaign",
            source = new { path, size = file.Length, sha256 = Hashing.Sha256(path) },
            campaign = new
            {
                formatVersion = (int)info.FormatVersion,
                campaignVersion = info.CampaignVersion,
                editorVersion = info.EditorVersion,
                name = info.CampaignName,
                difficulty = info.CampaignDifficulty,
                author = info.CampaignAuthor,
                description = info.CampaignDescription,
                rawStrings,
                race = info.Race.ToString(),
                flags = info.CampaignFlags.ToString(),
                mapButtons = info.MapButtons.Select(button => new
                {
                    visibleInitially = button.IsVisibleInitially != 0,
                    chapter = button.Chapter,
                    title = button.Title,
                    path = button.MapFilePath,
                }).ToArray(),
                maps = info.Maps.Select(map => new { path = map.MapFilePath }).ToArray(),
            },
            content = new
            {
                triggerStringCount = campaign.TriggerStrings?.Strings.Count ?? 0,
                importedFileCount = campaign.ImportedFiles?.Files.Count,
                objectData = new
                {
                    abilities = campaign.AbilityObjectData is not null,
                    buffs = campaign.BuffObjectData is not null,
                    destructables = campaign.DestructableObjectData is not null,
                    doodads = campaign.DoodadObjectData is not null,
                    items = campaign.ItemObjectData is not null,
                    units = campaign.UnitObjectData is not null,
                    upgrades = campaign.UpgradeObjectData is not null,
                },
            },
            warnings,
        };

        void TryLoad(string component, CampaignFiles files, Action<Campaign> assign)
        {
            try
            {
                assign(Campaign.Open(path, files));
            }
            catch (Exception exception)
            {
                warnings.Add($"Could not parse optional {component}: {exception.Message}");
            }
        }
    }
}
