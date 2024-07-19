using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;

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
            switch (Plugin.WebClient.ConnectionStatus)
            {
                case "connected":
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), $"Connected! Pinging in {(int)TimeSpan.FromMilliseconds(Plugin.WebClient.UpdateTimer.TimeLeft).TotalSeconds}s..."); break;
                case "disconnected":
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), $"The Server is Offline.\nRetrying in {(int)TimeSpan.FromMilliseconds(Plugin.WebClient.UpdateTimer.TimeLeft).TotalSeconds}s..."); break;
                case "outdated":
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Can't Connect: Outdated Version!"); break;
                case "connecting":
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Connecting to Webserver..."); break;
                case "not started":
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Loading Plugin Configurations..."); break;
                case "cant connect":
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Something went wrong!\nPlease check the /xllog Window"); break;
                case "invalid key":
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "The saved key does not match with the Server.\nYou may only reset it by asking the Dev."); break;

            }

            if (Plugin.WebClient.ConnectionStatus != "connected" && Plugin.WebClient.ConnectionStatus != "connecting")
            {
                ImGui.SameLine();
                if (ImGui.Button("O", new Vector2(30, 30))) Plugin.WebClient.sendServerLogin();
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Reconnect");
            }

            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();

            if (ImGui.Combo("", ref presetIndex, [.. Plugin.Configuration.PresetNames], Plugin.Configuration.Presets.Count, 3))
            {
                Plugin.Configuration.loadPreset(Plugin.Configuration.PresetNames[presetIndex]);
            }

            if (Plugin.NetworkWatcher.running) { ImGui.TextColored(activeColor, "The Plugin is running."); }
            else { ImGui.TextColored(deactivatedColor, "The Plugin is deactivated."); }
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
            if (ImGui.Button("Open Configuration", new Vector2(ImGui.GetWindowSize().X - 10, 25)))
            {
                Plugin.ToggleConfigUI();
            }


            if (Plugin.Authentification.IsMaster)
            {
                if (ImGui.Button("Open Master Window", new Vector2(ImGui.GetWindowSize().X - 10, 25)))
                {
                    Plugin.ToggleMasterUI();
                }
            }



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
                    Plugin.Authentification.PishockShockerCodes.Add("Shocker" + Plugin.Authentification.PishockShockerCodes.Count, Plugin.Authentification.PishockShareCode);
                    //Plugin.Authentification.PishockShareCode = "";
                }
                int x = 0;


                ImGui.Text("Current Shockers:");
                while (Plugin.Authentification.PishockShockerCodes.Count > x)
                {
                    string Name = Plugin.Authentification.PishockShockerCodes.ElementAt(x).Key;
                    string Code = Plugin.Authentification.PishockShockerCodes.ElementAt(x).Value;
                    //Plugin.PluginLog.Verbose("- " + Name + " C: " + Code);
                    string tName = Name;
                    ImGui.SetNextItemWidth(205);
                    if (ImGui.InputText($"##nameof{Code}", ref tName, 32, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        Plugin.Authentification.PishockShockerCodes.Remove(Name);
                        Plugin.Authentification.PishockShockerCodes.Add(tName, Code);
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"Remove##remove{Code}"))
                    {
                        Plugin.Authentification.PishockShockerCodes.Remove(Name);
                    }
                    x++;
                }

                if (Plugin.Authentification.PishockShockerCodes.Count > 0) ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.8f), "Press [Enter] to finish renaming.");


                if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();

                if (ImGui.Button("Save & Test", new Vector2(ImGui.GetWindowSize().X - 10, 25)))
                {
                    Plugin.Authentification.Save();
                    Plugin.WebClient.sendRequestShock([1, 30, 1]);
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
