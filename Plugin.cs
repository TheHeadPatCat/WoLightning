using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;
using WoLightning.Windows;

namespace WoLightning;

public sealed class Plugin : IDalamudPlugin
{

    // General stuff
    private const string CommandName = "/wol";
    private const string CommandNameAlias = "/wolightning";
    private const string Failsafe = "/red";
    private const string OpenConfigFolder = "/wolfolder";

    public const string currentVersion = "0.2.3.4";
    public const string randomKey = "j9fw90j3jf0ska098jf0m30jh2ng0d9f03f9290jf0s9is";
    public string? ConfigurationDirectoryPath { get; set; }


    // Services
    public DalamudPluginInterface PluginInterface { get; init; }
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
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ICommandManager commandManager,
        [RequiredVersion("1.0")] ITextureProvider textureProvider,
        [RequiredVersion("1.0")] IPluginLog pluginlog,
        [RequiredVersion("1.0")] IFramework framework,
        [RequiredVersion("1.0")] IGameNetwork gamenetwork,
        [RequiredVersion("1.0")] IChatGui chatgui,
        [RequiredVersion("1.0")] IDutyState dutystate,
        [RequiredVersion("1.0")] IClientState clientstate,
        [RequiredVersion("1.0")] INotificationManager notificationManager,
        [RequiredVersion("1.0")] IObjectTable objectTable,
        [RequiredVersion("1.0")] IGameInteropProvider gameInteropProvider
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
            NetworkWatcher = new NetworkWatcher( this ); // we need this to check for logins
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
            if (!Directory.Exists(ConfigurationDirectoryPath))Directory.CreateDirectory(ConfigurationDirectoryPath);
            ConfigurationDirectoryPath += "\\";

            Configuration = new Configuration();
            Configuration.LocalPlayerNameFull = ClientState.LocalPlayer.Name.ToString() + "#" + ClientState.LocalPlayer.HomeWorld.Id;
            Configuration.Initialize(false, ConfigurationDirectoryPath);

            if (Configuration.DebugEnabled)
            {
                PluginInterface.OpenDeveloperMenu();
            }

            Authentification = new Authentification(ConfigurationDirectoryPath);


            WebClient = new WebClient(this);
            EmoteReaderHooks = new EmoteReaderHooks(this);

            MainWindow = new MainWindow(this);
            ConfigWindow = new ConfigWindow(this);
            MasterWindow = new MasterWindow(this);




            /*
             * 
             * Todo fix up all of these initialzers // done
             * bugtest masterwindow and maybe make it a tiny bit more useable
             * make configuration save for different player characters // done
             * bugtest network more
             * add debug option to delete server data
             * 
             */

            if (Configuration.ActivateOnStart) NetworkWatcher.Start();

            Configuration.LocalPlayerNameFull = ClientState.LocalPlayer.Name.ToString() + "#" + ClientState.LocalPlayer.HomeWorld.Id;
            WebClient.createHttpClient();

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(MasterWindow);

            // add buttons to ui
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
        OnCommand(command,args);
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
        result.Type = Dalamud.Interface.Internal.Notifications.NotificationType.Warning;
        return result;
    }

    public void sendNotif(string content)
    {
        Notification result = new Notification();
        result.InitialDuration = new TimeSpan(0, 0, 7);
        result.Title = "Warrior of Lighting";
        result.Type = Dalamud.Interface.Internal.Notifications.NotificationType.Warning;
        result.Content = content;
        NotificationManager.AddNotification(result);
    }


    public void handleMasterAnswer(string answer)
    {
        if(answer == "false")
        {
            sendNotif("The other player rejected your Request!");
            Configuration.MasterNameFull = "";
            return;
        }
        if(answer == "true")
        {
            sendNotif("The other player accepted your Request!");
            Configuration.HasMaster = true;
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
        Configuration.HasMaster = false;
        Configuration.MasterNameFull = "";
        Configuration.isDisallowed = false;
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
