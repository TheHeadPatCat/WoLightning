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
        // wipes the entire thing (except the above) or if version number is higher than found one
        public int Version { get; set; } = 21;
        public bool DebugEnabled { get; set; } = false;

        // General Settings
        public string ActivePreset = "default";
        public int ActivePresetIndex = 0;
        public string PresetCreatorNameFull = "";
        public bool ActivateOnStart { get; set; } = false;
        public bool IsWhitelistEnforced { get; set; } = false;
        public bool IsPassthroughAllowed { get; set; } = false;

        public string LocalPlayerNameFull = "";
        public int globalTriggerCooldown { get; set; } = 10;
        public float globalTriggerCooldownGate { get; set; } = 0.75f;

        // Are we a Master?
        public bool IsMaster { get; set; } = false;
        public int LeashEmoteIdMaster { get; set; } = 0;
        [NonSerialized] public string isLeashedTo = "";


        // Are we controlled by a Master?
        public bool HasMaster { get; set; } = false;
        public string MasterNameFull { get; set; } = string.Empty;
        public bool isDisallowed { get; set; } = false; //locks the interface
        public int LeashEmoteIdSub { get; set; } = 0;
        public bool isLeashed { get; set; } = false;
        public bool CommandActionsEnabled { get; set; } = false;

        // Section Badword
        public bool ShockOnBadWord { get; set; } = false;



        // Settings are : [Mode, Intensity, Duration]
        // Mode: 0 Shock, 1 Vibrate, 2 Beep
        // Intensity: 1-100
        // Duration: 1-10 (seconds)
        // Social Triggers

        public Trigger GetPat { get; set; } = new Trigger("GetPat");
        public Trigger LoseDeathRoll { get; set; } = new Trigger("LoseDeathroll");
        public Trigger SayFirstPerson { get; set; } = new Trigger("SayFirstPerson");
        public bool ShockOnPat { get; set; } = false;
        public int[] ShockPatSettings { get; set; } = [0, 1, 1];
        public bool ShockOnDeathroll { get; set; } = false;
        public int[] ShockDeathrollSettings { get; set; } = [0, 1, 1];
        public bool ShockOnFirstPerson { get; set; } = false;
        public int[] ShockFirstPersonSettings { get; set; } = [0, 1, 1];


        // Combat Triggers
        public bool ShockOnDamage { get; set; } = false;
        public int[] ShockDamageSettings { get; set; } = [0, 1, 1];
        public bool ShockOnVuln { get; set; } = false;
        public int[] ShockVulnSettings { get; set; } = [0, 1, 1];
        public bool ShockOnRescue { get; set; } = false; // TODO
        public int[] ShockRescueSettings { get; set; } = [0, 1, 1];
        public bool ShockOnDeath { get; set; } = false;
        public int[] ShockDeathSettings { get; set; } = [0, 1, 1];
        public bool ShockOnWipe { get; set; } = false;
        public int[] ShockWipeSettings { get; set; } = [0, 1, 1];
        public bool DeathMode { get; set; } = false;
        public int[] DeathModeSettings { get; set; } = [0, 100, 15];

        // Lists
        public Dictionary<string, int> PermissionList { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string[]> Presets { get; set; } = new Dictionary<string, string[]>();
        public Dictionary<string, int[]> ShockBadWordSettings { get; set; } = new Dictionary<string, int[]>();
        public Dictionary<string, int[]> CommandActionSettings { get; set; } = new Dictionary<string, int[]>();
        public List<string> OwnedSubs { get; set; } = new List<string>();
        public Dictionary<string, int> SubsActivePresetIndexes { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, bool> SubsIsDisallowed { get; set; } = new Dictionary<string, bool>();
        public List<RegexTrigger> Triggers { get; set; } = new List<RegexTrigger>();
        public List<ChatType.ChatTypes> Channels { get; set; } = new List<ChatType.ChatTypes>();


        // Instance-Only things
        [NonSerialized] public bool isAlternative = false;
        [NonSerialized] public string ConfigurationDirectoryPath;
        [NonSerialized] public Dictionary<string, bool> SubsIsActive = new Dictionary<string, bool>();
        [NonSerialized] private Plugin plugin;

        public void Initialize(Plugin plugin, bool isAlternative, string ConfigurationDirectoryPath)
        {
            this.plugin = plugin;
            this.isAlternative = isAlternative;
            this.ConfigurationDirectoryPath = ConfigurationDirectoryPath;
            isLeashed = false;

            string f = "";
            if (!isAlternative && File.Exists(ConfigurationDirectoryPath + "Config.json")) f = File.ReadAllText(ConfigurationDirectoryPath + "Config.json");
            if (isAlternative && File.Exists(ConfigurationDirectoryPath + "masterConfig.json")) f = File.ReadAllText(ConfigurationDirectoryPath + "masterConfig.json");

            Configuration s = DeserializeConfig(f);
            foreach (PropertyInfo property in typeof(Configuration).GetProperties().Where(p => p.CanWrite)) property.SetValue(this, property.GetValue(s, null), null);

            if (!HasMaster && MasterNameFull != "") MasterNameFull = "";
            if (PresetCreatorNameFull == "") PresetCreatorNameFull = LocalPlayerNameFull;
            if (Presets.Count == 0) savePreset("default");
            Save();
        }

        public void Initialize(Plugin plugin, bool isAlternative, string ConfigurationDirectoryPath, bool createNew)
        {
            this.isAlternative = isAlternative;
            this.ConfigurationDirectoryPath = ConfigurationDirectoryPath;
            isLeashed = false;

            savePreset("default");

            Save();
        }

        public void Save()
        {
            if (isAlternative)
            {
                File.WriteAllText(ConfigurationDirectoryPath + "masterConfig.json", SerializeConfig(this));
                return;
            }
            File.WriteAllText(ConfigurationDirectoryPath + "Config.json", SerializeConfig(this));
        }

        public bool checkPermission(string name, int neededLevel) // 0 for no perms needed, 1 for whitelist needed, 2 for privileged needed
        {
            name = name.ToLower();
            if (name == MasterNameFull) return true;
            bool found = false;
            bool pass = false;
            if (IsWhitelistEnforced && neededLevel == 0) neededLevel = 1;
            foreach (var (Name, permission) in PermissionList)
            {
                if (Name.Contains(name) || name.Contains(Name))
                {
                    if (permission == 0) pass = false;
                    if (permission == 1) pass = neededLevel <= permission; // wtf
                    if (permission == 2) pass = true;
                    return pass;
                }
            }

            if (!found) return neededLevel <= 0;
            return pass;

        }

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

                foreach (var Trigger in Triggers)
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
            */

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

            /*if (Sharestring[0] == 'c')
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
            }*/

            if (Sharestring[0] == 't')
            {
                Triggers.Clear();
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
                    Triggers.Add(temp);
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

        public bool loadPreset(string preset)
        {
            //PluginLog.Info("Load Preset got called");
            string[] codes = new string[4];

            var x = 0;
            var found = false;
            foreach (var key in Presets.Keys)
            {
                if (key == preset)
                {
                    //PluginLog.Info($"Preset found at x: {x} - {Presets.Values.ToArray()[x][0]}");
                    codes = Presets.Values.ToArray()[x];
                    found = true;
                    break;
                }
                x++;
            }
            if (!found) return false;
            foreach (var code in codes)
            {
                DecodeConfiguration(code.ToLower());
            }
            Save();
            return true;
        }

        public void importPreset(string code)
        {
            string[] codes = code.Split("*");

            ActivePreset = DecodeWord(codes[0].Split("#")[2]);
            if (Presets.ContainsKey(ActivePreset)) Presets.Remove(ActivePreset);
            foreach (var c in codes)
            {

                DecodeConfiguration(c);
            }

            Presets.Add(ActivePreset, codes);
            ActivePresetIndex = Presets.Count - 1;

            swapPreset(ActivePreset);

            Save();
        }

        public void savePreset(string name, string[] codes)
        {
            name = name.ToLower();
            if (Presets.ContainsKey(name)) Presets.Remove(name);
            string[] encoded = new string[codes.Length];
            int x = 0;
            foreach (var code in codes)
            {
                encoded[x] = EncodeConfiguration(code);
                x++;
            }
            Presets.Add(name, encoded);
            Save();
        }

        public void savePreset(string name)
        {
            name = name.ToLower();
            string[] codes =
            [
                EncodeConfiguration("Preset"),
            EncodeConfiguration("Main"),
            EncodeConfiguration("Badword"),
            EncodeConfiguration("Permissions"),
            EncodeConfiguration("Master"),
            EncodeConfiguration("Triggers")
            ];
            if (Presets.ContainsKey(name)) Presets.Remove(name);
            string[] encoded = new string[codes.Length];
            int x = 0;
            foreach (var code in codes)
            {
                encoded[x] = code;
                x++;
            }
            Presets.Add(name, encoded);
            Save();
        }

        public void swapPreset(string name)
        {
            name = name.ToLower();
            savePreset(ActivePreset);
            if (!loadPreset(name))
            {
                return;
            }
            ActivePreset = name;
            int x = 0;
            foreach (var key in Presets.Keys)
            {
                if (key.Equals(name)) ActivePresetIndex = x;
                x++;
            }
        }

        public void swapPreset(string name, bool rem)
        {
            name = name.ToLower();
            if (!rem) savePreset(ActivePreset);
            loadPreset(name);
            ActivePreset = name;
            ActivePresetIndex = 0;
        }

        public string sharePreset(string preset)
        {
            savePreset(ActivePreset);
            string result = "";
            var x = 0;
            foreach (var key in Presets.Keys)
            {
                //PluginLog.Info($"Checking Preset x: {x} - {key}");
                if (key == preset)
                {
                    //PluginLog.Info($"Preset found at x: {x} - {Presets.Values.ToArray()[x][0]}");
                    string[] codes = new string[4];
                    codes = Presets.Values.ToArray()[x];
                    foreach (var code in codes)
                    {
                        //PluginLog.Info($"Patching String: {code}");
                        result += code;
                        result += "*";
                    }
                    return result;
                }
                x++;
            }

            //PluginLog.Info($"Preset wasnt found");
            return result;
        }

        public string sharePreset()
        {
            savePreset(ActivePreset);
            string result = "";
            var x = 0;
            foreach (var key in Presets.Keys)
            {
                //PluginLog.Info($"Checking Preset x: {x} - {key}");
                if (key == ActivePreset)
                {
                    //PluginLog.Info($"Preset found at x: {x} - {Presets.Values.ToArray()[x][0]}");
                    string[] codes = new string[4];
                    codes = Presets.Values.ToArray()[x];
                    foreach (var code in codes)
                    {
                        //PluginLog.Info($"Patching String: {code}");
                        result += code;
                        result += "*";
                    }
                    return result;
                }
                x++;
            }

            //PluginLog.Info($"Preset wasnt found");
            return result;
        }

        public char[] shareCypher()
        {
            return ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', ' ', '\'', '+', '!', '=', '?', '%'];
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

        public void updateBoolSetting(string input)
        { // todo possibly remove this?
            string[] keyValue = input.Split("#");
            foreach (PropertyInfo prop in GetType().GetProperties())
            {

                if (prop.PropertyType == typeof(bool) && prop.Name.ToLower() == keyValue[0].ToLower())
                {
                    try
                    {
                        prop.SetValue(this, bool.Parse(keyValue[1]));
                        return;
                    }
                    catch
                    {
                        // tried to set invalid setting
                    }
                }
            }
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