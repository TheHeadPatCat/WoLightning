using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Diagnostics;
using System.IO;
using WoLightning.Windows;

namespace WoLightning;
public sealed class Plugin : IDalamudPlugin
{

    // General stuff
    private const string CommandName = "/wolightning";
    private const string CommandNameAlias = "/wol";
    private const string Failsafe = "/red";
    private const string OpenConfigFolder = "/wolfolder";

    public const string currentVersion = "0.3.0.0";
    public const string randomKey = "Currently Unused";
    public string? ConfigurationDirectoryPath { get; set; }

    public string? LocalPlayerNameFull;


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
        IPartyList partyList
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
        NetworkWatcher = new NetworkWatcher(this); // we need this to check for logins
        WindowSystem.AddWindow(BufferWindow);

        if (ClientState.LocalPlayer != null) onLogin();

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the main window."
        });
        CommandManager.AddHandler(CommandNameAlias, new CommandInfo(OnCommandAlias)
        {
            HelpMessage = "Alias for /wolighting"
        });
        CommandManager.AddHandler(Failsafe, new CommandInfo(OnFailsafe)
        {
            HelpMessage = "Stops the plugin."
        });
        CommandManager.AddHandler(OpenConfigFolder, new CommandInfo(OnOpenConfigFolder)
        {
            HelpMessage = "Opens the Configuration Folder."
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
            if(!Directory.Exists(ConfigurationDirectoryPath + "\\Presets")) Directory.CreateDirectory(ConfigurationDirectoryPath + "\\Presets");
            if (!Directory.Exists(ConfigurationDirectoryPath + "\\MasterPresets")) Directory.CreateDirectory(ConfigurationDirectoryPath + "\\MasterPresets");

            ConfigurationDirectoryPath += "\\";

            Configuration = new Configuration();
            LocalPlayerNameFull = ClientState.LocalPlayer.Name.ToString() + "#" + ClientState.LocalPlayer.HomeWorld.Id;
            Configuration.Initialize(this, false, ConfigurationDirectoryPath);

            Authentification = new Authentification(ConfigurationDirectoryPath);


            WebClient = new WebClient(this);
            EmoteReaderHooks = new EmoteReaderHooks(this);

            MainWindow = new MainWindow(this);
            ConfigWindow = new ConfigWindow(this);
            MasterWindow = new MasterWindow(this);

            if (Configuration.ActivateOnStart) NetworkWatcher.Start();

            LocalPlayerNameFull = ClientState.LocalPlayer.Name.ToString() + "#" + ClientState.LocalPlayer.HomeWorld.Id;
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
    public void ToggleMasterConfigUI() => MasterWindow.ConfigWindow.Toggle();

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


    public void handleMasterAnswer(string answer)
    {
        if (answer == "false")
        {
            sendNotif("The other player rejected your Request!");
            //Configuration.MasterNameFull = "";
            return;
        }
        if (answer == "true")
        {
            sendNotif("The other player accepted your Request!");
            //.HasMaster = true;
            Configuration.Save();
        }
    }

    public void handleMasterRequest(string subNameFull)
    {
        MasterWindow.requestingSub = subNameFull;
        if (!MasterWindow.IsOpen) MasterWindow.Toggle();
    }

    public void handleSubUnbind()
    {
        sendNotif("Your Master unbound you!");
        // Configuration.HasMaster = false;
        // Configuration.MasterNameFull = "";
        // Plugin.Authentification.isDisallowed = false;
        Configuration.Save();
    }

    public void updateMasterWindow(string subNameFull, bool active)
    {
        MasterWindow.Configuration.SubsIsActive[subNameFull] = active;
        MasterWindow.updating = false;
    }


    public string devHash()
    {
        return ConfigWindow.debugKmessage;

    }



}
