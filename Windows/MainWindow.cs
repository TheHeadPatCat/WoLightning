using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;
using WoLightning.Types;

namespace WoLightning.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private int presetIndex = 0;
    private Vector4 activeColor = new Vector4(0, 1, 0, 1);
    private Vector4 deactivatedColor = new Vector4(1, 0, 0, 1);
    private string resetKeyInput = string.Empty;

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("Warrior of Lightning##Main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(280, 250),
            MaximumSize = new Vector2(280, 2000)
        };

        Plugin = plugin;
    }

    public void Dispose()
    {
        if (this.IsOpen) this.Toggle();
    }

    public override async void Draw()
    {
        try
        {
            switch (Plugin.WebClient.Status)
            {
                case ConnectionStatus.NotConnected:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), $"Not Connected to the Webserver."); break;
                case ConnectionStatus.Connected:
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), $"Connected!\nAverage Ping: {Plugin.WebClient.Ping()}ms"); break;
                case ConnectionStatus.Connecting:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Connecting to web server..."); break;
                case ConnectionStatus.UnknownUser:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "The Server does not know us. Registering..."); break;


                case ConnectionStatus.Outdated:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Can't Connect - Outdated Version!"); break;
                case ConnectionStatus.WontRespond:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), $"The Server is offline.\nRetrying in {(int)TimeSpan.FromMilliseconds(Plugin.WebClient.PingTimer.TimeLeft).TotalSeconds}s..."); break;
                case ConnectionStatus.FatalError:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Something went wrong!\nPlease check the /xllog window."); break;
                case ConnectionStatus.InvalidKey:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "The saved key does not match with the server.\nYou may only reset it by asking the dev."); break;


                case ConnectionStatus.Unavailable:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "The web server is temporarily unavailable.\nAll other functions still work."); break;
            }

            if (((int)Plugin.WebClient.Status) < 199)
            {
                ImGui.SameLine();
                if (ImGui.Button("O", new Vector2(30, 30)))
                {
                    Plugin.WebClient.establishWebserverConnection();
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Reconnect");
            }


            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
            presetIndex = Plugin.Configuration.PresetIndex;
            if (presetIndex == -1) Plugin.Configuration.Save();
            if (ImGui.Combo("", ref presetIndex, [.. Plugin.Configuration.PresetNames], Plugin.Configuration.Presets.Count, 6))
            {
                Plugin.Configuration.loadPreset(Plugin.Configuration.PresetNames[presetIndex]);
            }

            if (Plugin.NetworkWatcher.running) { ImGui.TextColored(activeColor, "The plugin is running."); }
            else { ImGui.TextColored(deactivatedColor, "The plugin is deactivated."); }
            if (Plugin.WebClient.failsafe) ImGui.TextColored(deactivatedColor, "Failsafe is engaged. Use /red to reactivate the plugin.");



            if (!Plugin.NetworkWatcher.running && ImGui.Button("Start Plugin", new Vector2(ImGui.GetWindowSize().X - 10, 50)))
            {
                Plugin.NetworkWatcher.Start();
            }
            else if (Plugin.NetworkWatcher.running && ImGui.Button("Stop Plugin", new Vector2(ImGui.GetWindowSize().X - 10, 50)))
            {
                Plugin.NetworkWatcher.Dispose();
            }



            var ActivateOnStart = Plugin.Configuration.ActivateOnStart;

            if (ImGui.Checkbox("Activate whenever the game starts.", ref ActivateOnStart))
            {
                Plugin.Configuration.ActivateOnStart = ActivateOnStart;
                Plugin.Configuration.Save();
            }
            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            if (ImGui.Button("Open Trigger Configuration", new Vector2(ImGui.GetWindowSize().X - 10, 25)))
            {
                Plugin.ToggleConfigUI();
            }



            ImGui.BeginDisabled();
            if (ImGui.Button("Master Mode", new Vector2(ImGui.GetWindowSize().X - 10, 25)))
            {
                Plugin.ToggleMasterUI();
            }
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) { ImGui.SetTooltip($"Temporarily Disabled until i have reworked the Server."); }




            if (ImGui.CollapsingHeader("Account & Shockers", ImGuiTreeNodeFlags.CollapsingHeader))
            {

                if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
                var PishockNameField = Plugin.Authentification.PishockName;
                if (ImGui.InputTextWithHint("##PishockUsername", "Pishock Username", ref PishockNameField, 24))
                    Plugin.Authentification.PishockName = PishockNameField;

                var PishockApiField = Plugin.Authentification.PishockApiKey;
                if (ImGui.InputTextWithHint("##PishockAPIKey", "API Key from \"Account\"", ref PishockApiField, 64, ImGuiInputTextFlags.Password))
                    Plugin.Authentification.PishockApiKey = PishockApiField;


                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                var PishockCodeField = Plugin.Authentification.PishockShareCode;
                ImGui.SetNextItemWidth(220);
                if (ImGui.InputTextWithHint("##PishockSharecode", "Sharecode from your Shocker", ref PishockCodeField, 256))
                {
                    if (PishockCodeField.StartsWith("https://pishock.com/#/Control?sharecode=")) PishockCodeField = PishockCodeField.Split("https://pishock.com/#/Control?sharecode=")[1];
                    Plugin.Authentification.PishockShareCode = PishockCodeField;
                }
                ImGui.SameLine();
                if (ImGui.Button("+ Add##registerShocker"))
                {
                    Plugin.PluginLog.Verbose(Plugin.Authentification.PishockShareCode);
                    Plugin.Authentification.PishockShockers.Add(new Shocker($"Shocker{Plugin.Authentification.PishockShockers.Count}", Plugin.Authentification.PishockShareCode));
                    Plugin.WebClient.requestPishockInfo(Plugin.Authentification.PishockShareCode);
                }
                int x = 0;


                ImGui.Text("Current Shockers:");
                while (Plugin.Authentification.PishockShockers.Count > x)
                {
                    Shocker target = Plugin.Authentification.PishockShockers[x];
                    string tName = target.Name;
                    ImGui.SetNextItemWidth(205);

                    ImGui.Text("Status: ");
                    ImGui.SameLine();
                    switch (target.Status)
                    {
                        case ShockerStatus.Online: ImGui.TextColored(new Vector4(0, 1, 0, 1), "Online!"); break;
                        case ShockerStatus.Paused: ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "Paused"); break;
                        case ShockerStatus.Unchecked: ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "Requesting Data..."); break;
                        default:
                            ImGui.TextColored(new Vector4(0.7f, 0, 0, 1), "Unavailable"); break;

                    }
                    ImGui.Text(target.Name);

                    ImGui.SameLine();
                    if (ImGui.Button($"Remove##remove{target.Code}"))
                    {
                        Plugin.Authentification.PishockShockers.Remove(target);
                    }
                    ImGui.Separator();
                    x++;
                }


                if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();

                if (ImGui.Button("Save & Test", new Vector2(ImGui.GetWindowSize().X - 10, 25)))
                {
                    Plugin.Authentification.Save();
                    Plugin.WebClient.requestPishockInfoAll();
                    //Plugin.WebClient.sendPishockTestAll();
                }
            }
        }
        catch (Exception e)
        {
            Plugin.PluginLog.Error("Something went terribly wrong!");
            Plugin.PluginLog.Error(e.ToString());
        }

    }
}
