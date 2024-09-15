using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WoLightning.Classes;

namespace WoLightning.Types
{
    [Serializable]
    public class Preset(string Name, string CreatorFullName)
    {
        public string Name { get; set; } = Name;
        public string CreatorFullName { get; set; } = CreatorFullName;


        public bool IsPassthroughAllowed { get; set; } = false;
        public int globalTriggerCooldown { get; set; } = 10;
        public float globalTriggerCooldownGate { get; set; } = 0.75f;
        public bool showCooldownNotifs { get; set; } = false;


        // Social Triggers
        public Trigger GetPat { get; set; } = new Trigger("GetPat","You got pat'd!",false);
        public Trigger GetSnapped { get; set; } = new Trigger("GetSnapped","You got snap'd!", false);
        public Trigger LoseDeathRoll { get; set; } = new Trigger("LoseDeathroll","You lost a deathroll!", false);
        public Trigger SitOnFurniture { get; set; } = new Trigger("SitOnFurniture","You are sitting on furniture!", false);
        public Trigger SayFirstPerson { get; set; } = new Trigger("SayFirstPerson","You refered to yourself wrongly!", false);
        public Trigger SayBadWord = new Trigger("SayBadWord","You said a bad word!", true);
        public Trigger DontSayWord = new Trigger("DontSayWord","You forgot to say a enforced word!", true);

        // Combat Triggers
        public Trigger TakeDamage { get; set; } = new Trigger("TakeDamage","You took damage!", true);
        public Trigger FailMechanic { get; set; } = new Trigger("FailMechanic","You failed a mechanic!", true);
        public Trigger Die { get; set; } = new Trigger("Die","You died!", false);
        public Trigger PartymemberDies { get; set; } = new Trigger("PartymemberDies","A partymember died!", false);
        public Trigger Wipe { get; set; } = new Trigger("Wipe","Your party wiped!", false);

        // Custom Triggers
        public List<RegexTrigger> SayCustomMessage { get; set; } = new List<RegexTrigger>();
        public List<ChatType.ChatTypes> Channels { get; set; } = new List<ChatType.ChatTypes>();


        public void resetInvalidTriggers()
        {
            Preset cleanPreset = new Preset("Clean", "None");

            foreach (var property in typeof(Preset).GetProperties())
            {
                //Log($"{property.Name} - {property.PropertyType}");
                if (property.PropertyType == typeof(Trigger))
                {
                    object? obj = property.GetValue(this);
                    if (obj == null) continue;
                    Trigger t = (Trigger)obj;

                    if (!t.ValidateNoShockers()) {
                        property.SetValue(this,property.GetValue(cleanPreset));
                        ((Trigger)property.GetValue(this)!).hasBeenReset = true;
                     }
                }
            }
        }
    }

    
}
