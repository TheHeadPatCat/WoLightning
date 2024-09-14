using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using WoLightning.Types;


namespace WoLightning
{

    [Serializable]
    public class Configuration : IPluginConfiguration, IDisposable
    {
        public int Version { get; set; } = 410;


        public bool DebugEnabled { get; set; } = false;
        public bool LogEnabled { get; set; } = true;

        // Preset Settings
        [NonSerialized] public Preset ActivePreset = new("Default");
        [NonSerialized] public List<Preset> Presets = new();
        [NonSerialized] public List<String> PresetNames = new(); // used for comboBoxes
        [NonSerialized] public int PresetIndex = 0;

        public string LastPresetName { get; set; } = "Default";
        // General Settings
        public bool ActivateOnStart { get; set; } = false;

        // Generic Lists
        public Dictionary<string, int> PermissionList { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SubsActivePresetIndexes { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, bool> SubsIsDisallowed { get; set; } = new Dictionary<string, bool>();



        // Instance-Only things - Not Saved
        [NonSerialized] public bool isAlternative = false;
        [NonSerialized] public string ConfigurationDirectoryPath;
        [NonSerialized] public Dictionary<string, bool> SubsIsActive = new Dictionary<string, bool>();
        [NonSerialized] private Plugin plugin;

        public void Initialize(Plugin plugin, bool isAlternative, string ConfigurationDirectoryPath)
        {
            this.plugin = plugin;
            this.isAlternative = isAlternative;
            this.ConfigurationDirectoryPath = ConfigurationDirectoryPath;

            string f = "";
            if (!isAlternative && File.Exists(ConfigurationDirectoryPath + "Config.json")) f = File.ReadAllText(ConfigurationDirectoryPath + "Config.json");
            if (isAlternative && File.Exists(ConfigurationDirectoryPath + "masterConfig.json")) f = File.ReadAllText(ConfigurationDirectoryPath + "masterConfig.json");

            Configuration s = DeserializeConfig(f);
            foreach (PropertyInfo property in typeof(Configuration).GetProperties().Where(p => p.CanWrite)) property.SetValue(this, property.GetValue(s, null), null);


            if (Directory.Exists(ConfigurationDirectoryPath + "\\Presets"))
            {
                foreach (var file in Directory.EnumerateFiles(ConfigurationDirectoryPath + "\\Presets"))
                {
                    string p = File.ReadAllText(file);
                    Preset tPreset;
                    try
                    {
                        tPreset = DeserializePreset(p);
                    }
                    catch (Exception e)
                    {
                        plugin.Log(e);
                        tPreset = new Preset("Default");
                    }
                    Presets.Add(tPreset);
                }
            }
            if (Presets.Count == 0)
            {
                ActivePreset = new Preset("Default");
                Presets.Add(ActivePreset);
                Save();
                loadPreset("Default");
                return;
            }
            Save();
            if (!loadPreset(LastPresetName)) loadPreset("Default");
        }

        public void Initialize(Plugin plugin, bool isAlternative, string ConfigurationDirectoryPath, bool createNew)
        {
            this.isAlternative = isAlternative;
            this.ConfigurationDirectoryPath = ConfigurationDirectoryPath;

            Save();
        }

        public void Save()
        {
            LastPresetName = ActivePreset.Name;
            PresetNames = new();
            if (isAlternative)
            {
                foreach (var preset in Presets)
                {
                    PresetNames.Add(preset.Name);
                    savePreset(preset, true);
                }
                File.WriteAllText(ConfigurationDirectoryPath + "masterConfig.json", SerializeConfig(this));
                return;
            }

            foreach (var preset in Presets)
            {
                PresetNames.Add(preset.Name);
                savePreset(preset);
            }
            File.WriteAllText(ConfigurationDirectoryPath + "Config.json", SerializeConfig(this));
        }

        public bool loadPreset(string Name)
        {
            if (!Presets.Exists(preset => preset.Name == Name)) return false;
            ActivePreset = Presets.Find(preset => preset.Name == Name);
            PresetIndex = Presets.IndexOf(ActivePreset);
            LastPresetName = ActivePreset.Name;
            return true;
        }

        public void savePreset(Preset target)
        {
            File.WriteAllText($"{ConfigurationDirectoryPath}\\Presets\\{target.Name}.json", SerializePreset(target));
        }
        public void savePreset(Preset target, bool isAlternative)
        {
            File.WriteAllText($"{ConfigurationDirectoryPath}\\MasterPresets\\{target.Name}.json", SerializePreset(target));
        }

        public void deletePreset(Preset target)
        {
            if (!Presets.Exists(preset => preset.Name == target.Name)) return;
            if (!File.Exists(ConfigurationDirectoryPath + "\\Presets\\" + target.Name + ".json")) return;

            File.Delete(ConfigurationDirectoryPath + "\\Presets\\" + target.Name + ".json");
            Presets.Remove(target);
            if (!Presets.Exists(preset => preset.Name == "Default")) Presets.Add(new Preset("Default"));
            loadPreset("Default");
            Save();
        }


        internal static string SerializeConfig(object? config)
        {
            return JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        internal static Configuration DeserializeConfig(string input)
        {
            if (input == "") return new Configuration();
            return JsonConvert.DeserializeObject<Configuration>(input);
        }

        internal static string SerializePreset(object? preset)
        {
            return JsonConvert.SerializeObject(preset, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        internal static Preset DeserializePreset(string input)
        {
            if (input == "") ; // cry
            return JsonConvert.DeserializeObject<Preset>(input);
        }

        public void updateSetting(string PropertyName, int Value)
        {
            foreach (PropertyInfo prop in GetType().GetProperties())
            {

                if (prop.PropertyType == typeof(int) && prop.Name.ToLower() == PropertyName.ToLower())
                {
                    try
                    {
                        prop.SetValue(this, Value);
                        Save();
                        return;
                    }
                    catch
                    {
                        // tried to set invalid setting
                    }
                }
            }
        }

        public void updateSetting(string PropertyName, int[] Value)
        {
            foreach (PropertyInfo prop in GetType().GetProperties())
            {

                if (prop.PropertyType == typeof(int[]) && prop.Name.ToLower() == PropertyName.ToLower())
                {
                    try
                    {
                        prop.SetValue(this, Value);
                        Save();
                        return;
                    }
                    catch
                    {
                        // tried to set invalid setting
                    }
                }
            }
        }

        public void updateSetting(string PropertyName, bool Value)
        {
            foreach (PropertyInfo prop in GetType().GetProperties())
            {

                if (prop.PropertyType == typeof(bool) && prop.Name.ToLower() == PropertyName.ToLower())
                {
                    try
                    {
                        prop.SetValue(this, Value);
                        Save();
                        return;
                    }
                    catch
                    {
                        // tried to set invalid setting
                    }
                }
            }
        }

        public void Dispose()
        {
            Save();
        }
    }
}