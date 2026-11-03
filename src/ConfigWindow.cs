using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace StatusExpiryCallouts;

public sealed class ConfigWindow : Window
{
    private readonly Configuration config;
    private readonly Action save;
    private readonly IinactTtsClient tts;
    private readonly Action playSound;
    private readonly Action<string> reportError;
    private int thresholdInput;
    private int soundEffectInput;
    private string websocketUrl;

    public ConfigWindow(Configuration config, Action save, IinactTtsClient tts, Action playSound, Action<string> reportError)
        : base("Status Expiry Callouts")
    {
        this.config = config;
        this.save = save;
        this.tts = tts;
        this.playSound = playSound;
        this.reportError = reportError;
        this.thresholdInput = config.ExpiryThresholdMs;
        this.soundEffectInput = config.SoundEffectId;
        this.websocketUrl = config.IinactWebSocketUrl;
        this.Size = new System.Numerics.Vector2(520, 640);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        ImGui.TextWrapped("Call out selected statuses when their remaining duration reaches your threshold.");

        var ttsEnabled = config.IinactTtsEnabled;
        if (ImGui.Checkbox("Use IINACT TTS", ref ttsEnabled))
        {
            config.IinactTtsEnabled = ttsEnabled;
            save();
        }

        var fallbackEnabled = config.SoundFallbackEnabled;
        if (ImGui.Checkbox("Play sound if IINACT TTS fails", ref fallbackEnabled))
        {
            config.SoundFallbackEnabled = fallbackEnabled;
            save();
        }

        if (ImGui.InputInt("Expiry threshold (milliseconds)", ref thresholdInput))
        {
            thresholdInput = Math.Clamp(thresholdInput, 1, 600_000);
            config.ExpiryThresholdMs = thresholdInput;
            save();
        }

        if (ImGui.InputInt("Game sound effect (1-16)", ref soundEffectInput))
        {
            soundEffectInput = Math.Clamp(soundEffectInput, 1, 16);
            config.SoundEffectId = soundEffectInput;
            save();
        }

        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("IINACT WebSocket URL", ref websocketUrl, 256))
        {
            config.IinactWebSocketUrl = websocketUrl.Trim();
            save();
        }

        ImGui.TextWrapped("IINACT must be running with its OverlayPlugin WebSocket server enabled. Default URL is ws://127.0.0.1:10501/ws.");

        if (ImGui.Button("Test IINACT TTS"))
            _ = TestTtsAsync();

        ImGui.SameLine();
        if (ImGui.Button("Test sound"))
        {
            try
            {
                playSound();
            }
            catch (Exception ex)
            {
                reportError(ex.Message);
            }
        }

        ImGui.Separator();
        ImGui.Text("Tracked statuses");
        ImGui.TextDisabled("Alerts apply to statuses sourced by you, including your debuffs on enemies and your own buffs.");

        foreach (var group in StatusCatalog.All.GroupBy(x => x.Job))
        {
            ImGui.PushID(group.Key);
            if (ImGui.CollapsingHeader(group.Key, ImGuiTreeNodeFlags.DefaultOpen))
            {
                foreach (var entry in group)
                {
                    var isEnabled = entry.StatusIds.Any(config.EnabledStatuses.Contains);
                    if (ImGui.Checkbox(entry.Label, ref isEnabled))
                    {
                        foreach (var id in entry.StatusIds)
                        {
                            if (isEnabled)
                                config.EnabledStatuses.Add(id);
                            else
                                config.EnabledStatuses.Remove(id);
                        }

                        save();
                    }
                }
            }

            ImGui.PopID();
        }
    }

    private async Task TestTtsAsync()
    {
        try
        {
            await tts.SpeakAsync(config.IinactWebSocketUrl, "Status callouts test");
        }
        catch (Exception ex)
        {
            reportError(ex.Message);
        }
    }

}
