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
        public int PluginVersion { get; set; } = 301;
        public int Version { get; set; } = 30;
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
                    Preset tPreset = DeserializePreset(p);
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



        // old de-en-code stuffs
        /*
        public string EncodeConfiguration(string section)
        {
            string result = new string("");

            if (section == "Preset")
            {
                result += "W";
                result += "#";
                result += EncodeWord(LocalPlayerNameFull);
                result += "#";
                result += EncodeWord(ActivePreset);
                result += "#";
                return result.ToLower();
            }

            if (section == "Main") // pass,pat,roll,damage,vuln,rescue,death,wipe
            {
                ////PluginLog.Info($"Encoding Main Settings");
                result += "M";
                result += IsPassthroughAllowed ? 1 : 0;
                result += "#";

                result += globalTriggerCooldown;
                result += "#";

                result += ShockOnPat ? 1 : 0;
                result += EncodeArray(ShockPatSettings);
                result += "#";

                result += ShockOnDeathroll ? 1 : 0;
                result += EncodeArray(ShockDeathrollSettings);
                result += "#";

                result += ShockOnFirstPerson ? 1 : 0;
                result += EncodeArray(ShockFirstPersonSettings);
                result += "#";

                result += ShockOnDamage ? 1 : 0;
                result += EncodeArray(ShockDamageSettings);
                result += "#";

                result += ShockOnVuln ? 1 : 0;
                result += EncodeArray(ShockVulnSettings); //mhhh static conversions
                result += "#";

                result += ShockOnRescue ? 1 : 0;
                result += EncodeArray(ShockRescueSettings);
                result += "#";

                result += ShockOnDeath ? 1 : 0;
                result += EncodeArray(ShockDeathSettings);
                result += "#";

                result += ShockOnWipe ? 1 : 0;
                result += EncodeArray(ShockWipeSettings);
                result += "#";

                result += DeathMode ? 1 : 0;
                result += EncodeArray(DeathModeSettings);
                result += "#";

                ////PluginLog.Info($"Encoded Main Settings into string: {result}");
                return result.ToLower();
            }

            if (section == "Badword")
            {
                ////PluginLog.Info($"Encoding Bad words...");
                result += "B";
                result += ShockOnBadWord ? 1 : 0;
                result += "#";

                foreach (var (word, settings) in ShockBadWordSettings)
                {
                    result += EncodeWord(word.ToLower());
                    result += "-";
                    result += EncodeArray(settings);
                    result += "#";
                }
                //PluginLog.Info($"Encoded Badwords into string: {result}");
                return result.ToLower();
            }

            if (section == "Permissions")
            {
                //PluginLog.Info($"Encoding Permissions...");
                result += "P";
                result += IsWhitelistEnforced ? 1 : 0;
                result += "#";

                foreach (var (playerName, permission) in PermissionList)
                {
                    result += EncodeWord(playerName.ToLower());
                    result += ".";
                    result += permission;
                    result += "#";
                }
                //PluginLog.Info($"Encoded Permissions into string: {result}");
                return result.ToLower();
            }

            if (section == "Master")
            {
                //PluginLog.Info($"Encoding Master Settings...");
                result += "C";
                result += CommandActionsEnabled ? 1 : 0;
                result += "#";

                foreach (var (action, settings) in CommandActionSettings)
                {
                    result += EncodeWord(action.ToLower());
                    result += "-";
                    result += EncodeArray(settings);
                    result += "#";
                }
                //PluginLog.Info($"Encoded Master Settings into string: {result}");
                return result.ToLower();
            }

            if (section == "Triggers")
            {
                result += "T";

                foreach (var Trigger in CustomMessageTriggers)
                {
                    result += EncodeWord(Trigger.Name);
                    result += ".";
                    result += Trigger.RegexString;
                    result += ".";
                    result += Trigger.Enabled ? 1 : 0;
                    result += "." + Trigger.Mode + "." + Trigger.Intensity + "." + Trigger.Duration;
                    result += "#";
                }
                return result.ToLower();
            }

            return result.ToLower();
        }
        private string EncodeArray(int[] array)
        {
            string result = new string("");
            result += array[0].ToString("X2");
            result += array[1].ToString("X2");
            result += array[2].ToString("X2");
            return result;
        }

        public void DecodeConfiguration(string Sharestring)
        {

            //PluginLog.Info($"Starting Decoding on: {Sharestring}");
            //int intAgain = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
            if (Sharestring.Length == 0) return;

            if (Sharestring[0] == 'w')
            {
                //PluginLog.Info($"Decoding Preset Settings from string: {Sharestring}");
                var x = 0;
                foreach (var part in Sharestring.Split("#"))
                {
                    if (x == 1) PresetCreatorNameFull = DecodeWord(part);
                    if (x == 2) ActivePreset = DecodeWord(part);
                    x++;
                }
                Save();
                return;
            }

            /*
            if (section == "Preset")
            {
                result += "W";
                result += EncodeWord(LocalPlayerName);
                result += "#";
                result += EncodeWord(ActivePreset);
                result += "#";
                return result;
            }
            

            if (Sharestring[0] == 'm')
            {
                //PluginLog.Info($"Decoding Main Settings from string: {Sharestring}");
                var x = 0;
                foreach (var part in Sharestring.Split("#"))
                {
                    if (part.Length == 0) break;
                    switch (x)
                    {
                        case 0:
                            {
                                //PluginLog.Info($"Saving Passthrough {part}");
                                IsPassthroughAllowed = part[1] == '1';
                                break;
                            }
                        case 1:
                            globalTriggerCooldown = int.Parse(part + "");
                            break;
                        case 2:
                            //PluginLog.Info($"Saving Pat {part}");
                            ShockOnPat = part[0] == '1';
                            ShockPatSettings = DecodeArray(part.Substring(1));
                            break;
                        case 3:
                            //PluginLog.Info($"Saving Deathroll {part}");
                            ShockOnDeathroll = part[0] == '1';
                            ShockDeathrollSettings = DecodeArray(part.Substring(1));
                            break;
                        case 4:
                            ShockOnFirstPerson = part[0] == '1';
                            ShockFirstPersonSettings = DecodeArray(part.Substring(1));
                            break;
                        case 5:
                            //PluginLog.Info($"Saving Damage {part}");
                            ShockOnDamage = part[0] == '1';
                            ShockDamageSettings = DecodeArray(part.Substring(1));
                            break;
                        case 6:
                            //PluginLog.Info($"Saving Vuln {part}");
                            ShockOnVuln = part[0] == '1';
                            ShockVulnSettings = DecodeArray(part.Substring(1));
                            break;
                        case 7:
                            //PluginLog.Info($"Saving Rescue {part}");
                            ShockOnRescue = part[0] == '1';
                            ShockRescueSettings = DecodeArray(part.Substring(1));
                            break;
                        case 8:
                            //PluginLog.Info($"Saving Death {part}");
                            ShockOnDeath = part[0] == '1';
                            ShockDeathSettings = DecodeArray(part.Substring(1));
                            break;
                        case 9:
                            //PluginLog.Info($"Saving Wipe {part}");
                            ShockOnWipe = part[0] == '1';
                            ShockWipeSettings = DecodeArray(part.Substring(1));
                            break;
                        case 10:
                            //PluginLog.Info($"Saving Wipe {part}");
                            DeathMode = part[0] == '1';
                            DeathModeSettings = DecodeArray(part.Substring(1));
                            break;
                    }
                    x++;
                }
                Save();
                return;
            }

            if (Sharestring[0] == 'b')
            {
                //PluginLog.Info($"Decoding Badwords from string: {Sharestring}");
                ShockBadWordSettings.Clear();
                var x = 0;
                foreach (var part in Sharestring.Split("#"))
                {
                    //PluginLog.Info($"x= {x} part: {part}");
                    if (part.Length == 0) break;
                    if (x == 0) ShockOnBadWord = part[1].ToString() == "1";

                    else
                    {
                        //PluginLog.Info($"Decoding Word: {part}");
                        ShockBadWordSettings.Add(DecodeWord(part.Split("-")[0]), DecodeArray(part.Split("-")[1]));
                    }
                    x++;

                }
                Save();
                return;
            }

            if (Sharestring[0] == 'p')
            {
                //PluginLog.Info($"Decoding Permissions from string: {Sharestring}");
                PermissionList.Clear();
                var x = 0;
                foreach (var part in Sharestring.Split("#"))
                {
                    //Plugin.//PluginLog.Info($"x= {x} part: {part}");
                    if (part.Length == 0) break;
                    if (x == 0) IsWhitelistEnforced = part[1].ToString() == "1";

                    else
                    {
                        //Plugin.//PluginLog.Info($"Decoding Name: {part}");
                        PermissionList.Add(DecodeWord(part.Split(".")[0]), int.Parse(part.Split(".")[1]));

                    }
                    x++;

                }
                Save();
                return;
            }

            if (Sharestring[0] == 'c')
            {
                if (PresetCreatorNameFull != LocalPlayerNameFull || PresetCreatorNameFull == MasterNameFull) return;
                CommandActionSettings.Clear();
                var x = 0;
                foreach (var part in Sharestring.Split("#"))
                {
                    if (part.Length == 0) break;
                    if (x == 0) CommandActionsEnabled = part[1].ToString() == "1";

                    else
                    {
                        CommandActionSettings.Add(DecodeWord(part.Split("-")[0]), DecodeArray(part.Split("-")[1]));
                    }
                    x++;

                }
                Save();
            }

            if (Sharestring[0] == 't')
            {
                CustomMessageTriggers.Clear();
                var x = 0;
                RegexTrigger temp;
                foreach (var part in Sharestring.Split("#"))
                {
                    if (part.Length <= 2) break;
                    var lParts = part.Split(".");
                    temp = new RegexTrigger();
                    temp.Name = DecodeWord(lParts[0]);
                    temp.RegexString = lParts[1];
                    temp.Regex = new System.Text.RegularExpressions.Regex(lParts[1]);
                    temp.Enabled = lParts[2] == "1";
                    temp.Intensity = int.Parse(lParts[3]);
                    temp.Duration = int.Parse(lParts[4]);
                    CustomMessageTriggers.Add(temp);
                    x++;

                }
                Save();
            }
        }

        private int[] DecodeArray(string Sharestring)
        {
            int[] result = new int[3];
            result[0] = int.Parse(Sharestring.Substring(0, 2).ToString(), System.Globalization.NumberStyles.HexNumber);
            result[1] = int.Parse(Sharestring.Substring(2, 2).ToString(), System.Globalization.NumberStyles.HexNumber);
            result[2] = int.Parse(Sharestring.Substring(4, 2).ToString(), System.Globalization.NumberStyles.HexNumber);
            return result;
        }

        public string EncodeWord(string word) // """""""""Encoding"""""""""""" (i just didnt want it readable)
        {
            word = word.ToLower();
            //PluginLog.Info($"Started encoding: {word}");
            string result = "";
            foreach (char c in word)
            {
                var x = 0;
                foreach (char en in shareCypher())
                {
                    //Plugin.//PluginLog.Info($" encoding letter: {c} ind: {x}");
                    try
                    {
                        result += int.Parse(c.ToString());
                        break;
                    }
                    catch { }
                    if (c == en)
                    {
                        result += shareCypher()[x + 3];
                        break;
                    }
                    x++;
                }
                //Plugin.//PluginLog.Info($" progress: {result}");
            }
            //PluginLog.Info($"Finished: {result}");
            return result;
        }

        public string DecodeWord(string word)
        {
            word = word.ToLower();
            //PluginLog.Info($"Started Decoding: {word}");
            string result = "";
            foreach (char c in word)
            {
                var x = 0;
                foreach (char en in shareCypher())
                {
                    //Plugin.//PluginLog.Info($" decoding letter: {c} ind: {x}");
                    try
                    {
                        result += int.Parse(c.ToString());
                        break;
                    }
                    catch { }
                    if (c == en)
                    {
                        result += shareCypher()[x - 3];
                        break;
                    }
                    x++;
                }
                //Plugin.//PluginLog.Info($" progress: {result}");
            }
            //PluginLog.Info($"Finished: {result}");
            return result;
        }

        */

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