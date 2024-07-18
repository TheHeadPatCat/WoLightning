using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace WoLightning.Types
{
    [Serializable]
    public class Trigger
    {
        public string Name { get; set; } // Name of the Trigger itself, used for Logging
        public bool Enabled {  get; set; }
        public OpType OpMode { get; set; }
        public int Intensity { get; set; }
        public int Duration { get; set; }
        public int Cooldown { get; set; }
        public string[] Shockers { get; set; } = new string[0]; // List of all Shocker Codes to run on
        public Dictionary<String, int> Settings { get; set; } = new Dictionary<String, int>();// Custom Settings for each Trigger

        public Trigger(string Name) {
            this.Name = Name;
        }

        public Trigger(string Name, Dictionary<String,int> Settings)
        {
            this.Name = Name;
            this.Settings = Settings;
        }

        public Trigger(string Name, Dictionary<String, int> Settings, int[] SettingsApi)
        {
            this.Name = Name;
            this.Settings = Settings;
            OpMode = (OpType)SettingsApi[0];
            Intensity = SettingsApi[1];
            Duration = SettingsApi[2];
        }
        public Trigger(string Name, Dictionary<String, int> Settings, OpType defaultOpMode, int defaultIntensity, int defaultDuration)
        {
            this.Name = Name;
            this.Settings = Settings;
            OpMode = defaultOpMode;
            Intensity = defaultIntensity;
            Duration = defaultDuration;
        }


        public bool Validate()
        {
            if (Intensity < 1 || Intensity > 100 || Duration < 1 || Duration > 10 || Shockers.Length < 1 || Shockers.Length > 5)return false;

            return true;
        }
    }
}
