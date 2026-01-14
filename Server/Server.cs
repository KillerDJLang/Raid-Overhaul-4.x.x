using System.Reflection;
using RaidOverhaulMain.Controllers;
using RaidOverhaulMain.Helpers;
using RaidOverhaulMain.Models;
using RaidOverhaulMain.Routers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;

[assembly: AssemblyTitle("Raid Overhaul Server")]
[assembly: AssemblyDescription("A large overhaul for raids including events, dead body clean up, and much more. Server component.")]
[assembly: AssemblyCopyright("Copyright © 2025 nameless")]
[assembly: AssemblyFileVersion("3.0.1")]

namespace RaidOverhaulMain;

public sealed record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "nameless.raidoverhaul.server";
    public override string Name { get; init; } = "Raid Overhaul Server";
    public override string Author { get; init; } = "nameless";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("3.0.1");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; } = true;
    public override string License { get; init; } = "CC BY-NC-ND 4.0";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public sealed class ROMain(
    ISptLogger<ROMain> logger,
    ROBoss roBoss,
    ROStaticRouter roStaticRouter,
    ROCustomItems roCustomItems,
    RODbEdits roDbEdits,
    ROTrader roTrader,
    ROHelpers helpers
) : IOnLoad
{
    public Task OnLoad()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var devFilesPath = Path.Combine("db", "devFiles");
        var presetsFilesPath = Path.Combine("db", "presets");
        var config = helpers.LoadConfig<ConfigFile>(assembly, "config", "config.json");
        var debugConfig = helpers.LoadConfig<DebugFile>(assembly, devFilesPath, "debugOptions.json");
        var eventsConfig = helpers.LoadConfig<EventsConfigFile>(assembly, "config", "eventWeightings.json");
        var seasonsConfig = helpers.LoadConfig<SeasonalProgression>(assembly, devFilesPath, "seasonsProgressionFile.json");
        var ammoListFile = helpers.LoadConfig<AmmoStackList>(assembly, devFilesPath, "ammoStackList.json");
        var presetFile = helpers.LoadConfig<Dictionary<MongoId, Preset>>(assembly, presetsFilesPath, "customPresets.json");
        var legionConfig = helpers.LoadConfig<LegionProgression>(assembly, "config", "legionProgressionFile.json");

        if (debugConfig.DebugMode && debugConfig.DumpData)
        {
            helpers.DumpDataMaps(assembly);
        }
        roBoss.PassBossConfigs(config, debugConfig, legionConfig);
        roTrader.PassTraderConfigs(config, debugConfig);
        roDbEdits.PassDbConfigs(config, ammoListFile, presetFile);
        roCustomItems.PassCustomItemConfigs(config, debugConfig);
        roStaticRouter.PassRouterConfigs(config, seasonsConfig, debugConfig, eventsConfig, legionConfig);

        roCustomItems.BuildCustomItems();
        roTrader.BuildTrader();
        roDbEdits.BuildDbEdits();
        roBoss.BuildBoss();
        ROLogger.Log(logger, "Raid Overhaul Finished Loaded", LogTextColor.Magenta);

        return Task.CompletedTask;
    }
}
