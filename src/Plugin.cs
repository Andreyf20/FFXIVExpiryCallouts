using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Collections.Concurrent;

namespace StatusExpiryCallouts;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/statuscallouts";
    private readonly WindowSystem windowSystem = new("StatusExpiryCallouts");
    private readonly IinactTtsClient tts = new();
    private readonly HashSet<(ulong ActorId, uint StatusId)> announced = [];
    private readonly Dictionary<ulong, DateTime> statusReadRetryAfter = [];
    private readonly ConcurrentQueue<byte> fallbackSounds = new();
    private readonly Configuration configuration;
    private readonly ConfigWindow configWindow;
    private bool statusReadWarningLogged;

    public Plugin()
    {
        configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        configuration.EnabledStatuses ??= StatusCatalog.All.SelectMany(x => x.StatusIds).ToHashSet();
        configuration.IinactWebSocketUrl ??= "ws://127.0.0.1:10501/ws";
        configuration.ExpiryThresholdMs = Math.Clamp(configuration.ExpiryThresholdMs, 1, 600_000);
        configuration.SoundEffectId = Math.Clamp(configuration.SoundEffectId, 1, 16);

        configWindow = new ConfigWindow(configuration, SaveConfiguration, tts, PlayConfiguredSound, message => Log.Error($"[StatusExpiryCallouts] {message}"));
        windowSystem.AddWindow(configWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo((_, _) => configWindow.Toggle())
        {
            HelpMessage = "Open Status Expiry Callouts settings.",
        });

        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        CommandManager.RemoveHandler(CommandName);
        windowSystem.RemoveAllWindows();
    }

    private void SaveConfiguration() => PluginInterface.SavePluginConfig(configuration);

    private void OnFrameworkUpdate(IFramework framework)
    {
        while (fallbackSounds.TryDequeue(out _))
            PlayConfiguredSound();

        var localPlayer = ObjectTable.LocalPlayer;
        if (localPlayer is null)
        {
            announced.Clear();
            return;
        }

        var active = new HashSet<(ulong ActorId, uint StatusId)>();
        var visibleBattleActorIds = new HashSet<ulong>();
        var thresholdSeconds = configuration.ExpiryThresholdMs / 1000f;
        var now = DateTime.UtcNow;

        foreach (var actor in ObjectTable)
        {
            if (actor is not IBattleChara battleChara)
                continue;

            var actorId = battleChara.GameObjectId;
            visibleBattleActorIds.Add(actorId);
            if (statusReadRetryAfter.TryGetValue(actorId, out var retryAfter) && now < retryAfter)
                continue;
            statusReadRetryAfter.Remove(actorId);

            try
            {
                foreach (var status in battleChara.StatusList)
                {
                    if (status.SourceId != localPlayer.EntityId || !configuration.EnabledStatuses.Contains(status.StatusId))
                        continue;

                    var remaining = status.RemainingTime;
                    if (remaining <= 0)
                        continue;

                    var key = (actorId, status.StatusId);
                    active.Add(key);

                    if (remaining > thresholdSeconds)
                    {
                        announced.Remove(key);
                        continue;
                    }

                    if (announced.Add(key))
                    {
                        var name = GetStatusName(status.StatusId, status.GameData.Value.Name.ToString());
                        if (configuration.IinactTtsEnabled)
                            _ = SendCalloutAsync(name);
                        else if (configuration.SoundFallbackEnabled)
                            PlayConfiguredSound();
                    }
                }
            }
            catch (Exception ex)
            {
                statusReadRetryAfter[actorId] = now.AddSeconds(5);
                if (!statusReadWarningLogged)
                {
                    Log.Warning($"[StatusExpiryCallouts] Skipping actor {actorId}; could not read its status list: {ex}");
                    statusReadWarningLogged = true;
                }
            }
        }

        foreach (var actorId in statusReadRetryAfter.Keys.Where(actorId => !visibleBattleActorIds.Contains(actorId)).ToArray())
            statusReadRetryAfter.Remove(actorId);

        announced.RemoveWhere(key => !active.Contains(key));
    }

    private static string GetStatusName(uint statusId, string gameName)
    {
        if (!string.IsNullOrWhiteSpace(gameName))
            return gameName;

        return StatusCatalog.All.FirstOrDefault(x => x.StatusIds.Contains(statusId))?.Label ?? "Status expired";
    }

    private async Task SendCalloutAsync(string name)
    {
        try
        {
            await tts.SpeakAsync(configuration.IinactWebSocketUrl, name);
        }
        catch (Exception ex)
        {
            Log.Warning($"[StatusExpiryCallouts] IINACT TTS failed: {ex.Message}");
            if (configuration.SoundFallbackEnabled)
                fallbackSounds.Enqueue(0);
        }
    }

    private void PlayConfiguredSound() => UIGlobals.PlayChatSoundEffect((uint)configuration.SoundEffectId);
}
