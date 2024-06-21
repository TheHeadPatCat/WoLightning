using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using WoLightning.Types;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WoLightning.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin Plugin;



    // Badword List
    private String WordListInput = new String("");
    private int[] WordListSetting = new int[3];
    private String selectedWord = new String("");
    private int currentWordIndex = -1;


    // Permission List
    private String PermissionListInput = new String("");
    private int PermissionListSetting = -1;
    private int PermissionListLevel = 0;
    private int currentPermissionIndex = -1;
    private String selectedPlayerName = new String("");


    // Sharewindows
    private Vector4 redCol = new Vector4(1, 0, 0, 1);
    private bool isAddModalOpen = true;
    private bool isRemoveModalOpen = true;
    private bool isShareModalOpen = true;
    private string addInput = "";
    private string importInput = "";
    private string exportInput = "";


    // MasterWindow Config
    private bool isAlternative = false;
    private MasterWindow? Parent;
    private int selectedSubIndex = 0;
    private string selectedSubNameFull;

    // Debug stuffs
    private int debugFtype = 0;
    private string debugFsender = "";
    private string debugFmessage = "";
    public string debugKmessage = "";
    private string debugRmessage = "";
    private string debugCstring = "";


    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("Warrior of Lightning Configuration##configmain")
    {
        Flags = ImGuiWindowFlags.AlwaysUseWindowPadding;


        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(580, 620),
            MaximumSize = new Vector2(2000, 2000)
        };

        Configuration = plugin.Configuration;
        Configuration.Save(); //make sure all fields exist on first start
        Plugin = plugin;

    }

    public ConfigWindow(Plugin plugin, Configuration configuration, MasterWindow parent) : base("Master of Lightning Configuration##configmaster")
    {
        Flags = ImGuiWindowFlags.AlwaysUseWindowPadding;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(580, 620),
            MaximumSize = new Vector2(2000, 2000)
        };

        Configuration = configuration;
        Configuration.Save(); //make sure all fields exist on first start
        Plugin = plugin;
        isAlternative = true;
        Parent = parent;
    }

    public void Dispose() {
        if (this.IsOpen) this.Toggle();
        Configuration.Save();
    }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
    }

    public override void Draw() // TODO make this entire thing dynamic (if performance allows it)
    {

        DrawHeader();

        if (ImGui.BeginTabBar("Tab Bar##tabbarmain", ImGuiTabBarFlags.None))
        {
            DrawGeneralTab();
            DrawDefaultTriggerTab();
            if(Configuration.ShockOnBadWord)
            {
                DrawWordlistTab();
            }
            DrawCustomTriggerTab();
            DrawPermissionsTab();
            DrawCommandTab();
            DrawDebugTab();

            ImGui.EndTabBar();
        }




    }

    private void DrawHeader()
    {
        if (Configuration.Version < new Configuration().Version)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "YOUR CONFIGURATION IS OUTDATED!");
            if (ImGui.Button("Reset & Update Config"))
            {
                if (isAlternative) Plugin.ToggleMasterConfigUI();
                else Plugin.ToggleConfigUI();
                Configuration = new Configuration();
                Configuration.Initialize(isAlternative, Plugin.ConfigurationDirectoryPath, true);
                Plugin.sendNotif("Your configuration has been reset!");

                Configuration.Save();
                if (!isAlternative) Plugin.Configuration = Configuration;
                else Parent.Configuration = Configuration;
            }
        }


        if (Configuration.HasMaster)
        {
            ImGui.Text("Your Master is currently " + Configuration.MasterNameFull);
        }

        if (Configuration.isDisallowed)
        {
            ImGui.TextColored(redCol, $"They do not allow you to change your Settings.");
            ImGui.BeginDisabled();
        }

        DrawPresetHeader();
    }

    private void DrawPresetHeader()
    {
        var presetIndex = Configuration.ActivePresetIndex;
        ImGui.PushItemWidth(ImGui.GetWindowSize().X - 90);

        if (ImGui.Combo("", ref presetIndex, [.. Configuration.Presets.Keys], Configuration.Presets.Keys.Count, 3))
        {
            Configuration.swapPreset(Configuration.Presets.Keys.ToArray()[presetIndex]);
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("+"))
        {
            importInput = "";
            addInput = "";
            ImGui.OpenPopup("Add Preset##addPreMod");
        };

        if (Configuration.isDisallowed) ImGui.EndDisabled();
        ImGui.SameLine();
        if (ImGui.SmallButton(">>"))
        {
            exportInput = "";
            ImGui.OpenPopup("Share Preset##shaPreMod");
        };

        if (Configuration.isDisallowed) ImGui.BeginDisabled();
        ImGui.SameLine();
        if (ImGui.SmallButton("X"))
        {
            ImGui.OpenPopup("Delete Preset##delPreMod");
        };
        if (Configuration.isDisallowed) ImGui.EndDisabled();




        Vector2 center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(300, 150));

        if (ImGui.BeginPopupModal("Add Preset##addPreMod", ref isAddModalOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.Text("Please name your new preset.");
            ImGui.PushItemWidth(ImGui.GetWindowSize().X - 10);
            if (importInput.Length != 0) ImGui.BeginDisabled();
            ImGui.InputText("##addInput", ref addInput, 32, ImGuiInputTextFlags.CharsNoBlank);
            if (importInput.Length != 0) ImGui.EndDisabled();

            ImGui.Text("Or enter a Sharestring to import.");
            ImGui.PushItemWidth(ImGui.GetWindowSize().X - 10);
            if (addInput.Length != 0) ImGui.BeginDisabled();
            ImGui.InputText("##importInput", ref importInput, 256, ImGuiInputTextFlags.CharsNoBlank);
            if (addInput.Length != 0) ImGui.EndDisabled();

            ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
            if (ImGui.Button("Add##addPre", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
            {
                if (addInput.Length > 0)
                {
                    string[] codes;
                    Configuration.Presets.TryGetValue(Configuration.ActivePreset, out codes);
                    Configuration.savePreset(addInput, codes);
                    Configuration.swapPreset(addInput);
                }
                if (importInput.Length > 0)
                {
                    Configuration.importPreset(importInput);
                }

                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel##canPre", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25))) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }

        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(290, 120));

        if (ImGui.BeginPopupModal("Share Preset##shaPreMod", ref isShareModalOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.TextWrapped("Use this String to share your current Preset!");
            ImGui.PushItemWidth(ImGui.GetWindowSize().X - 10);
            ImGui.InputText("", ref exportInput, 256, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);
            if (ImGui.Button("Generate##shaGen", new Vector2(ImGui.GetWindowSize().X - 10, 25))) exportInput = Configuration.sharePreset(Configuration.ActivePreset);
            if (ImGui.Button("Close##shaClo", new Vector2(ImGui.GetWindowSize().X - 10, 25))) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }


        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(250, 85));

        if (ImGui.BeginPopupModal("Delete Preset##delPreMod", ref isRemoveModalOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.TextWrapped("Are you sure you want to delete this preset?.");
            ImGui.PushItemWidth(ImGui.GetWindowSize().X - 10);
            if (ImGui.Button("Confirm##conRem", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
            {
                if (Configuration.ActivePreset == "Default")
                {
                    ImGui.CloseCurrentPopup();
                }
                else
                {
                    Configuration.Presets.Remove(Configuration.ActivePreset);
                    Configuration.swapPreset("Default", true);
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel##canRem", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25))) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    #region Tabs
    private void DrawGeneralTab()
    {
        if (ImGui.BeginTabItem("General Settings"))
        {
            if (Configuration.isDisallowed) ImGui.BeginDisabled();
            var IsPassthroughAllowed = Configuration.IsPassthroughAllowed;

            if (ImGui.Checkbox("Allow passthrough of triggers?", ref IsPassthroughAllowed))
            {
                Configuration.IsPassthroughAllowed = IsPassthroughAllowed;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Will make it possible for multiple Triggers to happen at once (ex. Damage and Death)"); }




            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();

            if (!isAlternative && !Configuration.HasMaster)
            {
                ImGui.Text($"Assign a Master\nCurrently targeted:");

                if (Plugin.ClientState.LocalPlayer != null && Plugin.ClientState.LocalPlayer.TargetObject != null && Plugin.ClientState.LocalPlayer.TargetObject.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(1, 1, 1, 1), "\n" + Plugin.ClientState.LocalPlayer.TargetObject.Name.ToString());
                    if (Configuration.MasterNameFull == "")
                    {
                        if (ImGui.Button("Send Request to targeted Player"))
                        {

                            if (Plugin.ClientState.LocalPlayer.TargetObject == null
                                || Plugin.ClientState.LocalPlayer.TargetObject.ObjectKind !=
                                Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player) return;

                            Configuration.MasterNameFull = ((PlayerCharacter)Plugin.ClientState.LocalPlayer.TargetObject).Name.ToString() + "#" + ((PlayerCharacter)Plugin.ClientState.LocalPlayer.TargetObject).HomeWorld.Id;
                            Plugin.WebClient.sendServerData(new NetworkPacket(["packet", "refplayer", "requestmaster"], ["attempt", Configuration.MasterNameFull, "undefined"]));

                        }
                    }
                }
                if (Configuration.MasterNameFull != "" && !Configuration.HasMaster)
                {
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), $"Waiting for response from {Configuration.MasterNameFull}...");
                }
            }

            if (Configuration.isDisallowed) ImGui.EndDisabled();
            ImGui.EndTabItem();
        }
    }
    private void DrawWordlistTab()
    {
        if (ImGui.BeginTabItem("Word List"))
        {
            if (Configuration.isDisallowed) ImGui.BeginDisabled();
            var SavedWordSettings = Configuration.ShockBadWordSettings;

            if (ImGui.InputTextWithHint("Word to add", "Click on a Entry to edit it.", ref WordListInput, 48))
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


            if (ImGui.Button("Add Word"))
            {
                if (SavedWordSettings.ContainsKey(WordListInput)) SavedWordSettings.Remove(WordListInput);
                SavedWordSettings.Add(WordListInput, WordListSetting);
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
                if (SavedWordSettings.ContainsKey(WordListInput)) SavedWordSettings.Remove(WordListInput);
                Configuration.ShockBadWordSettings = SavedWordSettings;
                Configuration.Save();
                currentWordIndex = -1;
                WordListInput = new String("");
                WordListSetting = new int[3];
                selectedWord = new String("");
            }

            ImGui.Spacing();
            ImGui.Spacing();

            if (Configuration.isDisallowed) ImGui.EndDisabled();

            if (ImGui.BeginListBox("Active Words"))
            {
                int index = 0;
                foreach (var (word, settings) in SavedWordSettings)
                {
                    var modeInt = settings[0];
                    var mode = new String("");
                    bool is_Selected = (currentWordIndex == index);
                    switch (modeInt) { case 0: mode = "Shock"; break; case 1: mode = "Vibrate"; break; case 2: mode = "Beep"; break; };
                    if (ImGui.Selectable($" {word}   Mode: {mode}  Intensity: {settings[1]}  Duration: {settings[2]}", ref is_Selected))
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
    }

    private void DrawDefaultTriggerTab()
    {
        if(ImGui.BeginTabItem("Default Trigger Settings"))
        {
            ImGui.Text("Default triggers will always be prioritized over custom triggers, if passthrough is not enabled.");
            if (Configuration.isDisallowed) ImGui.BeginDisabled();
            DrawSocial();
            DrawCombat();
            if (Configuration.isDisallowed) ImGui.EndDisabled();
            ImGui.EndTabItem();
        }
    }

    private void DrawCustomTriggerTab()
    {

        if (ImGui.BeginTabItem("Custom Trigger Settings"))
        {
            DrawCustomChats();
            DrawCustomTable();
            
            ImGui.EndTabItem();
        }
    }

    private void DrawPermissionsTab()
    {
        if (ImGui.BeginTabItem("Permissions"))
        {

            if (Configuration.isDisallowed) ImGui.BeginDisabled();
            var IsWhitelistEnforced = Configuration.IsWhitelistEnforced;
            if (ImGui.Checkbox("Activate Whitelist mode.", ref IsWhitelistEnforced))
            {
                Configuration.IsWhitelistEnforced = IsWhitelistEnforced;
                Configuration.Save();
            }

            var PermissionList = Configuration.PermissionList;

            if (ImGui.InputTextWithHint("##PlayerAddPerm", "Enter a playername or click on a entry", ref PermissionListInput, 48))
            {
                if (currentPermissionIndex != -1) // Get rid of the old settings, otherwise we build connections between two items
                {
                    PermissionListSetting = -1;
                }
            }

            ImGui.ListBox("##PermissionLevel", ref PermissionListSetting, ["Blocked", "Whitelisted", "Privileged"], 3);


            if (ImGui.Button("Add Player", new Vector2(45, 25)))
            {
                if (PermissionList.ContainsKey(PermissionListInput.ToLower())) PermissionList.Remove(PermissionListInput);
                PermissionList.Add(PermissionListInput.ToLower(), PermissionListSetting);
                Configuration.PermissionList = PermissionList;
                Configuration.Save();
                currentPermissionIndex = -1;
                PermissionListInput = new String("");
                PermissionListSetting = -1;
                selectedPlayerName = new String("");
            }
            ImGui.SameLine();
            if (ImGui.Button("Remove Player", new Vector2(45, 25)))
            {
                if (PermissionList.ContainsKey(PermissionListInput)) PermissionList.Remove(PermissionListInput);
                Configuration.PermissionList = PermissionList;
                Configuration.Save();
                currentPermissionIndex = -1;
                PermissionListInput = new String("");
                PermissionListSetting = -1;
                selectedPlayerName = new String("");
            }


            if (Configuration.isDisallowed) ImGui.EndDisabled();
            if (ImGui.BeginListBox("##PlayerPermissions"))
            {
                int index = 0;
                foreach (var (name, permissionlevel) in PermissionList)
                {
                    bool is_Selected = (currentWordIndex == index);
                    var permissionleveltext = new String("");
                    switch (permissionlevel) { case 0: permissionleveltext = "Blocked"; break; case 1: permissionleveltext = "Whitelisted"; break; case 2: permissionleveltext = "Privileged"; break; };
                    if (ImGui.Selectable($" Player: {name}   Permission: {permissionleveltext}", ref is_Selected))
                    {
                        selectedPlayerName = name;
                        currentPermissionIndex = index;
                        PermissionListInput = name;
                        PermissionListSetting = permissionlevel;
                    }
                    index++;
                }
                ImGui.EndListBox();
            }

            // TODO add text wrap
            ImGui.TextWrapped(" - \"Privileged\" allows a player to enable/disable your Triggers through messages. [Currently Unused]");
            ImGui.TextWrapped(" - \"Whitelisted\" allows a player to activate your Triggers, when you have \"Whitelist Mode\" on.");
            ImGui.TextWrapped(" - \"Blocked\" disallows a player from interacting with this plugin in any way.");

            ImGui.EndTabItem();
        }
    }
    private void DrawCommandTab()
    {
        if (isAlternative && ImGui.BeginTabItem("Commands") || Configuration.CommandActionsEnabled && ImGui.BeginTabItem("Commands"))
        {

            ImGui.Text("Not finished yet.");
            /*
            if (Configuration.isDisallowed) ImGui.BeginDisabled();
            var IsWhitelistEnforced = Configuration.IsWhitelistEnforced;
            if (ImGui.Checkbox("Activate Commands", ref IsWhitelistEnforced))
            {
                Configuration.IsWhitelistEnforced = IsWhitelistEnforced;
                Configuration.Save();
            }

            var PermissionList = Configuration.PermissionList;

            if (ImGui.InputTextWithHint("##PlayerAddPerm", "Enter a playername or click on a entry", ref PermissionListInput, 48))
            {
                if (currentPermissionIndex != -1) // Get rid of the old settings, otherwise we build connections between two items
                {
                    PermissionListSetting = -1;
                }
            }

            ImGui.ListBox("##PermissionLevel", ref PermissionListSetting, ["Blocked", "Whitelisted", "Privileged"], 3);


            if (ImGui.Button("Add Player", new Vector2(45, 25)))
            {
                if (PermissionList.ContainsKey(PermissionListInput.ToLower())) PermissionList.Remove(PermissionListInput);
                PermissionList.Add(PermissionListInput.ToLower(), PermissionListSetting);
                Configuration.PermissionList = PermissionList;
                Configuration.Save();
                currentPermissionIndex = -1;
                PermissionListInput = new String("");
                PermissionListSetting = -1;
                selectedPlayerName = new String("");
            }
            ImGui.SameLine();
            if (ImGui.Button("Remove Player", new Vector2(45, 25)))
            {
                if (PermissionList.ContainsKey(PermissionListInput)) PermissionList.Remove(PermissionListInput);
                Configuration.PermissionList = PermissionList;
                Configuration.Save();
                currentPermissionIndex = -1;
                PermissionListInput = new String("");
                PermissionListSetting = -1;
                selectedPlayerName = new String("");
            }


            if (Configuration.isDisallowed) ImGui.EndDisabled();
            if (ImGui.BeginListBox("##PlayerPermissions"))
            {
                int index = 0;
                foreach (var (name, permissionlevel) in PermissionList)
                {
                    bool is_Selected = (currentWordIndex == index);
                    var permissionleveltext = new String("");
                    switch (permissionlevel) { case 0: permissionleveltext = "Blocked"; break; case 1: permissionleveltext = "Whitelisted"; break; case 2: permissionleveltext = "Privileged"; break; };
                    if (ImGui.Selectable($" Player: {name}   Permission: {permissionleveltext}", ref is_Selected))
                    {
                        selectedPlayerName = name;
                        currentPermissionIndex = index;
                        PermissionListInput = name;
                        PermissionListSetting = permissionlevel;
                    }
                    index++;
                }
                ImGui.EndListBox();
            }

            // TODO add text wrap
            ImGui.TextWrapped(" - \"Privileged\" allows a player to enable/disable your Triggers through messages. [Currently Unused]");
            ImGui.TextWrapped(" - \"Whitelisted\" allows a player to activate your Triggers, when you have \"Whitelist Mode\" on.");
            ImGui.TextWrapped(" - \"Blocked\" disallows a player from interacting with this plugin in any way.");
            */
            ImGui.EndTabItem();
        }
    }
    private void DrawDebugTab()
    {
        if (ImGui.BeginTabItem("Debug"))
        {
            ImGui.Text("These are Debug settings.\nPlease don't touch them.");

            ImGui.InputInt("XIVChattype", ref debugFtype);
            //ImGui.InputInt("senderId", ref debugFtype);
            ImGui.InputText("Sender", ref debugFsender, 64);
            ImGui.InputText("Message", ref debugFmessage, 128);
            //ImGui.InputInt("XIVChattype", ref debugFtype);

            if (ImGui.Button("Send Fake Message", new Vector2(200, 60)))
            {
                XivChatType t = (XivChatType)debugFtype;
                SeString s = debugFsender.ToString();
                SeString m = debugFmessage.ToString();
                bool b = false;
                Plugin.PluginLog.Info("Sending Fake Message:");
                Plugin.NetworkWatcher.HandleChatMessage(t, 0, ref s, ref m, ref b);
            }


            ImGui.InputText("Sharestring Export", ref debugCstring, 256);
            if (ImGui.Button("generate", new Vector2(200, 60)))
            {
                debugCstring = Configuration.EncodeConfiguration("Permissions");
            }

            ImGui.InputText("Sharestring Import", ref debugCstring, 256);
            if (ImGui.Button("import", new Vector2(200, 60)))
            {
                Configuration.DecodeConfiguration(debugCstring);
            }


            if (ImGui.Button("toggle master mode", new Vector2(200, 60)))
            {
                Plugin.Configuration.isDisallowed = !Plugin.Configuration.isDisallowed;
            }

            if (ImGui.Button("Test Update", new Vector2(200, 60)))
            {
                Plugin.WebClient.sendServerRequest();
            }

            if (ImGui.Button("Test Transfer", new Vector2(200, 60)))
            {
                //Plugin.WebClient.sendServerData();
            }

            if (ImGui.Button("Test Register", new Vector2(200, 60)))
            {
                //Plugin.WebClient.sendRequestServer("register", "", "");
            }

            if (ImGui.Button("Test Upload", new Vector2(200, 60)))
            {
                //Plugin.WebClient.sendRequestServer("upload", "", "");
            }

            ImGui.InputTextWithHint("##hashkey", "Master Key", ref debugKmessage, 256);
            if (ImGui.Button("Update Hash", new Vector2(200, 60)))
            {
                Plugin.WebClient.sendUpdateHash();
            }

            if (ImGui.Button("Toggle isMaster", new Vector2(200, 60)))
            {
                Configuration.IsMaster = !Configuration.IsMaster;
                Configuration.Save();
            }

            if (ImGui.Button("Set Self to Master", new Vector2(200, 60)))
            {
                Configuration.MasterNameFull = Plugin.ClientState.LocalPlayer.Name + "#" + Plugin.ClientState.LocalPlayer.HomeWorld.Id;
                Configuration.Save();
            }

            if (ImGui.Button("Set Self to Sub", new Vector2(200, 60)))
            {
                Configuration.MasterNameFull = Plugin.ClientState.LocalPlayer.Name + "#" + Plugin.ClientState.LocalPlayer.HomeWorld.Id;
                Configuration.Save();
            }




            ImGui.InputTextWithHint("##resetkey", "Reset Key", ref debugRmessage, 256);
            if (ImGui.Button("Reset Userdata", new Vector2(200, 60)))
            {
                Plugin.WebClient.sendResetUserdata(debugRmessage);
                Configuration.DebugEnabled = false;
                debugRmessage = "";
                this.Toggle();
            }




            ImGui.EndTabItem();
        }

    }
    #endregion

    private void DrawSocial()
    {
        if(!ImGui.CollapsingHeader("Social Triggers"))
        {
            return;
        }
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

        if (ShockOnBadWord)
        {
            ImGui.Text("You can find the Settings for this option in the tab \"Word List\"");
        }

    }
    private void DrawCombat()
    {
        if (!ImGui.CollapsingHeader("Combat Triggers"))
        {
            return;
        }
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

        var DeathMode = Configuration.DeathMode;
        if(ImGui.Checkbox("Death Mode", ref DeathMode))
        {
            Configuration.DeathMode = DeathMode;
            Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered()) { ImGui.SetTooltip("This delivers scaling shocks based on the amount of party members that are dead, up to the maximum with a wipe. Be warned."); }

        if (DeathMode)
        {
            var DeathModeSettings = Configuration.DeathModeSettings;
            if (ImGui.ListBox("Mode##dscale", ref DeathModeSettings[0], ["Shock", "Vibrate", "Beep"], 3))
            {
                Configuration.DeathModeSettings = DeathModeSettings;
                Configuration.Save();
            }
            ImGui.SliderInt("Max Intensity##dscaleInt", ref DeathModeSettings[1], 1, 100);
            ImGui.SliderInt("Max Duration##dscaleDur", ref DeathModeSettings[2], 1, 15);
            ImGui.Spacing();
            ImGui.Spacing();
        }
        
    }
    private void DrawCustomChats()
    {
        if (!ImGui.CollapsingHeader("Custom Trigger Channels"))
        {
            return;
        }
        var i = 0;
        foreach (var e in ChatType.GetOrderedChannels())
        {
            // See if it is already enabled by default
            var enabled = Configuration.Channels.Contains(e);
            // Create a new line after every 4 columns
            if (i != 0 && (i == 4 || i == 7 || i == 11 || i == 15 || i == 19 || i == 23))
            {
                ImGui.NewLine();
                //i = 0;
            }
            // Move to the next row if it is LS1 or CWLS1
            if (e is ChatType.ChatTypes.LS1 or ChatType.ChatTypes.CWL1)
                ImGui.Separator();

            if (ImGui.Checkbox($"{e}", ref enabled))
            {
                // See If the UIHelpers.Checkbox is clicked, If not, add to the list of enabled channels, otherwise, remove it.
                if (enabled) Configuration.Channels.Add(e);
                else Configuration.Channels.Remove(e);
                Configuration.Save();
            }

            ImGui.SameLine();
            i++;
        }
        ImGui.NewLine();
    }
    private void DrawCustomTable()
    {
        if (!ImGui.CollapsingHeader("Custom Triggers"))
        {
            return;
        }
        List<Trigger> triggers = Configuration.Triggers;
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), ImGui.GetFrameHeight() * Vector2.One))
        {
            Configuration.Triggers.Add(new());
            Configuration.Save();
        }
        ImGui.PopFont();

        int cnt = 7;
        if (ImGui.BeginTable("##Triggers", cnt, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
        {
            ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoResize, ImGuiHelpers.GlobalScale * 75);
            ImGui.TableSetupColumn("Regex", ImGuiTableColumnFlags.NoResize, ImGuiHelpers.GlobalScale * 170);
            ImGui.TableSetupColumn("Mode", ImGuiTableColumnFlags.NoResize, ImGuiHelpers.GlobalScale * 90);
            ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.None, ImGuiHelpers.GlobalScale * 40);
            ImGui.TableSetupColumn("Intensity", ImGuiTableColumnFlags.None, ImGuiHelpers.GlobalScale * 40);
            ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort);
            ImGui.TableHeadersRow();

            for (int i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];

                ImGui.PushID(trigger.GUID.ToString());

                ImGui.TableNextColumn();
                if (ImGui.Checkbox("##enabled", ref trigger.Enabled))
                {
                    Configuration.Save();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Enable the trigger to be used.");
                }

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.InputTextWithHint("##name", "", ref trigger.Name, 100))
                {
                    Configuration.Save();
                }

                ImGui.TableNextColumn();
                if (trigger.Regex == null)
                {
                    try
                    {
                        trigger.Regex = new Regex(trigger.RegexString);
                    }
                    catch (ArgumentException)
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(ImGuiColors.DPSRed, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                        ImGui.PopFont();
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Not a valid regex. Will not be parsed.");
                        }
                        ImGui.SameLine();
                    }   
                }
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.InputTextWithHint("##regex", "Regex", ref trigger.RegexString, 200))
                {
                    try
                    {
                        trigger.Regex = new Regex(trigger.RegexString);
                    }
                    catch (ArgumentException ex)
                    {
                        trigger.Regex = null;
                    }
                    Configuration.Save();
                }

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.Combo("##mode", ref trigger.Mode, ["Shock", "Vibrate", "Beep"], 3))
                {
                    Configuration.Save();
                }

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.SliderInt("##duration", ref trigger.Duration, 1, 10))
                {
                    trigger.Duration = checkDuration(trigger.Duration);
                    Configuration.Save();
                }

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.SliderInt("##intensity", ref trigger.Intensity, 1, 100))
                {
                    trigger.Intensity = checkIntensity(trigger.Intensity);
                    Configuration.Save();
                }

                ImGui.TableNextColumn();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString(), ImGui.GetFrameHeight() * Vector2.One))
                {
                    Configuration.Triggers.Remove(trigger);
                    Configuration.Save();
                }
                ImGui.PopFont();
            }
            ImGui.EndTable();
        }
    }

    private int checkDuration(int duration)
    {
        if (duration < 1)
        {
            return duration = 1;
        }
        else if (duration > 15)
        {
            return duration = 15;
        }
        return duration;
    }

    private int checkIntensity(int intensity)
    {
        if (intensity < 1)
        {
            return intensity = 1;
        }
        else if (intensity > 100)
        {
            return intensity = 100;
        }
        return intensity;
    }
}
