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
        public Trigger GetPat { get; set; } = new Trigger("GetPat");
        public Trigger LoseDeathRoll { get; set; } = new Trigger("LoseDeathroll");
        public Trigger SayFirstPerson { get; set; } = new Trigger("SayFirstPerson");
        public Trigger SayBadWord = new Trigger("SayBadWord");
        public Trigger DontSayWord = new Trigger("DontSayWord");

        // Combat Triggers
        public Trigger TakeDamage { get; set; } = new Trigger("TakeDamage");
        public Trigger FailMechanic { get; set; } = new Trigger("FailMechanic");
        public Trigger Die { get; set; } = new Trigger("Die");
        public Trigger PartymemberDies { get; set; } = new Trigger("PartymemberDies");
        public Trigger Wipe { get; set; } = new Trigger("Wipe");

        // Custom Triggers
        public List<RegexTrigger> SayCustomMessage { get; set; } = new List<RegexTrigger>();
        public List<ChatType.ChatTypes> Channels { get; set; } = new List<ChatType.ChatTypes>();




    }
}
