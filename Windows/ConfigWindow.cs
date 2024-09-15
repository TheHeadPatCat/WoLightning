using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Timers;
using WoLightning.Classes;
using WoLightning.Types;




namespace WoLightning.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin Plugin;

    private int presetIndex = 0;

    List<int> durationArray = [100, 300, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    private Vector4 descColor = new Vector4(0.7f, 0.7f, 0.7f, 0.8f);
    private Vector4 nameColorOff = new Vector4(1, 1, 1, 0.9f);
    private Vector4 nameColorOn = new Vector4(0.5f,1, 0.3f, 0.9f);

    // Badword List
    private String BadWordListInput = new String("");
    private int[] BadWordListSetting = new int[3];
    private String BadselectedWord = new String("");
    private int BadcurrentWordIndex = -1;

    // Enforced Word List
    private String DontSayWordListInput = new String("");
    private int[] DontSayWordListSetting = new int[3];
    private String DontSayselectedWord = new String("");
    private int DontSaycurrentWordIndex = -1;


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

    private int debugOpIndex = 0;
    private string debugOpData = "";
    private Player debugPlayerTarget = null;
    private string[] debugOpCodes = Operation.allOpCodesString(true);





    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base($"Warrior of Lightning Configuration - v{plugin.Configuration.Version}##configmain")
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

    public ConfigWindow(Plugin plugin, Configuration configuration, MasterWindow parent) : base($"Master of Lightning Configuration - v{plugin.Configuration.Version}##configmaster")
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
        //Plugin.Authentification.MasterNameFull = "";

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
            if (Configuration.ActivePreset.SayBadWord.IsEnabled()) DrawBadWordList();
            if (Configuration.ActivePreset.DontSayWord.IsEnabled()) DrawEnforcedWordList();
            DrawCustomTriggerTab();
            //DrawPermissionsTab(); todo make work again
            //DrawCommandTab(); todo not implemented
            if (Configuration.DebugEnabled) DrawDebugTab();

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
            ImGui.Text("You are bound to " + Plugin.Authentification.Master.Name);
        }

        if (Plugin.Authentification.isDisallowed)
        {
            ImGui.TextColored(redCol, $"They do not allow you to change your settings.");
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
            ImGui.TextWrapped("Use this string to share your current preset!");
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
            ImGui.TextWrapped("Are you sure you want to delete this preset?");
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
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("This will make it possible for multiple triggers to happen at once (ex. damage and death)"); }

            var GlobalTriggerCooldown = Configuration.ActivePreset.globalTriggerCooldown;
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 240);
            if (ImGui.SliderInt("Global cooldown of triggers (sec)", ref GlobalTriggerCooldown, 3, 300))
            {
                Configuration.ActivePreset.globalTriggerCooldown = GlobalTriggerCooldown;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("This sets a cooldown on how often you can be shocked, in seconds.\nThere is a 0.75 second delay before the cooldown triggers,\nto ensure that passthrough still works."); }

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();

            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();
            ImGui.EndTabItem();
        }
    }
    private void DrawBadWordList()
    {
        if (ImGui.BeginTabItem("Bad Word List"))
        {
            ImGui.Text("If you say any of these words, you'll trigger its settings!" +
                "\nPunctuation doesnt matter!");
            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
            var SavedWordSettings = Configuration.ActivePreset.SayBadWord.CustomData;

            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 15);
            if (ImGui.InputTextWithHint("##BadWordInput", "Click on a entry to edit it.", ref BadWordListInput, 48))
            {
                if (BadcurrentWordIndex != -1) // Get rid of the old settings, otherwise we build connections between two items
                {
                    int[] copyArray = new int[3];
                    BadWordListSetting.CopyTo(copyArray, 0);
                    BadWordListSetting = copyArray;
                }
                BadcurrentWordIndex = -1;
            }

            //clamp
            if (BadWordListSetting[0] < 0 || BadWordListSetting[0] > 3) BadWordListSetting[0] = 0;

            if (BadWordListSetting[1] <= 0) BadWordListSetting[1] = 1;
            if (BadWordListSetting[1] > 100) BadWordListSetting[1] = 100;

            if (BadWordListSetting[2] <= 0) BadWordListSetting[2] = 100;
            if (BadWordListSetting[2] > 10 && BadWordListSetting[2] != 100 && BadWordListSetting[2] != 300) BadWordListSetting[2] = 10;

            ImGui.Separator();
            
            ImGui.BeginGroup();
            ImGui.Spacing();
            ImGui.Text("    Mode");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 3 - 50);
            ImGui.Combo("##Word", ref BadWordListSetting[0], ["Shock", "Vibrate", "Beep"], 3);
            ImGui.EndGroup();

            ImGui.SameLine();

            ImGui.BeginGroup();
            ImGui.Spacing();
            ImGui.Text("    Duration");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 4.5f);
            int DurationIndex = durationArray.IndexOf(BadWordListSetting[2]);
            if(ImGui.Combo("##WordDur", ref DurationIndex, ["0.1s", "0.3s", "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "10s"], 12)){
                BadWordListSetting[2] = durationArray[DurationIndex];
            }
            ImGui.EndGroup();

            ImGui.SameLine();

            ImGui.BeginGroup();
            ImGui.Spacing();
            ImGui.Text("    Intensity");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 2f);
            ImGui.SliderInt("##BadWordInt", ref BadWordListSetting[1], 1, 100);
            ImGui.EndGroup();

            ImGui.Separator();

            ImGui.Spacing();

            if (ImGui.Button("Add Word##BadWordAdd",new Vector2(ImGui.GetWindowWidth() /2 - 8,25)))
            {
                if (SavedWordSettings.ContainsKey(BadWordListInput)) SavedWordSettings.Remove(BadWordListInput);
                SavedWordSettings.Add(BadWordListInput, BadWordListSetting);
                Configuration.ActivePreset.SayBadWord.CustomData = SavedWordSettings;
                Configuration.Save();
                BadcurrentWordIndex = -1;
                BadWordListInput = new String("");
                BadWordListSetting = new int[3];
                BadselectedWord = new String("");
            }

            ImGui.SameLine();

            if (BadcurrentWordIndex == -1) ImGui.BeginDisabled();
            if (ImGui.Button("Remove Word##BadWordRemove", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {
                if (SavedWordSettings.ContainsKey(BadWordListInput)) SavedWordSettings.Remove(BadWordListInput);
                Configuration.ActivePreset.SayBadWord.CustomData = SavedWordSettings;
                Configuration.Save();
                BadcurrentWordIndex = -1;
                BadWordListInput = new String("");
                BadWordListSetting = new int[3];
                BadselectedWord = new String("");
            }
            if (BadcurrentWordIndex == -1) ImGui.EndDisabled();

            ImGui.Spacing();

            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();

            if (ImGui.BeginListBox("##BadWordListBox",new Vector2(ImGui.GetWindowWidth() - 15,340)))
            {
                int index = 0;
                foreach (var (word, settings) in SavedWordSettings)
                {
                    string mode = new String("");
                    string durS = new String("");
                    bool is_Selected = (BadcurrentWordIndex == index);
                    switch (settings[0]) { case 0: mode = "Shock"; break; case 1: mode = "Vibrate"; break; case 2: mode = "Beep"; break; };
                    switch (settings[2]) { case 100: durS = "0.1s"; break; case 300: durS = "0.3s"; break; default: durS = $"{settings[2]}s"; break; }
                    if (ImGui.Selectable($" Word: {word}  | Mode: {mode} | Intensity: {settings[1]} | Duration: {durS}", ref is_Selected))
                    {
                        BadselectedWord = word;
                        BadcurrentWordIndex = index;
                        BadWordListInput = word;
                        BadWordListSetting = settings;
                    }
                    index++;
                }
                ImGui.EndListBox();
            }


            ImGui.EndTabItem();
        }
    }
    private void DrawEnforcedWordList()
    {
        if (ImGui.BeginTabItem("Enforced Word List"))
        {
            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();

            ImGui.Text("You have to say atleast one of the words from the list below, otherwise these settings will trigger." +
                "\nCorrect punctuation is needed as well.");
            createPickerBox(Configuration.ActivePreset.DontSayWord);

            var SavedWordSettings = Configuration.ActivePreset.DontSayWord.CustomData;

            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 15);
            if (ImGui.InputTextWithHint("##DontSayWordInput", "Click on a entry to edit it.", ref DontSayWordListInput, 48))
            {
                if (BadcurrentWordIndex != -1) // Get rid of the old settings, otherwise we build connections between two items
                {
                    int[] copyArray = new int[3];
                    BadWordListSetting.CopyTo(copyArray, 0);
                    BadWordListSetting = copyArray;
                }
                BadcurrentWordIndex = -1;
            }



            ImGui.Spacing();

            if (ImGui.Button("Add Word##DontSayWordAdd", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {
                if (SavedWordSettings.ContainsKey(DontSayWordListInput)) SavedWordSettings.Remove(DontSayWordListInput);
                SavedWordSettings.Add(DontSayWordListInput, DontSayWordListSetting);
                Configuration.ActivePreset.DontSayWord.CustomData = SavedWordSettings;
                Configuration.Save();
                DontSaycurrentWordIndex = -1;
                DontSayWordListInput = new String("");
                DontSayWordListSetting = new int[3];
                DontSayselectedWord = new String("");
            }

            ImGui.SameLine();

            if (DontSaycurrentWordIndex == -1) ImGui.BeginDisabled();
            if (ImGui.Button("Remove Word##DontSayWordRemove", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {
                if (SavedWordSettings.ContainsKey(DontSayWordListInput)) SavedWordSettings.Remove(DontSayWordListInput);
                Configuration.ActivePreset.DontSayWord.CustomData = SavedWordSettings;
                Configuration.Save();
                DontSaycurrentWordIndex = -1;
                DontSayWordListInput = new String("");
                DontSayWordListSetting = new int[3];
                DontSayselectedWord = new String("");
            }
            if (DontSaycurrentWordIndex == -1) ImGui.EndDisabled();

            ImGui.Spacing();

            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();

            if (ImGui.BeginListBox("##DontSayWordListBox", new Vector2(ImGui.GetWindowWidth() - 15, 320)))
            {
                int index = 0;
                foreach (var (word, settings) in SavedWordSettings)
                {
                    var modeInt = settings[0];
                    var mode = new String("");
                    bool is_Selected = (DontSaycurrentWordIndex == index);
                    if (ImGui.Selectable($" {word} ", ref is_Selected))
                    {
                        DontSayselectedWord = word;
                        DontSaycurrentWordIndex = index;
                        DontSayWordListInput = word;
                        DontSayWordListSetting = settings;
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
            ImGui.TextWrapped("These Triggers are premade to react to certain ingame events!\nThey will always be prioritized over custom triggers, if passthrough is not enabled.");
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
            if (ImGui.InputTextWithHint("##PlayerAddPerm", "Enter a player name or click on a entry", ref PermissionListInput, 48))
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
                    bool is_Selected = (BadcurrentWordIndex == index);
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


            ImGui.TextWrapped(" - \"Privileged\" allows a player to enable/disable your triggers through messages. [Currently Unused]");
            ImGui.TextWrapped(" - \"Whitelisted\" allows a player to activate your triggers, when you have \"Whitelist Mode\" on.");
            ImGui.TextWrapped(" - \"Blocked\" disallows a player from interacting with this plugin in any way.");

            ImGui.EndTabItem();
        }
    }

    //[Conditional("DEBUG")] //only draw debug tab if built locally with debug, yell at me if you dont want this
    // No Longer needed, since i reimplemented the Configuration.DebugEnabled check - which for some reason wasnt used?
    private void DrawDebugTab()
    {
        if (ImGui.BeginTabItem("Debug"))
        {

            ImGui.SetWindowFontScale(2f);
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "\nThese are debug settings.\nPlease don't touch them.\n\n");
            ImGui.SetWindowFontScale(1.33f);
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Pressing them might softlock the plugin, break your config\nBan you from the Webserver permanently, or worst of all...\nUnpat your dog/cat.");
            ImGui.SetWindowFontScale(0.77f);
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.8f), "(or fish)");

            ImGui.SetWindowFontScale(1f);
            ImGui.Spacing();

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
                Plugin.Log("Sending fake message:");
                Plugin.NetworkWatcher.HandleChatMessage(t, 0, ref s, ref m, ref b);
            }

            ImGui.ListBox("Operation", ref debugOpIndex, debugOpCodes, debugOpCodes.Length, 4);
            ImGui.InputText("OpData", ref debugOpData, 512);

            IGameObject st = Plugin.TargetManager.Target;
            if (st != null && st.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
            {
                IPlayerCharacter st1 = (IPlayerCharacter)st;
                if (debugPlayerTarget == null || debugPlayerTarget.Name != st1.Name.ToString())
                {
                    debugPlayerTarget = new Player(st1.Name.ToString(), (int)st1.HomeWorld.Id);
                }
            }

            string playerName = "None";
            if (debugPlayerTarget != null) playerName = debugPlayerTarget.Name;
            ImGui.InputText("##debugTargetPlayer", ref playerName, 512, ImGuiInputTextFlags.ReadOnly);

            ImGui.SameLine();
            if (ImGui.Button("X##debugRemovePlayer")) debugPlayerTarget = null;


            if (ImGui.Button("Test Operation", new Vector2(200, 60)))
            {
                Plugin.ClientWebserver.request(
                    Operation.getOperationCode(
                        debugOpCodes[debugOpIndex].Split(" - ")[1]),
                        debugOpData,
                        debugPlayerTarget);
            }

            if (ImGui.Button("Test OnRequest()"))
            {
                Plugin.Authentification.gotRequest = true;
                Plugin.ShowMasterUI();
            }

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

        createEntry(Configuration.ActivePreset.GetPat,"Get /pet'd", "Triggers whenever a player does the /pet emote on you.");
        createEntry(Configuration.ActivePreset.GetSnapped,"Get /snap'd" , "Triggers whenever a player does the /snap emote on you.");
        createEntry(Configuration.ActivePreset.SitOnFurniture,"Sit on Chairs" , "Triggers whenever you /sit onto any kind of furniture.",
            "This Trigger will activate again after 5 seconds (after the shock is done) if you dont get off!" +
            "\nIf you do /groundsit onto it, it wont count though.");
        createEntry(Configuration.ActivePreset.LoseDeathRoll,"Lose DR", "Triggers whenever you lose a deathroll.",
            "Deathroll is when you use /random against another player to see who reaches 1 first.");


        createEntry(Configuration.ActivePreset.SayFirstPerson,"Mention yourself", "Triggers whenever you refer to yourself in the first person.",
            "This currently only works when writing in English." +
            "\nExamples: 'Me', 'I', 'Mine' and so on.");


        createEntry(Configuration.ActivePreset.SayBadWord,"Say a bad word", "Triggers whenever you say a bad word from a list.",
            "You can set these words yourself in the new Tab 'Bad Word List' once this is activated."
            ,true);

        createEntry(Configuration.ActivePreset.DontSayWord,"Dont say a enforced word", "Triggers whenever you forget to say a enforced word from a list.",
            "You can set these words yourself in the new Tab 'Enforced Word List' once this is activated."
            , true);

    }
    private void DrawCombat()
    {
        if (!ImGui.CollapsingHeader("Combat Triggers"))
        {
            return;
        }

        createEntry(Configuration.ActivePreset.Wipe,"Party Wipe", "Triggers whenever all party members die.");

        createEntry(Configuration.ActivePreset.Die,"Death", "Triggers whenever you die.");

        createEntry(Configuration.ActivePreset.PartymemberDies,"Party member death" , "Triggers whenever any party member dies, this includes you.",
            "This delivers proportional shocks, based on how many players are dead - up to your set limit.");

        createEntry(Configuration.ActivePreset.FailMechanic,"Fail a Mechanic", "Triggers whenever you fail a mechanic.",
            "This will trigger whenever you get a [Vulnerability Up] or [Damage Down] debuff.");

        createEntry(Configuration.ActivePreset.TakeDamage,"Take Damage", "Triggers whenever you take damage of any kind.",
            "This will go off a lot, so be warned!" +
            "\nIt does mean literally any damage, from mobs to DoTs and even fall damage!");

    }
    private void DrawCustomChats()
    {

        ImGui.TextWrapped("These options let you set letters, words or phrases that will trigger specified settings!\nIt's important to note that ANYONE can cause these to activate!\nExcept if you set it up to only react to your playername, of course.");

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

        List<RegexTrigger> Triggers = Configuration.ActivePreset.SayCustomMessage;
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), ImGui.GetFrameHeight() * Vector2.One))
        {
            Configuration.ActivePreset.SayCustomMessage.Add(new("New Trigger",false));
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

            for (int i = 0; i < Triggers.Count; i++)
            {
                var trigger = Triggers[i];

                ImGui.PushID(trigger.GUID.ToString());

                createShockerSelector(trigger);
                bool isEnabled = trigger.IsEnabled();
                ImGui.TableNextColumn();
                if (ImGui.Checkbox("##enabled", ref isEnabled))
                {
                    ImGui.OpenPopup($"Select Shockers##selectShockers{trigger.Name}");
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Select on which shockers this trigger is enabled.");
                }

                string name = trigger.Name;
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.InputTextWithHint("##name", "", ref name, 100))
                {
                    trigger.Name = name;
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
                            ImGui.SetTooltip("Not a valid regular expression. Will not be parsed.");
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
                    catch
                    {
                        trigger.Regex = null;
                    }
                    Configuration.Save();
                }

                int opMode = (int)trigger.OpMode;
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.Combo("##mode", ref opMode, ["Shock", "Vibrate", "Beep"], 3))
                {
                    trigger.OpMode = (OpMode)opMode;
                    Configuration.Save();
                }

                int duration = trigger.Duration;
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.SliderInt("##duration", ref duration, 1, 10))
                {
                    trigger.Duration = duration;
                    Configuration.Save();
                }

                int intensity = trigger.Intensity;
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.SliderInt("##intensity", ref intensity, 1, 100))
                {
                    trigger.Intensity = intensity;
                    Configuration.Save();
                }

                ImGui.TableNextColumn();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString(), ImGui.GetFrameHeight() * Vector2.One))
                {
                    Configuration.ActivePreset.SayCustomMessage.Remove(trigger);
                    Configuration.Save();
                }
                ImGui.PopFont();

                ImGui.PopID();
            }
            ImGui.EndTable();
        }
    }







    private void createEntry(Trigger TriggerObject, string Name, string Description) { createEntry(TriggerObject, Name, Description, "", false); }
    private void createEntry(Trigger TriggerObject, string Name, string Description, bool noOptions) { createEntry(TriggerObject, Name,  Description, "", noOptions); }

    private void createEntry(Trigger TriggerObject, string Name, string Description, string Hint) { createEntry(TriggerObject, Name, Description, Hint, false); }

    private void createEntry(Trigger TriggerObject, string Name, string Description, string Hint, bool noOptions)
    {
        createShockerSelector(TriggerObject);
        bool enabled = TriggerObject.IsEnabled();
        ImGui.BeginGroup();
        if (noOptions || !enabled)
        {
            ImGui.Spacing();
            ImGui.Spacing();
        }
        if (ImGui.Checkbox($"##checkBox{TriggerObject.Name}", ref enabled))
            ImGui.OpenPopup($"Select Shockers##selectShockers{TriggerObject.Name}");
        bool isOptionsOpened = TriggerObject.isOptionsOpen;
        if (!noOptions && enabled)
        {
            if (isOptionsOpened && ImGui.ArrowButton("##collapse" + TriggerObject.Name, ImGuiDir.Down))
            {
                TriggerObject.isOptionsOpen = !isOptionsOpened;
            }
            if (!isOptionsOpened && ImGui.ArrowButton("##collapse" + TriggerObject.Name, ImGuiDir.Right))
            {
                TriggerObject.isOptionsOpen = !isOptionsOpened;
            }
        }
        ImGui.EndGroup();

        ImGui.SameLine();
        ImGui.BeginGroup();
        if (!noOptions && enabled) ImGui.Spacing();
        if (enabled) ImGui.TextColored(nameColorOn,"  " + Name + $"  [{TriggerObject.OpMode}]");
        else ImGui.TextColored(nameColorOff, "  " + Name);
        ImGui.TextColored(descColor,$"  {Description}");
        if (Hint.Length > 0)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip(Hint); }
        }
        ImGui.EndGroup();
        
        if (isOptionsOpened)
        {
            createPickerBox(TriggerObject);
        }
        ImGui.Spacing();
        ImGui.Separator();
    }

    private void createPickerBox(Trigger TriggerObject)
    {
        bool changed = false;

        ImGui.BeginDisabled();
        ImGui.Button($"{TriggerObject.Shockers.Count}##shockerButton{TriggerObject.Name}", new Vector2(35, 50));
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) { ImGui.SetTooltip($"Enabled Shockers:\n{TriggerObject.getShockerNamesNewLine()}"); }
        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Text("    Mode");
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 3 - 50);
        int OpMode = (int)TriggerObject.OpMode;
        if (ImGui.Combo("##" + TriggerObject.Name, ref OpMode, ["Shock", "Vibrate", "Beep"], 3))
        {
            TriggerObject.OpMode = (OpMode)OpMode;
            changed = true;
        }
        ImGui.EndGroup();

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Text("    Duration");
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 7);
        int DurationIndex = durationArray.IndexOf(TriggerObject.Duration);
        if (ImGui.Combo("##Duration" + TriggerObject.Name, ref DurationIndex, ["0.1s", "0.3s", "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "10s"], 12))
        {
            TriggerObject.Duration = durationArray[DurationIndex];
            changed = true;
        }
        ImGui.EndGroup();

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Text("    Intensity");
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 1.85f - 30);
        int Intensity = TriggerObject.Intensity;
        if (ImGui.SliderInt("##Intensity" + TriggerObject.Name, ref Intensity, 1, 100))
        {
            TriggerObject.Intensity = Intensity;
            changed = true;
        }
        ImGui.EndGroup();

        if (TriggerObject.Name == "TakeDamage") createProportional(TriggerObject, "Amount of Health% to lose to hit the Limit.", 1, 100);
        if (TriggerObject.Name == "FailMechanic") createProportional(TriggerObject, "Amount of Stacks needed to hit the Limit.", 1, 8);

        int Cooldown = TriggerObject.Cooldown;
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 1.25f - 30);
        if (ImGui.SliderInt("Cooldown (sec) ##Cooldown" + TriggerObject.Name, ref Cooldown, 0, 60))
        {
            TriggerObject.Cooldown = Cooldown;
            changed = true;
        }

        if (changed) Configuration.Save();
    }

    private void createShockerSelector(Trigger TriggerObject)
    {
        // Todo add proper formatting, this popup looks terrible
        Vector2 center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(400, 400));
        bool isModalOpen = TriggerObject.isModalOpen;
        if (ImGui.BeginPopupModal($"Select Shockers##selectShockers{TriggerObject.Name}", ref isModalOpen,
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoDecoration))
        {
            
            if (Plugin.Authentification.PishockShockers.Count == 0)
            {
                ImGui.TextWrapped("Please add Shockers first via the \"Account Settings\" in the main window.");
                if (ImGui.Button($"Okay##okayShockerSelectorAbort", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
                {
                    ImGui.CloseCurrentPopup();
                }
            }
            else
            {
                ImGui.Text("Please select all shockers that should activate for this trigger:");
                foreach (var shocker in Plugin.Authentification.PishockShockers)
                {
                    bool isEnabled = TriggerObject.Shockers.Find(sh => sh.Code == shocker.Code) != null;
                    if (ImGui.Checkbox($"{shocker.Name}##shockerbox{shocker.Code}", ref isEnabled))
                    { // this could probably be solved more elegantly
                        if (isEnabled) TriggerObject.Shockers.Add(shocker);
                        else TriggerObject.Shockers.RemoveAt(TriggerObject.Shockers.FindIndex(sh => sh.Code == shocker.Code));
                    }
                }

                if (ImGui.Button($"Apply##apply{TriggerObject.Name}", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
                {
                    ImGui.CloseCurrentPopup();
                }

                if (ImGui.Button($"Reset All##resetall{TriggerObject.Name}", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
                {
                    TriggerObject.Shockers.Clear();
                }
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
