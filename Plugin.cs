using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using WoLightning.Classes;
using WoLightning.Types;
using WoLightning.Windows;

namespace WoLightning;


public sealed class Plugin : IDalamudPlugin
{

    // General stuff
    private const string CommandName = "/wolightning";
    private const string CommandNameAlias = "/wol";
    private const string Failsafe = "/red";
    private const string OpenConfigFolder = "/wolfolder";

    public const int currentVersion = 403;
    public const string randomKey = "Currently Unused";

    public string? ConfigurationDirectoryPath { get; set; }

    public IPlayerCharacter LocalPlayerCharacter { get; set; }
    public Player LocalPlayer { get; set; }

    // Services
    public IDalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    public IPluginLog PluginLog { get; init; }
    public IFramework Framework { get; init; }
    public IGameNetwork GameNetwork { get; init; }
    public IChatGui ChatGui { get; init; }
    public IDutyState DutyState { get; init; }
    public IClientState ClientState { get; init; }
    public INotificationManager NotificationManager { get; init; }
    public IObjectTable ObjectTable { get; init; }
    public IGameInteropProvider GameInteropProvider { get; init; }
    public IPartyList PartyList { get; init; }
    public ITargetManager TargetManager { get; init; }
    public TextLog TextLog { get; set; }

    // Gui Interfaces
    public readonly WindowSystem WindowSystem = new("WoLightning");
    private readonly BufferWindow BufferWindow = new BufferWindow();
    private MainWindow? MainWindow { get; set; }
    private ConfigWindow? ConfigWindow { get; set; }
    private MasterWindow? MasterWindow { get; set; }


