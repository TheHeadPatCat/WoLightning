using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;

namespace WoLightning.Windows;

public class MasterWindow : Window, IDisposable
{
    private readonly Plugin Plugin;
    public Configuration Configuration;
    public readonly ConfigWindow ConfigWindow;


    public string requestingSub = "";
    private Vector4 active = new Vector4(0,1,0,1);
    private Vector4 inactive = new Vector4(0, 1, 0, 1);
    public bool updating = false;

    bool errorEncountered = false;


    public MasterWindow(Plugin plugin)
        : base("Master of Lightning##Master", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Plugin = plugin;
        Configuration = new Configuration();
        Configuration.Initialize(true, Plugin.ConfigurationDirectoryPath);
        Flags = ImGuiWindowFlags.AlwaysUseWindowPadding;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(450, 520),
            MaximumSize = new Vector2(2000, 2000)
        };

        ConfigWindow = new ConfigWindow(Plugin, Configuration, this);
        Plugin.WindowSystem.AddWindow(ConfigWindow);
        if(Configuration.OwnedSubs.Count > 0) { } //setup timer
    }




    public void Dispose() {
        if (this.IsOpen) this.Toggle();
        Plugin.WindowSystem.RemoveWindow(ConfigWindow);
        ConfigWindow.Dispose();
        Configuration.Dispose();
    }

    public override async void Draw()

    {
        if (errorEncountered)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Seomething went really really wrong!!\nYou can check /xllog for the error");
            return;
        }
        ImGui.TextColored(new Vector4(1, 0, 0, 1), "VERY HEAVY WIP\n\nAlot of Buttons and things dont have instant feedback.\nThis is because the Client on the other side only updates every 15 seconds.\nPlease have some patience and dont spam the buttons.\n(It shouldnt break anything, but it might...)");

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();


        if (updating) ImGui.BeginDisabled();
        if(ImGui.Button(updating ? "Requesting..." : "Request Status"))UpdateStatus();
        if(updating) ImGui.EndDisabled();

        try
        {
            ImGui.Text("You currently own:");
            foreach (var sub in Configuration.OwnedSubs)
            {
                ImGui.Bullet();

                if (!Configuration.SubsIsActive.ContainsKey(sub)) break;

                if (ImGui.SmallButton($"O##toggle{sub}"))Plugin.WebClient.sendServerData(new NetworkPacket(["packet", "refplayer", "setpluginstate"], ["ordered to toggle", sub, Configuration.SubsIsActive[sub] ? "false" : "true"]));
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Toggle Plugin State");
                ImGui.SameLine();
                ImGui.TextColored(Configuration.SubsIsActive[sub] ? active : inactive,sub);
                ImGui.SameLine();
                ImGui.Text("  Preset:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(120);
                
                var p = Configuration.SubsActivePresetIndexes[sub];
                if (ImGui.Combo($"##presetbox{sub}", ref p, [.. Configuration.Presets.Keys], Configuration.Presets.Keys.Count))
                {
                    Configuration.SubsActivePresetIndexes[sub] = p;
                    Plugin.WebClient.sendServerData(new NetworkPacket(["packet", "refplayer", "importpreset"], ["ordered to swap", sub, Configuration.sharePreset(Configuration.Presets.Keys.ToArray()[p])]));
                    Configuration.Save();
                }
                var c = Configuration.SubsIsDisallowed[sub];
                ImGui.SameLine();
                if (ImGui.Checkbox($"Disallow\nSettings?##checkbox{sub}", ref c))
                {
                    Configuration.SubsIsDisallowed[sub] = c;
                    Plugin.WebClient.sendServerData(new NetworkPacket(["packet", "refplayer", "updatesetting"], ["ordered to update", sub, "isdisallowed" + "#" + c.ToString()]));
                    Configuration.Save();
                }
                ImGui.SameLine();
                if (ImGui.Button($"Unbind##unbind{sub}"))
                {
                    Plugin.WebClient.sendServerData(new NetworkPacket(["packet", "refplayer", "unbindsub"], ["unbind request", sub, "undefined"]));
                    Configuration.SubsIsDisallowed.Remove(sub);
                    Configuration.SubsActivePresetIndexes.Remove(sub); 
                    Configuration.OwnedSubs.Remove(sub); // these will cause a error, but thats okay
                    Configuration.Save();
                }
            }
        }
        catch (Exception e) {
            
            Plugin.PluginLog.Error(e.ToString());
            errorEncountered = true;
        }


        if (ImGui.Button("Open Preset Creator"))
        {
            ConfigWindow.Toggle();
        }
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Open a creator window to setup Presets without impacting your own!");

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        if (requestingSub.Length > 0)
        {
            ImGui.TextColored(new Vector4(0.7f, 0, 0.7f, 1), $"{requestingSub} is requesting to become your Sub!");

            if (ImGui.Button("Accept"))
            {
                if(!Configuration.OwnedSubs.Contains(requestingSub))Configuration.OwnedSubs.Add(requestingSub);
                Configuration.SubsActivePresetIndexes[requestingSub] = 0;
                Configuration.SubsIsDisallowed[requestingSub] = false;
                Configuration.SubsIsActive[requestingSub] = false;
                Plugin.Configuration.IsMaster = true;
                Plugin.WebClient.sendServerData(new NetworkPacket(["packet", "refplayer", "answermaster"], ["acceptrequest", requestingSub, "true"]));
                requestingSub = "";
                UpdateStatus();
                Configuration.Save();
                Plugin.Configuration.Save();
            }
            ImGui.SameLine();
            if (ImGui.Button("Refuse"))
            {
                if(!Plugin.Configuration.IsMaster)this.Toggle();
                Plugin.WebClient.sendServerData(new NetworkPacket(["packet", "refplayer", "answermaster"], ["acceptrequest", requestingSub, "false"]));
                requestingSub = "";
            }
        }


    }

    private void UpdateStatus()
    {
        updating = true;
        NetworkPacket output = new NetworkPacket();
        foreach (var sub in Configuration.OwnedSubs)
        {
            output.append(new NetworkPacket(["packet", "refplayer", "requestsubstatus"], ["request", sub, "undefined"]));
        }
        Plugin.WebClient.sendServerData(output);
    }


}
