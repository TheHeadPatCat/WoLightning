using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;
using WoLightning.Classes;
using WoLightning.Types;

namespace WoLightning.Windows;

public class MasterWindow : Window, IDisposable
{
    private readonly Plugin Plugin;
    public Configuration Configuration;
    public readonly ConfigWindow CopiedConfigWindow;

    private Player selectedMaster = null;
    private bool validating = false;


    public MasterWindow(Plugin plugin)
        : base("Master of Lightning##Master", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Plugin = plugin;
        Configuration = new Configuration();
        Configuration.Initialize(Plugin, true, Plugin.ConfigurationDirectoryPath);
        Flags = ImGuiWindowFlags.AlwaysUseWindowPadding;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(450, 520),
            MaximumSize = new Vector2(650, 800)
        };

        CopiedConfigWindow = new ConfigWindow(Plugin, Configuration, this);
        Plugin.WindowSystem.AddWindow(CopiedConfigWindow);
    }




    public void Dispose()
    {
        if (this.IsOpen) this.Toggle();
        Configuration.Dispose();
    }

    public override async void Draw()

    {
        if (Plugin.Authentification.HasMaster) drawIsSubmissive();
        else drawBecomeSubmissive();

        drawLine();

        if (Plugin.Authentification.IsMaster) drawIsMaster();
        else drawBecomeMaster();

    }



    private void drawBecomeSubmissive()
    {
        if (ImGui.CollapsingHeader("Become a Submissive"))
        {

            ImGui.Text("This Menu allows you to designate another Player as your Master.\nOnce they accept your request, they will gain access to the following:");
            ImGui.BulletText("Changing your Presets & Triggers");
            ImGui.BulletText("Disallowing you from changing your Settings");
            ImGui.BulletText("Stopping or Starting the Plugin at will");
            ImGui.BulletText("Several Master-specific features, like leashing you to them.");

            ImGui.TextWrapped("\nPlease make sure that you fully trust the person, as the only ways to being released again, is through their choice, or resetting your account.");


            ImGui.Text("\nPlease select the Player ingame, that you want to have as your Master.");

            IGameObject st = Plugin.TargetManager.Target;
            if (!validating && st != null && st.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
            {
                IPlayerCharacter st1 = (IPlayerCharacter)st;
                if (selectedMaster == null || selectedMaster.Name != st1.Name.ToString())
                {
                    selectedMaster = new Player(st1.Name.ToString(), (int)st1.HomeWorld.Id);
                }
            }

            string playerName = "None";
            if (selectedMaster != null) playerName = selectedMaster.Name;
            ImGui.BeginDisabled();
            ImGui.InputText("##selectedMaster", ref playerName, 512, ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (!validating && ImGui.Button("X##removeSelectedMaster")) selectedMaster = null;

            if (selectedMaster != null && !selectedMaster.equals(Plugin.LocalPlayer))
            {

                if (!validating && ImGui.Button("Request to become Master"))
                {
                    validating = true;
                    Plugin.WebClient.sendWebserverRequest(OperationCode.RequestBecomeSub, null , selectedMaster);
                }
                else if (validating)
                {
                    ImGui.BeginDisabled();
                    ImGui.Button("Requested, please wait...");
                    ImGui.EndDisabled();
                }
            }

        }
    }
    
    private void drawIsSubmissive()
    {
        ImGui.TextColored(new Vector4(1, 0.6f, 1, 1), "Submission Status");
        ImGui.Separator();
        ImGui.Text("You are currently bound to ");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.6f, 0, 0.6f, 1), Plugin.Authentification.Master.getFullName());

        if (Plugin.Authentification.isDisallowed) ImGui.TextColored(new Vector4(1, 0, 0, 1), "They do not allow you to change your Settings.");
        else ImGui.TextColored(new Vector4(0, 1, 0, 1), "They allow you to change your Settings.");


    }

    private void drawBecomeMaster()
    {
        if (ImGui.CollapsingHeader("Become a Master"))
        {
            ImGui.TextWrapped("To become a Master, you need to have someone else send a submission request to you.\nTo do this, they need to navigate to this Menu, and select the 'Become a Submissive' option.");

            ImGui.TextWrapped("Once someone submits to you, you'll get access to a Menu here, letting you control and change several options on their behalf.");
        }
    }

    private void drawIsMaster()
    {
        ImGui.Text("You currently control ");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.6f, 0, 0.6f, 1), $"{Plugin.Authentification.OwnedSubs.Count} Submissives.");
        ImGui.Separator();
        if(ImGui.Button("Open Preset Configurator"))CopiedConfigWindow.Toggle();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Text("Statuslist");
        foreach (var sub in Plugin.Authentification.OwnedSubs)
        {
            if (sub.Online == null || sub.PluginActive == null) return;
            ImGui.Bullet();
            ImGui.SameLine();
            if ((bool)sub.Online) ImGui.TextColored(new Vector4(0, 1, 0, 1), $"{sub.Name}##{sub.getFullName()}");
            else ImGui.TextColored(new Vector4(1, 0, 0, 1), $"{sub.Name}##{sub.getFullName()}");
            ImGui.SameLine();
            if ((bool)sub.PluginActive) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 1, 0, 1));
            else ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, 1));
            if(ImGui.Button("Pluginstate"))togglePluginState(sub);
            ImGui.PopStyleColor();
        }
    }

    private void togglePluginState(Player sub)
    {
        sub.PluginActive = !sub.PluginActive;
        Plugin.WebClient.sendWebserverRequest(OperationCode.OrderEnabledChange, sub.PluginActive + "", sub);
    }

    private void drawLine()
    {
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.BeginDisabled();
        ImGui.Button("##decoLine", new Vector2(ImGui.GetWindowWidth() - 15, 10));
        ImGui.EndDisabled();
        ImGui.Spacing();
        ImGui.Spacing();
    }

}
