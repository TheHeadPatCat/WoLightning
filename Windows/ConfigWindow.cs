using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;

namespace WoLightning.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin Plugin;

    private String WordListInput = new String("");
    private int[] WordListSetting = new int[3];
    private String selectedWord = new String("");
    private int currentWordIndex = -1;

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

        
        if(ImGui.BeginTabBar("Tab Bar##tabbarmain", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("Social Settings"))
            {
                var ShockOnPat = Configuration.ShockOnPat;
                if (ImGui.Checkbox("Trigger when you get /pet", ref ShockOnPat))
                {
                    Configuration.ShockOnPat = ShockOnPat;
                    Configuration.Save();
                }

                if (ShockOnPat)
                {
                    var ShockPatSettings = Configuration.ShockPatSettings;
                    if (ImGui.ListBox("Mode##pat", ref ShockPatSettings[0], ["Shock", "Vibrate", "Beep"], 3))
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

                var ShockOnBadWord = Configuration.ShockOnBadWord;
                if (ImGui.Checkbox("Trigger when you say a specific word from a list.", ref ShockOnBadWord))
                {
                    Configuration.ShockOnBadWord = ShockOnBadWord;
                    Configuration.Save();
                }

                if(ShockOnBadWord)
                {
                    ImGui.Text("You can find the Settings for this option in the tab \"Word List\"");
                }

                ImGui.EndTabItem();
            }

            if (Configuration.ShockOnBadWord && ImGui.BeginTabItem("Word List"))
            {
                var SavedWordSettings = Configuration.ShockBadWordSettings;

                if(ImGui.InputTextWithHint("Word to add", "Click on a Entry to edit it.", ref WordListInput, 48))
                {
                    if (currentWordIndex != -1) // Get rid of the old settings, otherwise we build connections between two items
                    {
                        int[] copyArray = new int[3];
                        WordListSetting.CopyTo(copyArray, 0);
                        WordListSetting = copyArray;
                    }
                }

                ImGui.ListBox("Mode##Word", ref WordListSetting[0], ["Shock", "Vibrate", "Beep"], 3);
                ImGui.SliderInt("Intensity##WordInt", ref WordListSetting[1], 1, 100);
                ImGui.SliderInt("Duration##WordDur", ref WordListSetting[2], 1, 10);

                
                if(ImGui.Button("Add Word"))
                {
                    if(SavedWordSettings.ContainsKey(WordListInput))SavedWordSettings.Remove(WordListInput);
                    SavedWordSettings.Add(WordListInput,WordListSetting);
                    Configuration.ShockBadWordSettings = SavedWordSettings;
                    Configuration.Save();
                    currentWordIndex = -1;
                    WordListInput = new String("");
                    WordListSetting = new int[3];
                    selectedWord = new String("");
                }
                ImGui.SameLine();
                if (ImGui.Button("Remove Word"))
                {
                    if(SavedWordSettings.ContainsKey(WordListInput))SavedWordSettings.Remove(WordListInput);
                    Configuration.ShockBadWordSettings = SavedWordSettings;
                    Configuration.Save();
                    currentWordIndex = -1;
                    WordListInput = new String("");
                    WordListSetting = new int[3];
                    selectedWord = new String("");
                }
                
                ImGui.Spacing();
                ImGui.Spacing();


                
                if(ImGui.BeginListBox("Active Words"))
                {
                    int index = 0;
                    foreach (var (word, settings) in SavedWordSettings)
                    {
                        var modeInt = settings[0];
                        var mode = new String("");
                        bool is_Selected = (currentWordIndex == index);
                        switch (modeInt) { case 0: mode = "Shock"; break; case 1: mode = "Vibrate"; break; case 2: mode = "Beep"; break; };
                        if(ImGui.Selectable($" {word}   Mode: {mode}  Intensity: {settings[1]}  Duration: {settings[2]}", ref is_Selected))
                        {
                            selectedWord = word;
                            currentWordIndex = index;
                            WordListInput = word;
                            WordListSetting = settings;
                        }
                        index++;
                    }
                    ImGui.EndListBox();
                }

                ImGui.EndTabItem();
            }

            if(ImGui.BeginTabItem("Combat Settings"))
            {
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

                var ShockOnWipe = Configuration.ShockOnWipe;
                if (ImGui.Checkbox("Trigger whenever everyone dies. (Wipe)", ref ShockOnWipe))
                {
                    Configuration.ShockOnWipe = ShockOnWipe;
                    Configuration.Save();
                }

                if (ShockOnWipe)
                {
                    var ShockWipeSettings = Configuration.ShockWipeSettings;
                    if (ImGui.ListBox("Mode##Wipe", ref ShockWipeSettings[0], ["Shock", "Vibrate", "Beep"], 3))
                    {
                        Configuration.ShockWipeSettings = ShockWipeSettings;
                        Configuration.Save();
                    }
                    ImGui.SliderInt("Intensity##WipeInt", ref ShockWipeSettings[1], 1, 100);
                    ImGui.SliderInt("Duration##WipeDur", ref ShockWipeSettings[2], 1, 10);
                    ImGui.Spacing();
                    ImGui.Spacing();
                }
                ImGui.EndTabItem();
            }

            if(ImGui.BeginTabItem("General Settings"))
            {
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
                if (ImGui.InputTextWithHint("API Key", "You can find this under \"Account\".", ref PishockApiField, 64, ImGuiInputTextFlags.Password))
                {
                    Configuration.PishockApiKey = PishockApiField;
                    Configuration.Save();
                }

                if (ImGui.Button("Test Connection", new Vector2(200, 60)))
                {
                    Plugin.WebClient.sendRequest([1, 30, 1]);
                }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

    }
}
