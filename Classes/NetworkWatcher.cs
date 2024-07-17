using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.Network;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
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


        private IPlayerCharacter? LocalPlayer;
        private uint lastHP = 0;
        private uint lastMaxHP = 0;
        private uint lastMP = 0;
        private bool wasDead = true;
        private uint lastVulnAmount = 0;
        private uint lastDDownAmount = 0;
        private int lastStatusCheck = 0;
        private int lastPartyCheck = 0;
        private int lastCheckedIndex;
        private readonly bool[] deadIndexes = [false, false, false, false, false, false, false, false]; //how do i polyfill
        private int amountDead = 0;

        IPlayerCharacter? IPlayerCharacter;
        IPlayerCharacter? MasterCharacter;
        private Timer lookingForMaster = new Timer(new TimeSpan(0, 0, 5));
        private int masterLookDelay = 0;

        public bool isLeashed = false;
        public bool isLeashing = false;
        private Timer LeashTimer = new Timer(new TimeSpan(0, 0, 1));

        private int DeathModeCount = 0;

        readonly private string[] FirstPersonWords = ["i", "i'd", "i'll", "me", "my", "myself", "mine"];

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
            //LeashTimer.Start(); 650 // 305

            Plugin.Framework.Update += checkLocalPlayerState;

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

            Plugin.Framework.Update -= checkLocalPlayerState;

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

            Plugin.Framework.Update -= checkLocalPlayerState;

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
            Plugin.PluginLog.Info($"(Net) dataPtr: {dataPtr} - OpCode: {OpCode} - ActorId: {sourceActorId} - TargetId: {targetActorId} - direction: ${direction.ToString()}");

            //if (MasterCharacter != null && MasterCharacter.IsValid() && MasterCharacter.Name + "#" + MasterCharacter.HomeWorld.Id == Plugin.Configuration.MasterNameFull) return;

            /*var targetOb = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.GameObjectId == targetActorId);
            if (targetOb != null && targetOb.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
            {
                if (((IPlayerCharacter)targetOb).Name + "#" + ((IPlayerCharacter)targetOb).HomeWorld.Id == Plugin.Configuration.MasterNameFull)
                {
                    MasterCharacter = (IPlayerCharacter)targetOb;
                    Plugin.PluginLog.Info("Found Master Signature!");
                    Plugin.PluginLog.Info(MasterCharacter.ToString());
                    Plugin.GameNetwork.NetworkMessage -= HandleNetworkMessage;
                    return;
                }
                //Plugin.PluginLog.Info(targetOb.ToString());
            }*/
        }

        public IPlayerCharacter? scanForPlayerCharacter(string playerNameFull)
        {
            var f = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.ObjectKind == 1 && ((IPlayerCharacter)x).Name + "#" + ((IPlayerCharacter)x).HomeWorld.Id == playerNameFull); //player character
            if (f != null) return (IPlayerCharacter)f;
            else return null;
        }
        public IPlayerCharacter? scanForPlayerCharacter(uint GameObjectId)
        {
            var f = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.ObjectKind == 1 && x.GameObjectId == GameObjectId); //player character
            if (f != null) return (IPlayerCharacter)f;
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


        private void checkLocalPlayerState(IFramework Framework)
        {
            if (LocalPlayer == null)
            {
                LocalPlayer = Plugin.ClientState.LocalPlayer;
                lastHP = LocalPlayer.CurrentHp;
                lastMaxHP = LocalPlayer.MaxHp;
                lastMP = LocalPlayer.CurrentMp;
            }

            if (lastHP != LocalPlayer.CurrentHp) HandleHPChange(); //check maxhp due to synching and such
            if (lastMP != LocalPlayer.CurrentMp) HandleMPChange();

            if (lastStatusCheck >= 60 && Plugin.Configuration.ShockOnVuln)
            {
                lastStatusCheck = 0;
                bool foundVuln = false;
                bool foundDDown = false;
                if (LocalPlayer.StatusList != null)
                {
                    foreach (var status in LocalPlayer.StatusList)
                    {
                        //Yes. We have to check for the IconId. The StatusId is different for different expansions, while the Name is different through languages.
                        if (status.GameData.Icon >= 17101 && status.GameData.Icon <= 17116) // Vuln Up
                        {
                            foundVuln = true;
                            var amount = status.StackCount;

                            Plugin.PluginLog.Verbose("Found Vuln Up - Amount: " + amount + " lastVulnCount: " + lastVulnAmount);
                            if (amount > lastVulnAmount)
                            {
                                Plugin.sendNotif($"You failed a Mechanic!");
                                Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockVulnSettings);
                                if (!Plugin.Configuration.IsPassthroughAllowed)
                                {
                                    lastVulnAmount = amount;
                                    return;
                                }
                            }
                            lastVulnAmount = amount;
                        }
                        if (status.GameData.Icon >= 18441 && status.GameData.Icon <= 18456) // Damage Down
                        {
                            foundDDown = true;
                            var amount = status.StackCount;
                            if (amount > lastDDownAmount)
                            {
                                Plugin.sendNotif($"You failed a Mechanic!");
                                Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockVulnSettings);
                                if (!Plugin.Configuration.IsPassthroughAllowed)
                                {
                                    lastDDownAmount = amount;
                                    return;
                                }
                            }
                            lastDDownAmount = amount;
                        }
                    }
                }
                if (!foundVuln) lastVulnAmount = 0;
                if (!foundDDown) lastDDownAmount = 0;
            } //Shock On Vuln / Damage Down

            if (Plugin.Configuration.DeathMode && Plugin.PartyList.Length > 0 && lastPartyCheck >= 60) // DeathMode
            {
                if (lastCheckedIndex >= Plugin.PartyList.Length) lastCheckedIndex = 0;
                if (Plugin.PartyList[lastCheckedIndex].ObjectId > 0 && Plugin.PartyList[lastCheckedIndex].CurrentHP == 0 && !deadIndexes[lastCheckedIndex])
                {
                    deadIndexes[lastCheckedIndex] = true;
                    amountDead++;
                    Plugin.PluginLog.Information($"(Deathmode) - Player died - {amountDead}/{Plugin.PartyList.Length} Members are dead.");
                    Plugin.WebClient.sendRequestShock([
                        Plugin.Configuration.DeathModeSettings[0],
                        Plugin.Configuration.DeathModeSettings[1] / amountDead / Plugin.PartyList.Length,
                        Plugin.Configuration.DeathModeSettings[2] / amountDead / Plugin.PartyList.Length]);
                }
                else if (Plugin.PartyList[lastCheckedIndex].ObjectId > 0 && Plugin.PartyList[lastCheckedIndex].CurrentHP > 0 && deadIndexes[lastCheckedIndex])
                {
                    deadIndexes[lastCheckedIndex] = false;
                    amountDead--;
                    Plugin.PluginLog.Information($"(Deathmode) - Player revived - {amountDead}/{Plugin.PartyList.Length} Members are dead.");
                }
                lastCheckedIndex++;
                lastPartyCheck = 0;
            }

            lastHP = LocalPlayer.CurrentHp;
            lastStatusCheck++;
            lastPartyCheck++;
        }

        private void HandleHPChange()
        {
            //Plugin.PluginLog.Verbose("HP Changed from " + lastHP + "/" + lastMaxHP + " to " + LocalPlayer.CurrentHp + "/" + LocalPlayer.MaxHp);
            if (lastMaxHP != LocalPlayer.MaxHp)
            {
                lastMaxHP = LocalPlayer.MaxHp;
                return;
            }
            if (Plugin.Configuration.ShockOnDeath && LocalPlayer.CurrentHp == 0 && !wasDead)
            {
                Plugin.sendNotif($"You Died!");
                Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockDeathSettings);
                wasDead = false;
                if (!Plugin.Configuration.IsPassthroughAllowed) return;
            }
            if (lastHP < LocalPlayer.CurrentHp && Plugin.Configuration.ShockOnDamage)
            {
                //Plugin.sendNotif($"You took Damage!"); // possibly remove this
                Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockDamageSettings);
            }
            if (lastHP > 0) wasDead = false;
        }
        private void HandleMPChange()
        {
            // Currently Unused
        }
        private void HandleStatusChange()
        {
            //64 = vuln up
            Plugin.PluginLog.Verbose("StatusList Changed");
            Plugin.PluginLog.Verbose(LocalPlayer.StatusList.ToString());
        }

        public unsafe void HandleChatMessage(XivChatType type, int timespamp, ref SeString senderE, ref SeString message, ref bool isHandled)
        {
            if (Plugin.ClientState.LocalPlayer == null)
            {
                Plugin.PluginLog.Error("Wtf, LocalPlayer is null?");
                return;
            }

            //Plugin.PluginLog.Info($"(Chat) type: {type} - Sender SE: {senderE} - Message: {message} - isHandled: ${isHandled}");
            if (message == null) return; //sanity check in case we get sent bad data

            string sender = senderE.ToString().ToLower();
            if (sender.Length != 0 && !Plugin.Configuration.shareCypher().Contains(sender.ToString()[0]))
            {
                //Plugin.PluginLog.Info($"Illegal character found on {sender} - removing...");
                sender = sender.Substring(1);
            }


#pragma warning disable CS8602 // no localplayer can NOT be null here, because if it is then our game isnt even working

            int[] dmTypes = { 2234, 2874, 4410, 2106, 4154 };
            /*if (Plugin.Configuration.DeathMode && dmTypes.Contains((int)type))
            {
                HandleDeathMode(type, message.TextValue);
                if (!Plugin.Configuration.IsPassthroughAllowed) return;
            }*/


            if ((int)type <= 107 && Plugin.ClientState.LocalPlayer.Name.ToString().ToLower() == sender.ToString().ToLower()) // its proooobably a social message
            {

                if (Plugin.Configuration.ShockOnBadWord)
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
                }

                //slightly different logic
                if (Plugin.Configuration.ShockOnFirstPerson)
                {
                    foreach (var word in message.ToString().Split(' '))
                    {
                        string sanWord = word.ToLower();
                        sanWord = sanWord.Replace(".", "");
                        sanWord = sanWord.Replace(",", "");
                        sanWord = sanWord.Replace("!", "");
                        sanWord = sanWord.Replace("?", "");
                        sanWord = sanWord.Replace("\"", "");
                        sanWord = sanWord.Replace("\'", "");
                        if (FirstPersonWords.Contains(sanWord))
                        {
                            Plugin.sendNotif($"You referred to yourself wrongly!");
                            Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockFirstPersonSettings);
                            if (!Plugin.Configuration.IsPassthroughAllowed) return;
                        }
                    }
                }
            }
