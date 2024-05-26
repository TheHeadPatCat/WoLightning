using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;

namespace WoLightning.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin Plugin;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("Warrior of Lightning Configuration")
    {
        Flags = ImGuiWindowFlags.AlwaysVerticalScrollbar;

        Size = new Vector2(525, 700);

        Configuration = plugin.Configuration;
        Configuration.Save(); //make sure all fields exist on first start
        Plugin = plugin;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
    }

    public override void Draw()
    {

        ImGui.Text("Social Settings");
        ImGui.Spacing();
        var ShockOnPat = Configuration.ShockOnPat;
        if (ImGui.Checkbox("Trigger when you get /pet", ref ShockOnPat))
        {
            Configuration.ShockOnPat = ShockOnPat;
            Configuration.Save();
        }

        if (ShockOnPat) {
            var ShockPatSettings = Configuration.ShockPatSettings;
            if(ImGui.ListBox("Mode##pat", ref ShockPatSettings[0], ["Shock", "Vibrate", "Beep"], 3))
            {
                Configuration.ShockPatSettings = ShockPatSettings;
                Configuration.Save();
            }
            ImGui.SliderInt("Intensity##patInt", ref ShockPatSettings[1], 1, 100);
            ImGui.SliderInt("Duration##patDur", ref ShockPatSettings[2], 1, 10);
            ImGui.Spacing();
            ImGui.Spacing();
        }

        var ShockOnDeathroll = Configuration.ShockOnDeathroll;
        if (ImGui.Checkbox("Trigger when you lose a Deathroll.", ref ShockOnDeathroll))
        {
            Configuration.ShockOnDeathroll = ShockOnDeathroll;
            Configuration.Save();
        }

        if (ShockOnDeathroll)
        {
            var ShockDeathrollSettings = Configuration.ShockDeathrollSettings;
            if (ImGui.ListBox("Mode##deathroll", ref ShockDeathrollSettings[0], ["Shock", "Vibrate", "Beep"], 3))
            {
                Configuration.ShockDeathrollSettings = ShockDeathrollSettings;
                Configuration.Save();
            }
            ImGui.SliderInt("Intensity##deathrollInt", ref ShockDeathrollSettings[1], 1, 100);
            ImGui.SliderInt("Duration##deathrollDur", ref ShockDeathrollSettings[2], 1, 10);
            ImGui.Spacing();
            ImGui.Spacing();
        }


        ImGui.Spacing();
        ImGui.Text("Combat Settings");
        ImGui.Spacing();


        var ShockOnVuln = Configuration.ShockOnVuln;
        if (ImGui.Checkbox("Trigger when you get a [Vulnerability Up] debuff", ref ShockOnVuln))
        {
            Configuration.ShockOnVuln = ShockOnVuln;
            Configuration.Save();
        }

        if (ShockOnVuln)
        {
            var ShockVulnSettings = Configuration.ShockVulnSettings;
            if (ImGui.ListBox("Mode##vuln", ref ShockVulnSettings[0], ["Shock", "Vibrate", "Beep"], 3))
            {
                Configuration.ShockVulnSettings = ShockVulnSettings;
                Configuration.Save();
            }
            ImGui.SliderInt("Intensity##vulnInt", ref ShockVulnSettings[1], 1, 100);
            ImGui.SliderInt("Duration##vulnDur", ref ShockVulnSettings[2], 1, 10);
            ImGui.Spacing();
            ImGui.Spacing();
        }

        var ShockOnDamage = Configuration.ShockOnDamage;
        if (ImGui.Checkbox("Trigger when you take damage from a ability (No Auto Attacks)", ref ShockOnDamage))
        {
            Configuration.ShockOnDamage = ShockOnDamage;
            Configuration.Save();
        }

        if (ShockOnDamage)
        {
            var ShockDamageSettings = Configuration.ShockDamageSettings;
            if (ImGui.ListBox("Mode##damage", ref ShockDamageSettings[0], ["Shock", "Vibrate", "Beep"], 3))
            {
                Configuration.ShockDamageSettings = ShockDamageSettings;
                Configuration.Save();
            }
            ImGui.SliderInt("Intensity##damageInt", ref ShockDamageSettings[1], 1, 100);
            ImGui.SliderInt("Duration##damageDur", ref ShockDamageSettings[2], 1, 10);
            ImGui.Spacing();
            ImGui.Spacing();
        }

        var ShockOnDeath = Configuration.ShockOnDeath;
        if (ImGui.Checkbox("Trigger whenever you die.", ref ShockOnDeath))
        {
            Configuration.ShockOnDeath = ShockOnDeath;
            Configuration.Save();
        }

        if (ShockOnDeath)
        {
            var ShockDeathSettings = Configuration.ShockDeathSettings;
            if (ImGui.ListBox("Mode##Death", ref ShockDeathSettings[0], ["Shock", "Vibrate", "Beep"], 3))
            {
                Configuration.ShockDeathSettings = ShockDeathSettings;
                Configuration.Save();
            }
            ImGui.SliderInt("Intensity##deathInt", ref ShockDeathSettings[1], 1, 100);
            ImGui.SliderInt("Duration##deathDur", ref ShockDeathSettings[2], 1, 10);
            ImGui.Spacing();
            ImGui.Spacing();
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.LabelText("", "Account Settings");
        ImGui.Spacing();

        var PishockNameField = Configuration.PishockName;
        if (ImGui.InputText("Pishock Username", ref PishockNameField, 24))
        {
            Configuration.PishockName = PishockNameField;
            Configuration.Save();
        }

        var PishockCodeField = Configuration.PishockShareCode;
        if (ImGui.InputTextWithHint("Generated Sharecode", "Get this from \"Share\" on your Shocker", ref PishockCodeField, 256, ImGuiInputTextFlags.Password))
        {
            if (PishockCodeField.StartsWith("https://pishock.com/#/Control?sharecode=")) PishockCodeField = PishockCodeField.Split("https://pishock.com/#/Control?sharecode=")[1];
            Configuration.PishockShareCode = PishockCodeField;
            Configuration.Save();
        }

        var PishockApiField = Configuration.PishockApiKey;
        if (ImGui.InputTextWithHint("API Key","You can find this under \"Account\".", ref PishockApiField, 64, ImGuiInputTextFlags.Password))
        {
            Configuration.PishockApiKey = PishockApiField;
            Configuration.Save();
        }

        if (ImGui.Button("Test Connection", new Vector2(200, 60)))
        {
            Plugin.WebClient.sendRequest([1, 30, 1]);
        }
    }
}
