using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Timers;
using WoLightning.Types;
using static FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkHistory.Delegates;


namespace WoLightning.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin Plugin;

    private int presetIndex = 0;

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
    private TimerPlus timeOutRequest = new TimerPlus();

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


        timeOutRequest.Interval = 300000;
        timeOutRequest.Elapsed += resetRequest;
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

    public void Dispose()
    {
        if (this.IsOpen) this.Toggle();
        Configuration.Save();
    }

    private void resetRequest(object sender, ElapsedEventArgs e)
    {
        timeOutRequest.Stop();
        Plugin.Authentification.MasterNameFull = "";

    }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        //presetIndex = Configuration.Presets.IndexOf(Configuration.ActivePreset);

    }

    public override void Draw()
    {

        DrawHeader();

        if (Configuration.Version < 30) return; //safety check for old configs

        if (ImGui.BeginTabBar("Tab Bar##tabbarmain", ImGuiTabBarFlags.None))
        {
            DrawGeneralTab();
            DrawDefaultTriggerTab();
            if (Configuration.ActivePreset.SayBadWord.IsEnabled()) DrawWordlistTab();
            DrawCustomTriggerTab();
            //DrawPermissionsTab(); todo make work again
            // DrawCommandTab(); todo not implemented
            DrawDebugTab();

            ImGui.EndTabBar();
        }
    }

    private void DrawHeader()
    {
        if (Configuration.Version < new Configuration().Version)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Your Configuration is incompatible.");
            if (ImGui.Button("Reset & Update Config"))
            {
                Configuration = new Configuration();
                Configuration.Initialize(Plugin, isAlternative, Plugin.ConfigurationDirectoryPath, true);

                Configuration.Presets.Add(new Preset("Default"));
                Configuration.Save();
                Configuration.loadPreset(addInput);
                Configuration.deletePreset(Configuration.ActivePreset);
                Configuration.Save();

                Plugin.sendNotif("Your configuration has been reset!");

                if (!isAlternative) Plugin.Configuration = Configuration;
                else Parent.Configuration = Configuration;
            }
        }

        if (Plugin.Authentification.HasMaster)
        {
            ImGui.Text("Your Master is currently " + Plugin.Authentification.MasterNameFull);
        }

        if (Plugin.Authentification.isDisallowed)
        {
            ImGui.TextColored(redCol, $"They do not allow you to change your Settings.");
            ImGui.BeginDisabled();
        }

        DrawPresetHeader();
    }

    private void DrawPresetHeader()
    {

        ImGui.PushItemWidth(ImGui.GetWindowSize().X - 90);

        presetIndex = Configuration.PresetIndex;
        if (ImGui.Combo("", ref presetIndex, [.. Configuration.PresetNames], Configuration.Presets.Count, 6))
        {
            Configuration.loadPreset(Configuration.PresetNames[presetIndex]);
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("+"))
        {
            importInput = "";
            addInput = "";
            ImGui.OpenPopup("Add Preset##addPreMod");
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("X"))
        {
            ImGui.OpenPopup("Delete Preset##delPreMod");
        }

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

            /*
            ImGui.Text("Or enter a Sharestring to import.");
            ImGui.PushItemWidth(ImGui.GetWindowSize().X - 10);
            if (addInput.Length != 0) ImGui.BeginDisabled();
            ImGui.InputText("##importInput", ref importInput, 256, ImGuiInputTextFlags.CharsNoBlank);
            if (addInput.Length != 0) ImGui.EndDisabled();
            */

            ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
            if (ImGui.Button("Add##addPre", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
            {
                if (addInput.Length > 0)
                {
                    Preset tPreset = new Preset(addInput);
                    Configuration.Presets.Add(tPreset);
                    Configuration.Save();
                    Configuration.loadPreset(addInput);
                }
                if (importInput.Length > 0)
                {
                    //Configuration.importPreset(importInput);
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
            //if (ImGui.Button("Generate##shaGen", new Vector2(ImGui.GetWindowSize().X - 10, 25))) exportInput = Configuration.sharePreset(Configuration.ActivePreset);
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

                    Configuration.deletePreset(Configuration.ActivePreset);
                    ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel##canRem", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25))) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    #region Tabs
    private void DrawGeneralTab()
    {
        if (ImGui.BeginTabItem("General"))
        {
            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
            var IsPassthroughAllowed = Configuration.ActivePreset.IsPassthroughAllowed;

            if (ImGui.Checkbox("Allow passthrough of triggers?", ref IsPassthroughAllowed))
            {
                Configuration.ActivePreset.IsPassthroughAllowed = IsPassthroughAllowed;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Will make it possible for multiple Triggers to happen at once (ex. Damage and Death)"); }

            var GlobalTriggerCooldown = Configuration.ActivePreset.globalTriggerCooldown;
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 240);
            if (ImGui.SliderInt("Global Cooldown of Triggers (sec)", ref GlobalTriggerCooldown, 3, 300))
            {
                Configuration.ActivePreset.globalTriggerCooldown = GlobalTriggerCooldown;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("This sets a Cooldown on how often you can be shocked, in seconds.\nThere is a 0.75 second delay before the cooldown triggers,\nto ensure that passthrough still works."); }

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();

            /*if (!isAlternative && !Configuration.HasMaster)
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

                            Configuration.MasterNameFull = ((IPlayerCharacter)Plugin.ClientState.LocalPlayer.TargetObject).Name.ToString() + "#" + ((IPlayerCharacter)Plugin.ClientState.LocalPlayer.TargetObject).HomeWorld.Id;
                            Plugin.WebClient.sendServerData(new NetworkPacket(["packet", "refplayer", "requestmaster"], ["attempt", Configuration.MasterNameFull, "undefined"]));
                            timeOutRequest.Start();
                        }
                    }
                }
                if (Configuration.MasterNameFull != "" && !Configuration.HasMaster)
                {
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), $"Waiting for response from {Configuration.MasterNameFull}...\n{(int)TimeSpan.FromMilliseconds(timeOutRequest.TimeLeft).TotalSeconds} seconds until timeout...");
                }
            }
            */
            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();
            ImGui.EndTabItem();
        }
    }
    private void DrawWordlistTab()
    {
        if (ImGui.BeginTabItem("Word List"))
        {
            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
            var SavedWordSettings = Configuration.ActivePreset.SayBadWord.CustomData;

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
                Configuration.ActivePreset.SayBadWord.CustomData = SavedWordSettings;
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
                Configuration.ActivePreset.SayBadWord.CustomData = SavedWordSettings;
                Configuration.Save();
                currentWordIndex = -1;
                WordListInput = new String("");
                WordListSetting = new int[3];
                selectedWord = new String("");
            }

            ImGui.Spacing();
            ImGui.Spacing();

            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();

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
        if (ImGui.BeginTabItem("Default Triggers"))
        {
            ImGui.Text("Default triggers will always be prioritized over custom triggers, if passthrough is not enabled.");
            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
            DrawSocial();
            DrawCombat();
            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();
            ImGui.EndTabItem();
        }
    }

    private void DrawCustomTriggerTab()
    {

        if (ImGui.BeginTabItem("Custom Triggers"))
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

            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();

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


            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();
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


            ImGui.TextWrapped(" - \"Privileged\" allows a player to enable/disable your Triggers through messages. [Currently Unused]");
            ImGui.TextWrapped(" - \"Whitelisted\" allows a player to activate your Triggers, when you have \"Whitelist Mode\" on.");
            ImGui.TextWrapped(" - \"Blocked\" disallows a player from interacting with this plugin in any way.");

            ImGui.EndTabItem();
        }
    }
    /*private void DrawCommandTab()
    {
        if (isAlternative && ImGui.BeginTabItem("Commands") || Configuration.CommandActionsEnabled && ImGui.BeginTabItem("Commands"))
        {

            ImGui.Text("Not finished yet.");
            /*
            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
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


            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();
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
    }*/
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




            if (ImGui.Button("toggle master mode", new Vector2(200, 60)))
            {
                Plugin.Authentification.isDisallowed = !Plugin.Authentification.isDisallowed;
            }

            if (ImGui.Button("Test Login", new Vector2(200, 60)))
            {
                Plugin.WebClient.establishWebseverConnection();
            }

            if (ImGui.Button("Test Upload", new Vector2(200, 60)))
            {
                //Plugin.WebClient.sendRequestServer("upload", "", "");
            }


            /*if (ImGui.Button("Toggle isMaster", new Vector2(200, 60)))
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
            }*/







            ImGui.EndTabItem();
        }

    }
    #endregion

    private void DrawSocial()
    {
        if (!ImGui.CollapsingHeader("Social Triggers"))
        {
            return;
        }

        createEntry(Configuration.ActivePreset.GetPat, "Triggers whenever you get /pet.");
        createEntry(Configuration.ActivePreset.LoseDeathRoll, "Trigger whenever you lose a Deathroll.",
            "Deathroll is when you use /random against another player to see who reaches 1 first.");


        createEntry(Configuration.ActivePreset.SayFirstPerson, "Triggers whenever you refer to yourself in the First Person.",
            "First-Person refers to basically any way you can say 'me'. So saying 'I','I'll','Me','Myself' and so on.\nThis currently only works when writing in English.");


        ImGui.BeginDisabled();
        createEntry(Configuration.ActivePreset.SayBadWord, "Triggers whenever you say a word from a list.",
            "You can configure these words, once the setting is enabled.");
        if (Configuration.ActivePreset.SayBadWord.IsEnabled())
            ImGui.Text("You can find the Settings for this option in the tab \"Word List\"");
        ImGui.EndDisabled();

    }
    private void DrawCombat()
    {
        if (!ImGui.CollapsingHeader("Combat Triggers"))
        {
            return;
        }

        createEntry(Configuration.ActivePreset.Wipe, "Triggers whenever all Partymembers die.");

        createEntry(Configuration.ActivePreset.Die, "Triggers whenever you die.");

        createEntry(Configuration.ActivePreset.PartymemberDies, "Triggers whenever any partymember dies.",
            "This delivers scaling shocks based on the amount of party members that are dead, up to your selected Maximum.");

        createEntry(Configuration.ActivePreset.FailMechanic, "Triggers whenever you fail a Mechanic.",
            "This will trigger whenever you get a [Vulnerability Up] or [Damage Down] debuff.");

        createEntry(Configuration.ActivePreset.TakeDamage, "Triggers whenever you take damage of any kind.",
            "This will go off alot, so be warned! It does mean literally any damage, from Mobs to Dots and even Fall Damage!\nIf it ever gets too much, remember to set the Cooldown higher in General Settings!");

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
            var enabled = Configuration.ActivePreset.Channels.Contains(e);
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
                if (enabled) Configuration.ActivePreset.Channels.Add(e);
                else Configuration.ActivePreset.Channels.Remove(e);
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
        List<RegexTrigger> triggers = Configuration.ActivePreset.CustomMessageTriggers;
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), ImGui.GetFrameHeight() * Vector2.One))
        {
            Configuration.ActivePreset.CustomMessageTriggers.Add(new());
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
                    Configuration.ActivePreset.CustomMessageTriggers.Remove(trigger);
                    Configuration.Save();
                }
                ImGui.PopFont();

                ImGui.PopID();
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
        else if (duration > 10)
        {
            return duration = 10;
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


    private void createEntry(Trigger TriggerObject, string Description)
    {
        createShockerSelector(TriggerObject);
        bool enabled = TriggerObject.IsEnabled();
        if (ImGui.Checkbox($"##checkBox{TriggerObject.Name}", ref enabled))
        {
            if (Plugin.Authentification.PishockShockers.Count > 1) ImGui.OpenPopup($"Select Shockers##selectShockers{TriggerObject.Name}");
            else if (Plugin.Authentification.PishockShockers.Count == 1)
            { 
                if (enabled) TriggerObject.Shockers = Plugin.Authentification.PishockShockers;
                else TriggerObject.Shockers = new();
            }
            else if (Plugin.Authentification.PishockShockers.Count == 0)
            {
                // todo show message to add shocker first
            }
        }
        ImGui.SameLine();
        ImGui.Text($"{Description}");
        if (enabled) createPickerBox(TriggerObject);
    }

    private void createEntry(Trigger TriggerObject, string Description, string Hint)
    {
        createShockerSelector(TriggerObject);
        bool enabled = TriggerObject.IsEnabled();
        if (ImGui.Checkbox($"##checkBox{TriggerObject.Name}", ref enabled))
        {
            if (Plugin.Authentification.PishockShockers.Count > 1) ImGui.OpenPopup($"Select Shockers##selectShockers{TriggerObject.Name}");
            else if (Plugin.Authentification.PishockShockers.Count == 1)
            {
                if (enabled) TriggerObject.Shockers = Plugin.Authentification.PishockShockers;
                else TriggerObject.Shockers = new();
            }
            else if (Plugin.Authentification.PishockShockers.Count == 0)
            {
                // todo show message to add shocker first
            }
        }
        ImGui.SameLine();
        ImGui.Text($"{Description}");
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered()) { ImGui.SetTooltip(Hint); }
        if (enabled) createPickerBox(TriggerObject);
    }

    private void createPickerBox(Trigger TriggerObject)
    {
        bool changed = false;

        ImGui.BeginDisabled();
        ImGui.Button($"{TriggerObject.Shockers.Count}##shockerButton{TriggerObject.Name}", new Vector2(35, 50));
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered( ImGuiHoveredFlags.AllowWhenDisabled)) { ImGui.SetTooltip($"Enabled Shockers:\n{TriggerObject.getShockerNamesNewLine()}"); }
        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Text("    Mode");
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 3 - 50);
        int OpMode = (int)TriggerObject.OpMode;
        if (ImGui.Combo("##" + TriggerObject.Name, ref OpMode, ["Shock", "Vibrate", "Beep"], 3))
        {
            TriggerObject.OpMode = (OpType)OpMode;
            changed = true;
        }
        ImGui.EndGroup();

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Text("    Intensity");
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 3);
        int Intensity = TriggerObject.Intensity;
        if (ImGui.SliderInt("##Intensity" + TriggerObject.Name, ref Intensity, 1, 100))
        {
            TriggerObject.Intensity = Intensity;
            changed = true;
        }
        ImGui.EndGroup();

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Text("    Duration");
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 3);
        int Duration = TriggerObject.Duration;
        if (ImGui.SliderInt("##Duration" + TriggerObject.Name, ref Duration, 1, 10))
        {
            TriggerObject.Duration = Duration;
            changed = true;
        }
        ImGui.EndGroup();

        if (TriggerObject.Name == "TakeDamage") createProportional(TriggerObject, "Amount of Health% to lose to hit the Limit.", 1, 100);
        if (TriggerObject.Name == "FailMechanic") createProportional(TriggerObject, "Amount of Stacks needed to hit the Limit.", 1, 8);

        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Spacing();
        if (changed) Configuration.Save();
    }

    private void createShockerSelector(Trigger TriggerObject)
    {

        Vector2 center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(400, 250));
        bool isModalOpen = TriggerObject.isModalOpen;
        if (ImGui.BeginPopupModal($"Select Shockers##selectShockers{TriggerObject.Name}", ref isModalOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.TextWrapped("Please select all Shockers that should activate for this setting:");

            foreach (var shocker in Plugin.Authentification.PishockShockers)
            {
                bool isEnabled = TriggerObject.Shockers.Contains(shocker);
                if (ImGui.Checkbox($"{shocker.Name}##shockerbox{shocker.Code}", ref isEnabled))
                { // this could probably be solved more elegantly
                    if (isEnabled) TriggerObject.Shockers.Add(shocker);
                    else TriggerObject.Shockers.Remove(shocker);
                }
            }

            if (ImGui.Button($"Apply##apply{TriggerObject.Name}", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }


    }

    private void createProportional(Trigger TriggerObject, string Description, int minValue, int maxValue)
    {
        TriggerObject.setupCustomData();
        bool isEnabled = TriggerObject.CustomData["Proportional"][0] == 1;
        if (ImGui.Checkbox($"Enable proportional calculations.##proportionalIsEnabled{TriggerObject.Name}", ref isEnabled)) TriggerObject.CustomData["Proportional"][0] = isEnabled ? 1 : 0;
        if (isEnabled)
        {
            int setValue = TriggerObject.CustomData["Proportional"][1];
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 2 - 25);
            if (ImGui.SliderInt($"{Description}##proportionalSlider{TriggerObject.Name}", ref setValue, minValue, maxValue)) TriggerObject.CustomData["Proportional"][1] = setValue;
        }
    }

}
