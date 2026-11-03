using Dalamud.Configuration;

namespace StatusExpiryCallouts;

public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public int ExpiryThresholdMs { get; set; } = 5000;
    public string IinactWebSocketUrl { get; set; } = "ws://127.0.0.1:10501/ws";
    public bool IinactTtsEnabled { get; set; } = true;
    public bool SoundFallbackEnabled { get; set; } = true;
    public int SoundEffectId { get; set; } = 1;
    public HashSet<uint> EnabledStatuses { get; set; } = StatusCatalog.All.SelectMany(x => x.StatusIds).ToHashSet();
}
