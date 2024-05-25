using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;

namespace WoLightning.Windows;

public class MainWindow : Window, IDisposable
{
    private IDalamudTextureWrap? GoatImage;
    private Plugin Plugin;
    private Vector4 activeColor = new Vector4(0, 1, 0, 1);
    private Vector4 deactivatedColor = new Vector4(1, 0, 0, 1);

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, IDalamudTextureWrap? goatImage)
        : base("Warrior of Lightning##Main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(250, 200),
            MaximumSize = new Vector2(250, 200)
        };

        GoatImage = goatImage;
        

        Plugin = plugin;
    }

    public void Dispose() { }

    public override async void Draw()
    {
        if(Plugin.NetworkWatcher.running) { ImGui.TextColored(activeColor,"The Plugin is running."); }
        else { ImGui.TextColored(deactivatedColor,"The Plugin is deactivated."); }
        if (Plugin.WebClient.failsafe) ImGui.TextColored(deactivatedColor, "Failsafe is engaged. Use /red to reactivate the plugin.");
        if (ImGui.Button("Toggle",new Vector2(150,60)))
        {
            if (Plugin.NetworkWatcher.running) { Plugin.NetworkWatcher.Dispose(); }
            else { Plugin.NetworkWatcher.Start(); }
        }

        var ActivateOnStart = Plugin.Configuration.ActivateOnStart;
        if (ImGui.Checkbox("Activate whenever the game starts.", ref ActivateOnStart))
        {
            Plugin.Configuration.ActivateOnStart = ActivateOnStart;
            Plugin.Configuration.Save();
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        if (ImGui.Button("Open Configuration", new Vector2(130, 30)))
        {
            Plugin.ToggleConfigUI();
        }

        /*
        ImGui.Text("Have a goat:");
        if (GoatImage != null)
        {
            ImGuiHelpers.ScaledIndent(55f);
            ImGui.Image(GoatImage.ImGuiHandle, new Vector2(GoatImage.Width, GoatImage.Height));
            ImGuiHelpers.ScaledIndent(-55f);
        }
        else
        {
            ImGui.Text("Image not found.");
        }
        */
    }
}
