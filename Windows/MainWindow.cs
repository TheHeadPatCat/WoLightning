using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;

namespace WoLightning.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
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
            MaximumSize = new Vector2(280, 500)
        };

        Plugin = plugin;
    }

    public void Dispose() {
        if (this.IsOpen) this.Toggle();
    }

#pragma warning disable CS1998 // This is referenced, just not in this context since dalamud isnt loaded
    public override async void Draw()
#pragma warning restore CS1998 //
    {

        // todo swap to switch probably
        if (Plugin.WebClient.ConnectionStatus == "connected") ImGui.TextColored(new Vector4(0, 1, 0, 1), $"Connected to Webserver!\nRequesting next Update in {(int)TimeSpan.FromMilliseconds(Plugin.WebClient.UpdateTimer.TimeLeft).TotalSeconds}s...");
        else if (Plugin.WebClient.ConnectionStatus == "disconnected") ImGui.TextColored(new Vector4(1, 0, 0, 1), $"The Server is Offline.\nRetrying in {(int)TimeSpan.FromMilliseconds(Plugin.WebClient.UpdateTimer.TimeLeft).TotalSeconds}s...");
        else if (Plugin.WebClient.ConnectionStatus == "outdated") ImGui.TextColored(new Vector4(1, 0, 0, 1), "Can't Connect: Outdated Version!");
        else if (Plugin.WebClient.ConnectionStatus == "connecting") ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Connecting to Webserver...");
        else if (Plugin.WebClient.ConnectionStatus == "not started") ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Loading Plugin Configurations...");
        else if (Plugin.WebClient.ConnectionStatus == "cant connect") ImGui.TextColored(new Vector4(1, 0, 0, 1), "Something went wrong!\nPlease check the /xllog Window");
        else if (Plugin.WebClient.ConnectionStatus == "invalid key") ImGui.TextColored(new Vector4(1, 0, 0, 1), "The saved key does not match with the Server.\nYou may only reset it by asking the Dev.");


        if (Plugin.WebClient.ConnectionStatus != "connected" && Plugin.WebClient.ConnectionStatus != "connecting")
        {
            ImGui.SameLine();
            if (ImGui.Button("O",new Vector2(30,30))) Plugin.WebClient.sendServerLogin();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Reconnect");
        }

        if (Plugin.Configuration.isDisallowed) ImGui.BeginDisabled();
        var presetIndex = Plugin.Configuration.ActivePresetIndex;
        if (ImGui.Combo("", ref presetIndex, [.. Plugin.Configuration.Presets.Keys], Plugin.Configuration.Presets.Keys.Count))
        {
            Plugin.Configuration.swapPreset(Plugin.Configuration.Presets.Keys.ToArray()[presetIndex]);
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
        if (Plugin.Configuration.isDisallowed) ImGui.EndDisabled();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        if (ImGui.Button("Open Configuration", new Vector2(ImGui.GetWindowSize().X - 10, 25)))
        {
            Plugin.ToggleConfigUI();
        }


        if (Plugin.Configuration.IsMaster)
        {
            if (ImGui.Button("Open Master Window", new Vector2(ImGui.GetWindowSize().X - 10, 25)))
            {
                Plugin.ToggleMasterUI();
            }
        }



        if (ImGui.CollapsingHeader("Account Settings", ImGuiTreeNodeFlags.CollapsingHeader))
        {

            if (Plugin.Configuration.isDisallowed) ImGui.BeginDisabled();
            var PishockNameField = Plugin.Authentification.PishockName;
            if (ImGui.InputTextWithHint("##PishockUsername", "Pishock Username", ref PishockNameField, 24))
            {
                Plugin.Authentification.PishockName = PishockNameField;
                //Plugin.Configuration.Save();
            }

            var PishockCodeField = Plugin.Authentification.PishockShareCode;
            if (ImGui.InputTextWithHint("##PishockSharecode", "Sharecode from your Shocker", ref PishockCodeField, 256, ImGuiInputTextFlags.Password))
            {
                if (PishockCodeField.StartsWith("https://pishock.com/#/Control?sharecode=")) PishockCodeField = PishockCodeField.Split("https://pishock.com/#/Control?sharecode=")[1];
                Plugin.Authentification.PishockShareCode = PishockCodeField;
                //Plugin.Configuration.Save();
            }

            var PishockApiField = Plugin.Authentification.PishockApiKey;
            if (ImGui.InputTextWithHint("##PishockAPIKey", "API Key from \"Account\"", ref PishockApiField, 64, ImGuiInputTextFlags.Password))
            {
                Plugin.Authentification.PishockApiKey = PishockApiField;
                //Plugin.Configuration.Save();
            }

            if (Plugin.Configuration.isDisallowed) ImGui.EndDisabled();

            if (ImGui.Button("Save & Test", new Vector2(ImGui.GetWindowSize().X - 10, 25)))
            {
                Plugin.Authentification.Save();
                Plugin.WebClient.sendRequestShock([1, 30, 1]);
            }
        }

    }
}
