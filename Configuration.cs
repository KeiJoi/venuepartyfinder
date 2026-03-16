using Dalamud.Configuration;
using VenuePartyFinder.Models;
using VenuePartyFinder.Services;

namespace VenuePartyFinder;

public sealed class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public PartyFinderPreset Preset { get; set; } = PartyFinderPreset.CreateDefault();

    public bool AutoRefreshEnabled { get; set; } = true;

    public bool OpenNativePfDuringAutomation { get; set; } = true;

    public bool AutoOpenWindowOnLoad { get; set; }

    public string WarningMessageOverride { get; set; } = string.Empty;

    public DateTime LastRefreshAttemptUtc { get; set; } = DateTime.MinValue;

    public void Save() => Service.PluginInterface.SavePluginConfig(this);
}
