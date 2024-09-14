using System;
using System.Collections.Generic;

namespace WoLightning.Types
{
    [Serializable]
    public class Preset(string Name)
    {
        public string Name { get; set; } = Name;
        public string CreatorFullName { get; set; }


        public bool IsPassthroughAllowed { get; set; } = false;
        public int globalTriggerCooldown { get; set; } = 10;
        public float globalTriggerCooldownGate { get; set; } = 0.75f;


        // Social Triggers
        public Trigger GetPat { get; set; } = new Trigger("GetPat",false);
        public Trigger GetSnapped { get; set; } = new Trigger("GetSnapped", false);
        public Trigger LoseDeathRoll { get; set; } = new Trigger("LoseDeathroll", false);
        public Trigger SitOnFurniture { get; set; } = new Trigger("SitOnFurniture", false);
        public Trigger SayFirstPerson { get; set; } = new Trigger("SayFirstPerson", false);
        public Trigger SayBadWord = new Trigger("SayBadWord", true);
        public Trigger DontSayWord = new Trigger("DontSayWord", true);

        // Combat Triggers
        public Trigger TakeDamage { get; set; } = new Trigger("TakeDamage", true);
        public Trigger FailMechanic { get; set; } = new Trigger("FailMechanic", true);
        public Trigger Die { get; set; } = new Trigger("Die", false);
        public Trigger PartymemberDies { get; set; } = new Trigger("PartymemberDies", false);
        public Trigger Wipe { get; set; } = new Trigger("Wipe", false);

        // Custom Triggers
        public List<RegexTrigger> SayCustomMessage { get; set; } = new List<RegexTrigger>();
        public List<ChatType.ChatTypes> Channels { get; set; } = new List<ChatType.ChatTypes>();




    }
}
