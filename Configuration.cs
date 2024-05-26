using Dalamud.Configuration;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Collections.Generic;

namespace WoLightning;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool ActivateOnStart { get; set; } = false;

    public string PishockName { get; set; } = string.Empty;
    public string PishockShareCode { get; set;} = string.Empty;
    public string PishockApiKey { get; set;} = string.Empty;

    // Settings are : [Mode, Intensity, Duration]
    // Mode: 0 Shock, 1 Vibrate, 2 Beep
    // Intensity: 1-100
    // Duration: 1-10 (seconds)
    // Social Triggers
    public bool ShockOnPat { get; set; } = false;
    public int[] ShockPatSettings {  get; set; } = new int[3];
    public bool ShockOnDeathroll { get; set; } = false;
    public int[] ShockDeathrollSettings { get; set; } = new int[3];
    public bool ShockOnBadWord { get; set; } = false;
    public Dictionary<string, int[]> ShockBadWordSettings { get; set; } = new Dictionary<string, int[]>();

    // Combat Triggers
    public bool ShockOnDamage { get; set; } = false;
    public int[] ShockDamageSettings { get; set; } = new int[3];
    public bool ShockOnVuln { get; set; } = false;
    public int[] ShockVulnSettings { get; set; } = new int[3];
    public bool ShockOnRescue { get; set; } = false;
    public int[] ShockRescueSettings { get; set; } = new int[3];
    public bool ShockOnDeath { get; set; } = false;
    public int[] ShockDeathSettings { get; set; } = new int[3];
    public bool ShockOnWipe { get; set; } = false;
    public int[] ShockWipeSettings { get; set; } = new int[3];

    // Misc Triggers
    public bool ShockOnRandom { get; set; } = false;
    public int[] ShockRandomSettings { get; set; } = new int[3];


    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private DalamudPluginInterface? PluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
        PluginInterface!.SavePluginConfig(this);
    }

    public void Save()
    {
        PluginInterface!.SavePluginConfig(this);
    }
}