#pragma warning restore CS8602

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

            if (Plugin.Configuration.ShockOnDeathroll && (int)type == 2122)
            {
                if (message.TextValue.Contains("You roll a 1 (out of", StringComparison.Ordinal))
                {
                    Plugin.sendNotif($"You lost a Deathroll!");
                    Plugin.WebClient.sendRequestShock(Plugin.Configuration.ShockDeathrollSettings);
                }
                if (!Plugin.Configuration.IsPassthroughAllowed) return;
            }

            ChatTypes? chatType = GetChatTypeFromXivChatType(type);
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
                    if (trigger.Enabled && trigger.Regex != null && trigger.Regex.IsMatch(message.TextValue))
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
            IPlayerCharacter = Plugin.ClientState.LocalPlayer;
            Plugin.ClientState.Login -= HandleLogin;
            Plugin.ClientState.Logout += HandleLogout;
        }

        private void HandleLogout()
        {
            Plugin.onLogout();
            IPlayerCharacter = null;
            Plugin.ClientState.Logout -= HandleLogout;
            Plugin.ClientState.Login += HandleLogin;
        }


        private void HandlePlayerTerritoryChange(ushort obj)
        {
            // Currently Unused
        }


        private void OnEmoteIncoming(IPlayerCharacter sourceObj, ushort emoteId)
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

        private void OnEmoteUnrelated(IPlayerCharacter sourceObj, IGameObject targetObj, ushort emoteId)
        {
            Plugin.PluginLog.Info("[Unrelated Emote] Source: " + sourceObj.ToString() + " Target:" + targetObj + " EmoteId: " + emoteId);

        }

        private void OnEmoteOutgoing(IGameObject targetObj, ushort emoteId)
        {
            Plugin.PluginLog.Info("[OUTGOING EMOTE] Target: " + targetObj.ToString() + " EmoteId: " + emoteId);
            //Plugin.Configuration.MasterNameFull = ((IPlayerCharacter)targetObj).Name + "#" + ((IPlayerCharacter)targetObj).HomeWorld.Id;
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
                    foreach (IPartyMember member in Plugin.PartyList)
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
                    foreach (IPartyMember member in Plugin.PartyList)
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