    // Handler Classes
    public NetworkWatcher NetworkWatcher { get; init; }
    public EmoteReaderHooks? EmoteReaderHooks { get; set; }
    public WebClient? WebClient { get; set; }
    public Authentification? Authentification { get; set; }
    public Configuration? Configuration { get; set; }
    public Operation Operation { get; set; }



    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        ITextureProvider textureProvider,
        IPluginLog pluginlog,
        IFramework framework,
        IGameNetwork gamenetwork,
        IChatGui chatgui,
        IDutyState dutystate,
        IClientState clientstate,
        INotificationManager notificationManager,
        IObjectTable objectTable,
        IGameInteropProvider gameInteropProvider,
        IPartyList partyList,
        ITargetManager targetManager
        )
    {
        // Setup all Services
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        PluginLog = pluginlog;
        Framework = framework;
        GameNetwork = gamenetwork;
        ObjectTable = objectTable;
        ChatGui = chatgui;
        DutyState = dutystate;
        ClientState = clientstate;
        NotificationManager = notificationManager;
        GameInteropProvider = gameInteropProvider;
        PartyList = partyList;
        TargetManager = targetManager;

        Operation = new Operation(this);

        NetworkWatcher = new NetworkWatcher(this); // we need this to check for logins
        WindowSystem.AddWindow(BufferWindow);

        if (ClientState.LocalPlayer != null) onLogin();

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the main window."
        });
        CommandManager.AddHandler(CommandNameAlias, new CommandInfo(OnCommandAlias)
        {
            HelpMessage = "Alias for /wolighting."
        });
        CommandManager.AddHandler(Failsafe, new CommandInfo(OnFailsafe)
        {
            HelpMessage = "Stops the plugin."
        });
        CommandManager.AddHandler(OpenConfigFolder, new CommandInfo(OnOpenConfigFolder)
        {
            HelpMessage = "Opens the configuration folder."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;


        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public void onLogin()
    {
        try
        {

            ConfigurationDirectoryPath = PluginInterface.GetPluginConfigDirectory() + "\\" + ClientState.LocalPlayer.Name;
            if (!Directory.Exists(ConfigurationDirectoryPath)) Directory.CreateDirectory(ConfigurationDirectoryPath);
            if (!Directory.Exists(ConfigurationDirectoryPath + "\\Presets")) Directory.CreateDirectory(ConfigurationDirectoryPath + "\\Presets");
            if (!Directory.Exists(ConfigurationDirectoryPath + "\\MasterPresets")) Directory.CreateDirectory(ConfigurationDirectoryPath + "\\MasterPresets");

            ConfigurationDirectoryPath += "\\";

            TextLog = new TextLog(this, ConfigurationDirectoryPath);

            Configuration = new Configuration();
            try
            {
                Configuration.Initialize(this, false, ConfigurationDirectoryPath);
            }
            catch
            {
                Configuration = new Configuration();
                Configuration.Save();
                sendNotif("Your Configuration has been reset!");
            }

            try
            {
                Authentification = new Authentification(ConfigurationDirectoryPath);
                if (Authentification.Version < new Authentification().Version)
                {
                    Authentification = new Authentification(ConfigurationDirectoryPath, true);
                    sendNotif("Your Authentification has been reset!");
                }
            }
            catch
            {
                Authentification = new Authentification(ConfigurationDirectoryPath, true);
            }

            

            LocalPlayerCharacter = ClientState.LocalPlayer;
            LocalPlayer = new Player(LocalPlayerCharacter.Name.ToString(), (int)LocalPlayerCharacter.HomeWorld.Id, Authentification.ServerKey, NetworkWatcher.running);

            WebClient = new WebClient(this);
            EmoteReaderHooks = new EmoteReaderHooks(this);

            MainWindow = new MainWindow(this);
            ConfigWindow = new ConfigWindow(this);
            MasterWindow = new MasterWindow(this);

            if (Configuration.ActivateOnStart) NetworkWatcher.Start();

            WebClient.createHttpClient();

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(MasterWindow);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex.ToString());
            PluginLog.Error("Something went terribly wrong!!!");
        }
    }

    public void onLogout()
    {

        MainWindow.Dispose();
        ConfigWindow.Dispose();
        MasterWindow.Dispose();

        WindowSystem.RemoveWindow(MainWindow);
        WindowSystem.RemoveWindow(ConfigWindow);
        WindowSystem.RemoveWindow(MasterWindow);

        EmoteReaderHooks.Dispose();
        WebClient.Dispose();

        Configuration.Dispose();
        Authentification.Dispose();
        NetworkWatcher.Stop();
    }


    public void Dispose()
    {
        MainWindow.Dispose();
        ConfigWindow.Dispose();
        MasterWindow.Dispose();
        BufferWindow.Dispose();
        WindowSystem.RemoveWindow(MainWindow);
        WindowSystem.RemoveWindow(ConfigWindow);
        WindowSystem.RemoveWindow(MasterWindow);
        WindowSystem.RemoveWindow(BufferWindow);


        EmoteReaderHooks.Dispose();
        WebClient.Dispose();

        Configuration.Dispose();
        Authentification.Dispose();
        NetworkWatcher.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(CommandNameAlias);
        CommandManager.RemoveHandler(Failsafe);
        CommandManager.RemoveHandler(OpenConfigFolder);
    }

    public void validateShockerAssignments() // Goes through all Triggers and finds Shockers that are no longer saved - then deletes them.
    {
        List<Shocker> shockers = Authentification.PishockShockers;
        
        foreach (var property in typeof(Preset).GetProperties())
        {
            //Log($"{property.Name} - {property.PropertyType}");
            if (property.PropertyType == typeof(Trigger))
            {
                object? obj = property.GetValue(Configuration.ActivePreset);
                if (obj == null) continue;
                Trigger t = (Trigger)obj;

                if (shockers.Count == 0)
                {
                    t.Shockers.Clear();
                    continue;
                }

                bool[] marked = new bool[t.Shockers.Count];
                int i = 0;
                foreach(Shocker sh in t.Shockers)
                {
                    Log(sh);
                    if (shockers.Find(sh2 => sh.Code == sh2.Code) == null) marked[i] = true;
                    i++;
                }
                i = 0;
                foreach(bool del in marked)
                {
                    
                    if (del) t.Shockers.RemoveAt(i);
                    i++;
                }
            }
        }
        Configuration.Save();
    }


    private void OnCommand(string command, string args)
    {
        ToggleMainUI();
    }
    private void OnCommandAlias(string command, string args)
    {
        OnCommand(command, args);
    }
    private void OnFailsafe(string command, string args)
    {
        WebClient.toggleFailsafe();
    }

    private void OnOpenConfigFolder(string command, string args)
    {
        Process.Start(new ProcessStartInfo { Arguments = ConfigurationDirectoryPath, FileName = "explorer.exe" });
    }
    private void DrawUI() => WindowSystem.Draw();
    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
    public void ToggleMasterUI() => MasterWindow.Toggle();
    public void ToggleMasterConfigUI() => MasterWindow.CopiedConfigWindow.Toggle();
    public void ShowMasterUI() => MasterWindow.Open();

    #region Logging
    public void Log(string message)
    {
        if (!this.Configuration.LogEnabled) return;
        PluginLog.Verbose(message);
        TextLog.Log(message);
    }

    public void Log(Object obj)
    {
        if (!this.Configuration.LogEnabled) return;
        PluginLog.Verbose(obj.ToString());
        TextLog.Log(obj);
    }

    public void Log(string message,bool noText)
    {
        if (!this.Configuration.LogEnabled) return;
        PluginLog.Verbose(message);
    }

    public void Log(Object obj, bool noText)
    {
        if (!this.Configuration.LogEnabled) return;
        PluginLog.Verbose(obj.ToString());
    }


    public void Error(string message)
    {
        if (!this.Configuration.LogEnabled) return;
        PluginLog.Error(message);
        TextLog.Log("--- ERROR: \n" + message);
    }

    public void Error(string message, Object obj)
    {
        if (!this.Configuration.LogEnabled) return;
        PluginLog.Error(message);
        PluginLog.Error(obj.ToString());
        TextLog.Log("--- ERROR: \n" + message);
        TextLog.Log(obj);
    }

    public void Error(string message, bool noText)
    {
        if (!this.Configuration.LogEnabled) return;
        PluginLog.Error(message);
    }

    public void Error(string message,Object obj, bool noText)
    {
        if (!this.Configuration.LogEnabled) return;
        PluginLog.Error(message);
        PluginLog.Error(obj.ToString());
    }
    #endregion


    // Todo: Move all of these into a seperate Class
    public Notification getNotifTemp()
    {
        Notification result = new Notification();
        result.InitialDuration = new TimeSpan(0, 0, 7);
        result.Title = "Warrior of Lighting";
        result.Type = NotificationType.Warning;
        return result;
    }

    public void sendNotif(string content)
    {
        Notification result = new Notification();
        result.InitialDuration = new TimeSpan(0, 0, 7);
        result.Title = "Warrior of Lighting";
        result.Type = NotificationType.Warning;
        result.Content = content;
        NotificationManager.AddNotification(result);
    }




}


/*
 * Random Notes
 * Add Positionals as a trigger?
 * 
 * 
 * 
 * 
 * 
 */