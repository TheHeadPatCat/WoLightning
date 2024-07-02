using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Network;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel.GeneratedSheets2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using WoLightning.Types;
using static WoLightning.Types.ChatType;


namespace WoLightning
{
    public class NetworkWatcher
    {
        public bool running = false;
        Plugin Plugin;



        PlayerCharacter? PlayerCharacter;
        PlayerCharacter? MasterCharacter;
        private Timer lookingForMaster = new Timer(new TimeSpan(0, 0, 5));
        private int masterLookDelay = 0;

        public bool isLeashed = false;
        public bool isLeashing = false;
        private Timer LeashTimer = new Timer(new TimeSpan(0, 0, 1));

        private int DeathModeCount = 0;

        public NetworkWatcher(Plugin plugin)
        {
            Plugin = plugin;
            Plugin.ClientState.Login += HandleLogin;
        }

        public void Start() //Todo only start specific services, when respective trigger is on
        {
            running = true;
            //Plugin.GameNetwork.NetworkMessage += HandleNetworkMessage;
            LeashTimer.Elapsed += (sender, e) => CheckLeashDistance();
            //LeashTimer.Start();

            Plugin.ChatGui.ChatMessage += HandleChatMessage;
            Plugin.DutyState.DutyWiped += HandleWipe;
            Plugin.ClientState.TerritoryChanged += HandlePlayerTerritoryChange;
            Plugin.EmoteReaderHooks.OnEmoteIncoming += OnEmoteIncoming;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing += OnEmoteOutgoing;
            Plugin.EmoteReaderHooks.OnEmoteSelf += OnEmoteSelf;
            Plugin.EmoteReaderHooks.OnEmoteUnrelated += OnEmoteUnrelated;
        }

        public void Stop()
        {
            if (LeashTimer.Enabled) LeashTimer.Stop();
            LeashTimer.Dispose();

            if (lookingForMaster.Enabled) lookingForMaster.Stop();
            lookingForMaster.Dispose();

            Plugin.ChatGui.ChatMessage -= HandleChatMessage;
            Plugin.DutyState.DutyWiped -= HandleWipe;
            Plugin.ClientState.TerritoryChanged -= HandlePlayerTerritoryChange;
            Plugin.EmoteReaderHooks.OnEmoteIncoming -= OnEmoteIncoming;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing -= OnEmoteOutgoing;
            Plugin.EmoteReaderHooks.OnEmoteSelf -= OnEmoteSelf;
            Plugin.EmoteReaderHooks.OnEmoteUnrelated -= OnEmoteUnrelated;
            running = false;
        }

        public void Dispose()
        {
            //Plugin.GameNetwork.NetworkMessage -= HandleNetworkMessage;

            if (LeashTimer.Enabled) LeashTimer.Stop();
            LeashTimer.Dispose();

            if (lookingForMaster.Enabled) lookingForMaster.Stop();
            lookingForMaster.Dispose();

            Plugin.ChatGui.ChatMessage -= HandleChatMessage;
            Plugin.DutyState.DutyWiped -= HandleWipe;
            Plugin.ClientState.Login -= HandleLogin;
            Plugin.ClientState.Logout -= HandleLogout;
            Plugin.ClientState.TerritoryChanged -= HandlePlayerTerritoryChange;
            Plugin.EmoteReaderHooks.OnEmoteIncoming -= OnEmoteIncoming;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing -= OnEmoteOutgoing;
            Plugin.EmoteReaderHooks.OnEmoteSelf -= OnEmoteSelf;
            Plugin.EmoteReaderHooks.OnEmoteUnrelated -= OnEmoteUnrelated;
            running = false;
        }


        // Unused currently
        private void HandleNetworkMessage(nint dataPtr, ushort OpCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            //Plugin.PluginLog.Info($"(Net) dataPtr: {dataPtr} - OpCode: {OpCode} - ActorId: {sourceActorId} - TargetId: {targetActorId} - direction: ${direction.ToString()}");

            //if (MasterCharacter != null && MasterCharacter.IsValid() && MasterCharacter.Name + "#" + MasterCharacter.HomeWorld.Id == Plugin.Configuration.MasterNameFull) return;

            var targetOb = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.ObjectId == targetActorId);
            if (targetOb != null && targetOb.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
            {
                if (((PlayerCharacter)targetOb).Name + "#" + ((PlayerCharacter)targetOb).HomeWorld.Id == Plugin.Configuration.MasterNameFull)
                {
                    MasterCharacter = (PlayerCharacter)targetOb;
                    Plugin.PluginLog.Info("Found Master Signature!");
                    Plugin.PluginLog.Info(MasterCharacter.ToString());
                    Plugin.GameNetwork.NetworkMessage -= HandleNetworkMessage;
                    return;
                }
                //Plugin.PluginLog.Info(targetOb.ToString());
            }
        }

