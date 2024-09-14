using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;
using WoLightning.Types;
using static WoLightning.Classes.ClientWebserver;
using static WoLightning.Classes.ClientPishock;
using System.Data;
using WoLightning.Classes;

namespace WoLightning.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private int presetIndex = 0;
    private Vector4 activeColor = new Vector4(0, 1, 0, 1);
    private Vector4 deactivatedColor = new Vector4(1, 0, 0, 1);
    private string resetKeyInput = string.Empty;

    private bool isEulaModalActive = false;
    private TimerPlus eulaTimer = new TimerPlus();

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
        eulaTimer.Interval = 16000;
        eulaTimer.AutoReset = false;
    }

    public void Dispose()
    {
        if (this.IsOpen) this.Toggle();

    }

    public override async void Draw()
    {

        try
        {
            /*
            switch (Plugin.ClientWebserver.Status)
            {
                case ConnectionStatus.NotStarted:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), $"Starting Plugin..."); break;
                case ConnectionStatus.NotConnected:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), $"Not Connected to the Webserver."); break;
                case ConnectionStatus.Connected:
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), $"Connected!\nAverage Ping: {Plugin.ClientWebserver.Ping()}ms"); break;
                case ConnectionStatus.Connecting:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Connecting to web server..."); break;
                case ConnectionStatus.UnknownUser:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "The Server does not know us. Registering..."); break;


                case ConnectionStatus.Outdated:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Can't Connect - Outdated Version!"); break;
                case ConnectionStatus.WontRespond:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), $"The Server is offline.\nRetrying in {(int)TimeSpan.FromMilliseconds(Plugin.ClientWebserver.PingTimer.TimeLeft).TotalSeconds}s..."); break;
                case ConnectionStatus.FatalError:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Something went wrong!\nPlease check the /xllog window."); break;
                case ConnectionStatus.InvalidKey:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "The saved key does not match with the server.\nYou may only reset it by asking the dev."); break;


                case ConnectionStatus.Unavailable:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "The web server is temporarily unavailable.\nAll other functions still work."); break;
                case ConnectionStatus.DevMode:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "The web server is being worked on.\nAll other functions still work."); break;
                default:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "Received unknown response - are we up to date?"); break;

            }
            
            if (((int)Plugin.ClientWebserver.Status) < 199)
            {
                ImGui.SameLine();
                if (ImGui.Button("O", new Vector2(30, 30)))
                {
                    Plugin.ClientWebserver.establishWebserverConnection();
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Reconnect");
            }
            */


            ImGui.Text("Pishock API");

            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("The Pishock API is where all the shocks get sent to!\nIf you are not connected to it, you cannot receive shocks."); }
            
            switch (Plugin.ClientPishock.Status)
            {
                case ConnectionStatusPishock.NotStarted:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Starting Plugin..."); break;

                case ConnectionStatusPishock.Connecting:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Connecting..."); break;

                case ConnectionStatusPishock.Unavailable:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Unable to Connect!"); break;

                case ConnectionStatusPishock.Connected:
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), $"Connected!"); break;
            }

            ImGui.Separator();

            ImGui.Text("Webserver API");

            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("The Webserver is used for things between players, like sharing Presets or Mastermode.\nIt has no impact on the Pishock stuff!"); }
            
            switch (Plugin.ClientWebserver.Status)
            {
                case ConnectionStatusWebserver.NotStarted:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Starting Plugin..."); break;

                case ConnectionStatusWebserver.Connecting:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Connecting to web server..."); break;

                case ConnectionStatusWebserver.EulaNotAccepted:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Eula isn't accepted. ");
                    ImGui.SameLine();
                    if (ImGui.Button("Open")) {
                        eulaTimer.Start();
                        isEulaModalActive = true; 
                        ImGui.OpenPopup("WoL Webserver Eula##webserverEula"); 
                    }
                    break;

                case ConnectionStatusWebserver.Outdated:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Can't Connect - Outdated Version!"); break;
                case ConnectionStatusWebserver.WontRespond:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Offline.\nRetrying in {(int)TimeSpan.FromMilliseconds(Plugin.ClientWebserver.PingTimer.TimeLeft).TotalSeconds}s..."); break;
                case ConnectionStatusWebserver.FatalError:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Something went wrong!\nPlease check the /xllog window."); break;
                case ConnectionStatusWebserver.InvalidKey:
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "The saved key does not match with the server.\nYou may only reset it by asking the dev."); break;
                case ConnectionStatusWebserver.DevMode:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "Can't Connect - Webserver is in construction."); break;

                case ConnectionStatusWebserver.Connected:
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), $"Connected!"); break;

                default:
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "Unknown Response."); break;

            }

            ImGui.Separator();


            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
            presetIndex = Plugin.Configuration.PresetIndex;
            if (presetIndex == -1) Plugin.Configuration.Save();
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 15);
            if (ImGui.Combo("", ref presetIndex, [.. Plugin.Configuration.PresetNames], Plugin.Configuration.Presets.Count, 6))
            {
                Plugin.Configuration.loadPreset(Plugin.Configuration.PresetNames[presetIndex]);
            }

            if (Plugin.NetworkWatcher.running) { ImGui.TextColored(activeColor, "The plugin is running."); }
            else { ImGui.TextColored(deactivatedColor, "The plugin is deactivated."); }
            if (Plugin.ClientWebserver.failsafe) ImGui.TextColored(deactivatedColor, "Failsafe is engaged. Use /red to reactivate the plugin.");

            if (!Plugin.NetworkWatcher.running && ImGui.Button("Start Plugin", new Vector2(ImGui.GetWindowSize().X - 15, 50)))
            {
                Plugin.NetworkWatcher.Start();
            }
            else if (Plugin.NetworkWatcher.running && ImGui.Button("Stop Plugin", new Vector2(ImGui.GetWindowSize().X - 15, 50)))
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
            if (ImGui.Button("Open Trigger Configuration", new Vector2(ImGui.GetWindowSize().X - 15, 25)))
            {
                Plugin.ToggleConfigUI();
            }

            if (Plugin.ClientWebserver.Status != ConnectionStatusWebserver.Connected) ImGui.BeginDisabled();
            if (ImGui.Button("Master Mode", new Vector2(ImGui.GetWindowSize().X - 15, 25)))
            {
                Plugin.ToggleMasterUI();
            }
            if (Plugin.ClientWebserver.Status != ConnectionStatusWebserver.Connected) ImGui.EndDisabled();

            if (Plugin.ClientWebserver.Status != ConnectionStatusWebserver.Connected && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) { ImGui.SetTooltip($"You need to be Connected to the Webserver\nto access Mastermode!"); }



            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 15);
            if (ImGui.CollapsingHeader("Account & Shockers", ImGuiTreeNodeFlags.CollapsingHeader))
            {
                ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 15);
                if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
                var PishockNameField = Plugin.Authentification.PishockName;
                if (ImGui.InputTextWithHint("##PishockUsername", "Pishock Username", ref PishockNameField, 24))
                    Plugin.Authentification.PishockName = PishockNameField;
                ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 15);
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
                    Plugin.Log(Plugin.Authentification.PishockShareCode);
                    Plugin.Authentification.PishockShockers.Add(new Shocker($"Shocker{Plugin.Authentification.PishockShockers.Count}", Plugin.Authentification.PishockShareCode));
                    Plugin.ClientPishock.info(Plugin.Authentification.PishockShareCode);
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
                        case ShockerStatus.Unchecked: ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "Requesting Data..."); break;
                        case ShockerStatus.InvalidUser: ImGui.TextColored(new Vector4(1, 0, 0, 1), "Invalid Userdata"); break;

                        case ShockerStatus.Online: ImGui.TextColored(new Vector4(0, 1, 0, 1), "Online!"); break;
                        case ShockerStatus.Paused: ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "Paused"); break;
                        case ShockerStatus.Offline: ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "Offline"); break;

                        case ShockerStatus.NotAuthorized: ImGui.TextColored(new Vector4(1, 0, 0, 1), "Not Authorized!"); break;
                        case ShockerStatus.DoesntExist: ImGui.TextColored(new Vector4(1, 0, 0, 1), "Invalid Sharecode!"); break;
                        case ShockerStatus.AlreadyUsed: ImGui.TextColored(new Vector4(1, 0, 0, 1), "Sharecode is already used!"); break;

                        default:
                            ImGui.TextColored(new Vector4(0.7f, 0, 0, 1), "Unknown Response"); break;

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

                if (ImGui.Button("Save & Test", new Vector2(ImGui.GetWindowSize().X - 15, 25)))
                {
                    Plugin.Authentification.Save();
                    Plugin.ClientPishock.testAll();
                    Plugin.validateShockerAssignments();
                    //Plugin.WebClient.sendPishockTestAll();
                }
            }
        }
        catch (Exception e)
        {
            Plugin.Error("Something went terribly wrong!", e);
        }


        Vector2 center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(450, 465));

        if (ImGui.BeginPopupModal("WoL Webserver Eula##webserverEula", ref isEulaModalActive, 
            ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup))
        {
            ImGui.TextWrapped("This is a simple Eula for the Webserver functionality of this Plugin.\n\nThe Webserver currently has these functions:\n");
            
            ImGui.BulletText("Allow storage of user configurations as backups.");
            ImGui.BulletText("Allow sharing of user made presets.");
            ImGui.BulletText("Usage of the Mastermode feature.");
            ImGui.BulletText("Usage of the Soulbound feature.");

            ImGui.TextWrapped("" +
                "\nTo use these features, a persistent user account will be created upon your first connection." +
                "\nThis account is valid for the currently played FF character." +
                "\nHowever this does also mean, your character Name and World will be saved serverside." +
                "\n\nYou will receive a unique Key used for logins, which will then be stored in the Authentification.json file." +
                "\nThere is no way to recover this key upon loss, so remember to keep this file safe or create a backup of it." +
                "\n\nUpon accepting this Eula, your account will be created.");
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "This is a non-reversible option.");


            ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
            if (eulaTimer.TimeLeft > 0)
            {
                ImGui.BeginDisabled();
                ImGui.Button($"Wait {(int)(eulaTimer.TimeLeft / 1000)}s...##eulaAccept", new Vector2(ImGui.GetWindowSize().X / 2 - 5, 25));
                ImGui.EndDisabled();
            }
            else
            {
                if (ImGui.Button("Accept##eulaAccept", new Vector2(ImGui.GetWindowSize().X / 2 - 5, 25)))
                {
                    Plugin.Authentification.acceptedEula = true;
                    Plugin.ClientWebserver.createHttpClient();
                    isEulaModalActive = false;
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.SameLine();
            ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
            if (ImGui.Button("Decline##eulaDecline", new Vector2(ImGui.GetWindowSize().X / 2 - 5, 25)))
            {
                isEulaModalActive = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

    }
}
