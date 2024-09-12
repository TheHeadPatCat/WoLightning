using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.IO;
using WoLightning.Types;

namespace WoLightning.Windows
{

    public class UpdateWindow : Window, IDisposable
    {
        Plugin Plugin { get; init; }
        public List<UpdateEntry> Entries = new();

        public UpdateWindow(Plugin plugin) : base("Warrior of Lightning Updates##updateWindow")
        {
            this.Plugin = plugin;
            String[] f = [];
            if (File.Exists(Plugin.PluginInterface.GetPluginConfigDirectory() + "changelog.json")) f = File.ReadAllLines(Plugin.PluginInterface.GetPluginConfigDirectory() + "changelog.json");

            foreach (string s in f)
            {
                if (String.IsNullOrEmpty(s)) continue;
                Plugin.Log(s);
            }
        }

        public void Dispose()
        {
            if (this.IsOpen) this.Toggle();
        }

        public override async void Draw()
        {

        }
    }
}