        public PlayerCharacter? scanForPlayerCharacter(string playerNameFull)
        {
            var f = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.ObjectKind == 1 && ((PlayerCharacter)x).Name + "#" + ((PlayerCharacter)x).HomeWorld.Id == playerNameFull); //player character
            if (f != null) return (PlayerCharacter)f;
            else return null;
        }
        public PlayerCharacter? scanForPlayerCharacter(uint ObjectId)
        {
            var f = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.ObjectKind == 1 && (ulong)x.ObjectId == ObjectId); //player character
            if (f != null) return (PlayerCharacter)f;
            else return null;
        }


        public void scanForMasterCharacter()
        {
            MasterCharacter = scanForPlayerCharacter(Plugin.Configuration.MasterNameFull);
            if (MasterCharacter != null)
            {
                if (lookingForMaster.Enabled)
                {
                    Plugin.PluginLog.Info("Found Master Signature!");
                    lookingForMaster.Stop();
                    LeashTimer.Start();
                }
            }
            else Plugin.PluginLog.Info("Could not find Signature... Retrying");
        }



        private void CheckLeashDistance()
        {
            //Plugin.PluginLog.Info("Checking Leash Distance");
            //Plugin.PluginLog.Info($"Master: {Plugin.Configuration.MasterNameFull} Sig: {MasterCharacter}");

            scanForMasterCharacter(); // Is Master still in Memory?
            if (MasterCharacter == null)
            {

                if (!lookingForMaster.Enabled)
                {
                    lookingForMaster.Elapsed += (sender, e) => scanForMasterCharacter();
                    lookingForMaster.Start();
                    LeashTimer.Stop();
                    Plugin.PluginLog.Info("Lost Master Signature!");
                    Plugin.PluginLog.Info("Starting Scanner...");
                    return;
                }
                return;
            }

            Plugin.PluginLog.Info($"{MasterCharacter.Name} - {MasterCharacter.Address} - {MasterCharacter.ObjectIndex}");
            Plugin.PluginLog.Info($"Valid: {MasterCharacter.IsValid()} Master Pos: {MasterCharacter.Position} Local Pos: {Plugin.ClientState.LocalPlayer.Position} diff: {MasterCharacter.Position - Plugin.ClientState.LocalPlayer.Position}");
        }


        public unsafe void HandleChatMessage(XivChatType type, uint senderId, ref SeString senderE, ref SeString message, ref bool isHandled)
        {
            if (Plugin.ClientState.LocalPlayer == null)
            {
                Plugin.PluginLog.Error("Wtf, LocalPlayer is null?");
                return;
            }

            //Plugin.PluginLog.Info(Plugin.ClientState.LocalPlayer.ToString());
            //Plugin.PluginLog.Info(Plugin.ClientState.LocalPlayer.Address.ToString());
            //Plugin.PluginLog.Info(Plugin.ClientState.LocalPlayer.NameId.ToString());

            //Plugin.PluginLog.Info("Payloads:");
            /*foreach (Payload p in senderE.Payloads)
            {
                if (p.Type == PayloadType.Player)
                {
                    Plugin.PluginLog.Info(p.ToString());

                    Plugin.PluginLog.Info(((PlayerPayload)p).PlayerName);
                    Plugin.PluginLog.Info(((PlayerPayload)p).DisplayedName);
                    Plugin.PluginLog.Info(((PlayerPayload)p).World.ToString());


                }
            }*/

            //Plugin.PluginLog.Info($"Current Length of GameObjectTable: {Plugin.ObjectTable.Length}");
            //Plugin.ClientState.LocalPlayer.

            //Plugin.PluginLog.Info($"(Chat) type: {type} - SenderId: {senderId} - Sender SE: {senderE} - Message: {message} - isHandled: ${isHandled}");
            if (message == null) return; //sanity check in case we get sent bad data

            string sender = senderE.ToString().ToLower();
            if (sender.Length != 0 && !Plugin.Configuration.shareCypher().Contains(sender.ToString()[0]))
            {
                //Plugin.PluginLog.Info($"Illegal character found on {sender} - removing...");
                sender = sender.Substring(1);
            }

#pragma warning disable CS8602 // no localplayer can NOT be null here, because if it is then our game isnt even working

            int[] dmTypes = { 2234, 2874, 4410, 2106, 4154 };
            if (Plugin.Configuration.DeathMode && dmTypes.Contains((int)type))
            {
                HandleDeathMode(type, message.TextValue);
                if (!Plugin.Configuration.IsPassthroughAllowed) return;
            }


            if (Plugin.Configuration.ShockOnBadWord && (int)type <= 107 && Plugin.ClientState.LocalPlayer.Name.ToString() == sender.ToString()) // its proooobably a social message
            {
                foreach (var (word, settings) in Plugin.Configuration.ShockBadWordSettings)
                {

                    if (message.ToString().ToLower().Contains(word.ToLower()))
                    {
                        Plugin.sendNotif($"You said the bad word: {word}!");
                        Plugin.WebClient.sendRequestShock(settings);
                        if (!Plugin.Configuration.IsPassthroughAllowed) return;
                    }
                }
                if (!Plugin.Configuration.IsPassthroughAllowed) return;
            }
#pragma warning restore CS8602

            if (Plugin.Configuration.ShockOnDeath && ((int)type == 2874 || (int)type == 2234) && message.TextValue.Contains("You are defeated", StringComparison.Ordinal)) //Death TODO add unique player identifier
            {
                Plugin.sendNotif($"You died!");
                Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockDeathSettings);
                if (!Plugin.Configuration.IsPassthroughAllowed) return;
            }

            if (Plugin.Configuration.ShockOnVuln && (int)type >= 800 && message.TextValue.Contains("You suffer the effect of ", StringComparison.Ordinal) && message.TextValue.Contains("Vulnerability Up.", StringComparison.Ordinal)) //Suffered Debuff
            {
                Plugin.sendNotif($"You took a Vulnerability Up debuff!");
                Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockVulnSettings);
                if (!Plugin.Configuration.IsPassthroughAllowed) return;
            }

            if (Plugin.Configuration.ShockOnDamage && (int)type >= 800 && message.TextValue.Contains("You take", StringComparison.Ordinal) && message.TextValue.Contains("damage.", StringComparison.Ordinal))// Damage Taken
            {
                Plugin.sendNotif($"You took damage!");
                Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockDamageSettings);
                if (!Plugin.Configuration.IsPassthroughAllowed) return;
            }

            /*if (Plugin.Configuration.ShockOnPat && type == XivChatType.StandardEmote && message.TextValue.Contains("gently pats you.", StringComparison.Ordinal))
            {
                if (!Plugin.Configuration.checkPermission(sender.ToString(), 0))
                {
                    Plugin.PluginLog.Info("Aborting call because target does not have enough permission");
                    return;
                }
                Plugin.sendNotif($"You got headpatted by {sender.ToString()}!");
                Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockPatSettings);
                return;
            }*/

            if (Plugin.Configuration.ShockOnDeathroll && ((int)type) == 2122)
            {
                if (message.TextValue.Contains("You roll a 1 (out of", StringComparison.Ordinal))
                {
                    Plugin.sendNotif($"You lost a Deathroll!");
                    Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockDeathrollSettings);
                }
                if (!Plugin.Configuration.IsPassthroughAllowed) return;
            }

            ChatType.ChatTypes? chatType = ChatType.GetChatTypeFromXivChatType(type);
            if (chatType == null)
            {
                return;
            }
            if (Plugin.Configuration.Channels.Contains(chatType.Value)) //If the channel can be selected and is activated by the user
            {
                List<Trigger> triggers = Plugin.Configuration.Triggers;
                foreach (Trigger trigger in triggers)
                {
                    Plugin.PluginLog.Information(message.TextValue);
                    if (trigger.Enabled && (trigger.Regex != null && trigger.Regex.IsMatch(message.TextValue)))
                    {
                        Plugin.PluginLog.Information($"Trigger {trigger.Name} triggered. Zap!");
                        Plugin.WebClient.sendRequestShock([trigger.Mode, trigger.Intensity, trigger.Duration]);
                        if (!Plugin.Configuration.IsPassthroughAllowed) return;
                    }
                }
            }
        }

        private void HandleWipe(object? e, ushort i)
        {
            if (Plugin.Configuration.ShockOnWipe)
            {
                Plugin.sendNotif($"Your party wiped!");
                Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockWipeSettings);
                if (!Plugin.Configuration.IsPassthroughAllowed) return;
            }
        }

        private void HandleLogin()
        {
            Plugin.onLogin();
            PlayerCharacter = Plugin.ClientState.LocalPlayer;
            Plugin.ClientState.Login -= HandleLogin;
            Plugin.ClientState.Logout += HandleLogout;
        }

        private void HandleLogout()
        {
            Plugin.onLogout();
            PlayerCharacter = null;
            Plugin.ClientState.Logout -= HandleLogout;
            Plugin.ClientState.Login += HandleLogin;
        }


        private void HandlePlayerTerritoryChange(ushort obj)
        {

        }


        private void OnEmoteIncoming(PlayerCharacter sourceObj, ushort emoteId)
        {
            Plugin.PluginLog.Info("[INCOMING EMOTE] Source: " + sourceObj.ToString() + " EmoteId: " + emoteId);
            if (!Plugin.Configuration.checkPermission(sourceObj.Name + "#" + sourceObj.HomeWorld.Id, 0))
            {
                Plugin.PluginLog.Info("Aborting call because target does not have enough permission");
                return;
            }

            if (Plugin.Configuration.ShockOnPat && emoteId == 105)
            {
                Plugin.sendNotif($"You got headpatted by {sourceObj.Name}!");
                Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockPatSettings);
            }

        }

        private void OnEmoteUnrelated(PlayerCharacter sourceObj, GameObject targetObj, ushort emoteId)
        {
            Plugin.PluginLog.Info("[Unrelated Emote] Source: " + sourceObj.ToString() + " Target:" + targetObj + " EmoteId: " + emoteId);

        }

        private void OnEmoteOutgoing(GameObject targetObj, ushort emoteId)
        {
            Plugin.PluginLog.Info("[OUTGOING EMOTE] Target: " + targetObj.ToString() + " EmoteId: " + emoteId);
            //Plugin.Configuration.MasterNameFull = ((PlayerCharacter)targetObj).Name + "#" + ((PlayerCharacter)targetObj).HomeWorld.Id;
            //Plugin.Configuration.Save();
        }

        private void OnEmoteSelf(ushort emoteId)
        {
            Plugin.PluginLog.Info("[SELF EMOTE] EmoteId: " + emoteId);
            //Plugin.Configuration.MasterNameFull = Plugin.ClientState.LocalPlayer.Name + "#" + Plugin.ClientState.LocalPlayer.HomeWorld.Id;
            //Plugin.Configuration.Save();

        }

        private void HandleDeathMode(XivChatType type, string message)
        {
            Plugin.PluginLog.Error(((int)type).ToString());
            Plugin.PluginLog.Error(message);
            int partysize, intensity, duration;
            int[] settings;
            switch (type)
            {
                case (XivChatType)4410: //death other
                    foreach ( PartyMember member in Plugin.PartyList)
                    {
                        if (message.Contains(member.Name.TextValue))
                        {
                            DeathModeCount++;
                            settings = Plugin.Configuration.DeathModeSettings;
                            partysize = Plugin.PartyList.Count;
                            intensity = settings[1] * DeathModeCount / partysize;
                            duration = settings[2] * DeathModeCount / partysize;
                            Plugin.PluginLog.Information($"Duration: {duration}, Intensity: {intensity}.");
                            Plugin.WebClient.sendRequestShock([0, intensity, duration]);
                            return;
                        }
                    }
                    break;
                case (XivChatType)2234:
                case (XivChatType)2874:  //death self
                    if (!message.Contains("You are defeated", StringComparison.Ordinal))
                    {
                        return;
                    }
                    DeathModeCount++;
                    settings = Plugin.Configuration.DeathModeSettings;
                    partysize = Plugin.PartyList.Count;
                    intensity = settings[1] * DeathModeCount / partysize;
                    duration = settings[2] * DeathModeCount / partysize;
                    Plugin.PluginLog.Information($"Duration: {duration}, Intensity: {intensity}.");
                    Plugin.WebClient.sendRequestShock([0, intensity, duration]);
                    return;
                case (XivChatType)4154: //revive other
                    foreach (PartyMember member in Plugin.PartyList)
                    {
                        if (message.Contains(member.Name.TextValue))
                        {
                            DeathModeCount--;
                            return;
                        }
                    }
                    break;
                case (XivChatType)2106: //revive self
                    DeathModeCount--;
                    return;
                default:
                    break;
            }
        }

    }
}
