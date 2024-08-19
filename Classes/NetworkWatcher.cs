using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
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
        Preset ActivePreset;


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
            Plugin.ClientState.Logout += HandleLogout;
        }

        public void Start() //Todo only start specific services, when respective trigger is on
        {
            running = true;
            ActivePreset = Plugin.Configuration.ActivePreset;
            LeashTimer.Elapsed += (sender, e) => CheckLeashDistance();
            //LeashTimer.Start(); 650 // 305

            Plugin.Framework.Update += checkLocalPlayerState;

            Plugin.ChatGui.ChatMessage += HandleChatMessage;
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
            Plugin.ClientState.Login -= HandleLogin;
            Plugin.ClientState.Logout -= HandleLogout;
            Plugin.ClientState.TerritoryChanged -= HandlePlayerTerritoryChange;
            Plugin.EmoteReaderHooks.OnEmoteIncoming -= OnEmoteIncoming;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing -= OnEmoteOutgoing;
            Plugin.EmoteReaderHooks.OnEmoteSelf -= OnEmoteSelf;
            Plugin.EmoteReaderHooks.OnEmoteUnrelated -= OnEmoteUnrelated;
            running = false;
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
            //MasterCharacter = scanForPlayerCharacter(Plugin.Authentification.);
            if (MasterCharacter != null)
            {
                if (lookingForMaster.Enabled)
                {
                    Plugin.Log("Found Master Signature!", true);
                    lookingForMaster.Stop();
                    LeashTimer.Start();
                }
            }
            else Plugin.Log("Could not find Signature... Retrying", true);
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
                    Plugin.Log("Lost Master Signature!",true);
                    Plugin.Log("Starting Scanner...",true);
                    return;
                }
                return;
            }

            Plugin.Log($"{MasterCharacter.Name} - {MasterCharacter.Address} - {MasterCharacter.ObjectIndex}",true);
            Plugin.Log($"Valid: {MasterCharacter.IsValid()} Master Pos: {MasterCharacter.Position} Local Pos: {Plugin.ClientState.LocalPlayer.Position} diff: {MasterCharacter.Position - Plugin.ClientState.LocalPlayer.Position}",true);
        }


        private void checkLocalPlayerState(IFramework Framework)
        {
            try
            {
                if (LocalPlayer == null)
                {
                    LocalPlayer = Plugin.ClientState.LocalPlayer;
                    lastHP = LocalPlayer.CurrentHp;
                    lastMaxHP = LocalPlayer.MaxHp;
                    lastMP = LocalPlayer.CurrentMp;
                }



                if (lastHP != LocalPlayer.CurrentHp)
                {
                    if (lastMaxHP != LocalPlayer.MaxHp)
                    {
                        lastHP = LocalPlayer.CurrentHp;
                        lastMaxHP = LocalPlayer.MaxHp;
                    }
                    if (ActivePreset.Die.IsEnabled() && LocalPlayer.CurrentHp == 0 && !wasDead)
                    {
                        Plugin.sendNotif($"You Died!");
                        Plugin.WebClient.sendPishockRequest(ActivePreset.Die);
                        wasDead = false;
                    }

                    if (lastHP > LocalPlayer.CurrentHp && ActivePreset.TakeDamage.IsEnabled())
                    {
                        int amount = (int)lastHP - (int)LocalPlayer.CurrentHp;
                        int amountPercent = (int)((double)amount / lastMaxHP * 100);
                        //Plugin.PluginLog.Verbose($"Cur: {LocalPlayer.CurrentHp} Last: {lastHP} diff: {amount}|{amountPercent}%");
                        if (ActivePreset.TakeDamage.CustomData == null) ActivePreset.TakeDamage.setupCustomData(); //failsafe
                        if (ActivePreset.TakeDamage.CustomData["Proportional"][0] == 1)
                        {
                            int calcdIntensity = (int)((double)ActivePreset.TakeDamage.Intensity * ((double)amountPercent / ActivePreset.TakeDamage.CustomData["Proportional"][1]));
                            int calcdDuration = (int)((double)ActivePreset.TakeDamage.Duration * ((double)amountPercent / ActivePreset.TakeDamage.CustomData["Proportional"][1]));
                            Plugin.WebClient.sendPishockRequest(ActivePreset.TakeDamage, [(int)ActivePreset.TakeDamage.OpMode, calcdIntensity, calcdDuration]);
                        }
                        else Plugin.WebClient.sendPishockRequest(ActivePreset.TakeDamage);
                    }
                    if (lastHP > 0) wasDead = false;
                }
                lastHP = LocalPlayer.CurrentHp;
                if (lastMP != LocalPlayer.CurrentMp) HandleMPChange();

                if (lastStatusCheck >= 60 && ActivePreset.FailMechanic.IsEnabled())
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

                                Plugin.Log("Found Vuln Up - Amount: " + amount + " lastVulnCount: " + lastVulnAmount);
                                if (amount > lastVulnAmount)
                                {
                                    Plugin.sendNotif($"You failed a Mechanic!");
                                    if (ActivePreset.FailMechanic.CustomData == null) ActivePreset.FailMechanic.setupCustomData(); //failsafe
                                    if (ActivePreset.FailMechanic.CustomData["Proportional"][0] == 1)
                                    {
                                        int calcdIntensity = ActivePreset.FailMechanic.Intensity * (amount / ActivePreset.FailMechanic.CustomData["Proportional"][1]);
                                        int calcdDuration = ActivePreset.FailMechanic.Duration * (amount / ActivePreset.FailMechanic.CustomData["Proportional"][1]);
                                        Plugin.WebClient.sendPishockRequest(ActivePreset.FailMechanic, [(int)ActivePreset.FailMechanic.OpMode, calcdIntensity, calcdDuration]);
                                    }
                                    else Plugin.WebClient.sendPishockRequest(ActivePreset.FailMechanic);
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
                                    Plugin.WebClient.sendPishockRequest(ActivePreset.FailMechanic);
                                }
                                lastDDownAmount = amount;
                            }
                        }
                    }
                    if (!foundVuln) lastVulnAmount = 0;
                    if (!foundDDown) lastDDownAmount = 0;
                } //Shock On Vuln / Damage Down

                if (lastPartyCheck >= 60 && ActivePreset.PartymemberDies.IsEnabled() && Plugin.PartyList.Length > 0) // DeathMode
                {
                    if (lastCheckedIndex >= Plugin.PartyList.Length) lastCheckedIndex = 0;
                    if (Plugin.PartyList[lastCheckedIndex].ObjectId > 0 && Plugin.PartyList[lastCheckedIndex].CurrentHP == 0 && !deadIndexes[lastCheckedIndex])
                    {
                        deadIndexes[lastCheckedIndex] = true;
                        amountDead++;
                        Plugin.Log($"(Deathmode) - Player died - {amountDead}/{Plugin.PartyList.Length} members are dead.");
                        Plugin.WebClient.sendPishockRequest(ActivePreset.PartymemberDies, [ActivePreset.PartymemberDies.Intensity * (amountDead / Plugin.PartyList.Length), ActivePreset.PartymemberDies.Duration * (amountDead / Plugin.PartyList.Length)]);
                    }
                    else if (Plugin.PartyList[lastCheckedIndex].ObjectId > 0 && Plugin.PartyList[lastCheckedIndex].CurrentHP > 0 && deadIndexes[lastCheckedIndex])
                    {
                        deadIndexes[lastCheckedIndex] = false;
                        amountDead--;
                        Plugin.Log($"(Deathmode) - Player revived - {amountDead}/{Plugin.PartyList.Length} members are dead.");
                    }
                    lastCheckedIndex++;
                    lastPartyCheck = 0;
                }


                lastStatusCheck++;
                lastPartyCheck++;
            }
            catch (Exception e)
            {
                Plugin.Error(e.ToString());
            }
        }

        private void HandleHPChange()
        {
            if (lastMaxHP != LocalPlayer.MaxHp)
            {
                lastMaxHP = LocalPlayer.MaxHp;
                return;
            }
            if (ActivePreset.Die.IsEnabled() && LocalPlayer.CurrentHp == 0 && !wasDead)
            {
                Plugin.sendNotif($"You Died!");
                Plugin.WebClient.sendPishockRequest(ActivePreset.Die);
                wasDead = false;
            }
            if (lastHP < LocalPlayer.CurrentHp && ActivePreset.TakeDamage.IsEnabled())
            {
                uint amount = LocalPlayer.CurrentHp - lastHP;
                Plugin.Log($"Cur: {LocalPlayer.CurrentHp} Last: {lastHP} diff: {LocalPlayer.CurrentHp - lastHP}",true);
                if (ActivePreset.TakeDamage.CustomData == null) ActivePreset.TakeDamage.setupCustomData(); //failsafe
                if (ActivePreset.TakeDamage.CustomData["Proportional"][0] == 1)
                {
                    //int calcdIntensity = ActivePreset.TakeDamage.Intensity * (amount / ActivePreset.TakeDamage.CustomData["Proportional"][1]);
                    //int calcdDuration = ActivePreset.TakeDamage.Duration * (amount / ActivePreset.TakeDamage.CustomData["Proportional"][1]);
                    //Plugin.WebClient.sendPishockRequest(ActivePreset.TakeDamage, [(int)ActivePreset.TakeDamage.OpMode, calcdIntensity, calcdDuration]);
                }
                Plugin.WebClient.sendPishockRequest(ActivePreset.TakeDamage);
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
            Plugin.Log("StatusList Changed",true);
            Plugin.Log(LocalPlayer.StatusList.ToString(),true);
        }

        public unsafe void HandleChatMessage(XivChatType type, int timespamp, ref SeString senderE, ref SeString message, ref bool isHandled)
        {
            if (Plugin.ClientState.LocalPlayer == null)
            {
                Plugin.Error("Wtf, LocalPlayer is null?",true);
                return;
            }
            if (message == null) return; //sanity check in case we get sent bad data

            string sender = senderE.ToString().ToLower();

            if ((int)type <= 107 && Plugin.ClientState.LocalPlayer.Name.ToString().ToLower() == sender) // its proooobably a social message
            {

                if (ActivePreset.SayBadWord.IsEnabled())
                {
                    foreach (var (word, settings) in ActivePreset.SayBadWord.CustomData)
                    {

                        if (message.ToString().ToLower().Contains(word.ToLower()))
                        {
                            Plugin.sendNotif($"You said the bad word: {word}!");
                            Plugin.WebClient.sendPishockRequest(ActivePreset.SayBadWord, settings);
                        }
                    }
                }

                //slightly different logic
                if (ActivePreset.SayFirstPerson.IsEnabled())
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
                            Plugin.WebClient.sendPishockRequest(ActivePreset.SayFirstPerson);
                        }
                    }
                }
            }


            ChatTypes? chatType = GetChatTypeFromXivChatType(type);
            if (chatType == null)
            {
                return;
            }
            if (ActivePreset.Channels.Contains(chatType.Value)) //If the channel can be selected and is activated by the user
            {
                List<RegexTrigger> triggers = ActivePreset.CustomMessageTriggers;
                foreach (RegexTrigger trigger in triggers)
                {
                    Plugin.Log(message.TextValue,true);
                    if (trigger.Enabled && trigger.Regex != null && trigger.Regex.IsMatch(message.TextValue))
                    {
                        Plugin.Log($"Trigger {trigger.Name} triggered. Zap!");
                        //Plugin.WebClient.sendPishockRequest(ActivePreset.) todo rework
                    }
                }
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

        private void TargetSomething()
        {

        }


        private void OnEmoteIncoming(IPlayerCharacter sourceObj, ushort emoteId)
        {
            //Plugin.PluginLog.Info("[INCOMING EMOTE] Source: " + sourceObj.ToString() + " EmoteId: " + emoteId);

            if (ActivePreset.GetPat.IsEnabled() && emoteId == 105)
            {
                Plugin.sendNotif($"You got headpatted by {sourceObj.Name}!");
                Plugin.WebClient.sendPishockRequest(ActivePreset.GetPat);
            }

        }

        private void OnEmoteUnrelated(IPlayerCharacter sourceObj, IGameObject targetObj, ushort emoteId)
        {
            //Plugin.PluginLog.Info("[Unrelated Emote] Source: " + sourceObj.ToString() + " Target:" + targetObj + " EmoteId: " + emoteId);
            // Currently Unused
        }

        private void OnEmoteOutgoing(IGameObject targetObj, ushort emoteId)
        {
            //Plugin.PluginLog.Info("[OUTGOING EMOTE] Target: " + targetObj.ToString() + " EmoteId: " + emoteId);
            // Currently Unused.
        }

        private void OnEmoteSelf(ushort emoteId)
        {
            //Plugin.PluginLog.Info("[SELF EMOTE] EmoteId: " + emoteId);
            // Currently Unused.
        }


        /* Unused Debug stuff
        private void HandleNetworkMessage(nint dataPtr, ushort OpCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            Plugin.PluginLog.Info($"(Net) dataPtr: {dataPtr} - OpCode: {OpCode} - ActorId: {sourceActorId} - TargetId: {targetActorId} - direction: ${direction.ToString()}");

            //if (MasterCharacter != null && MasterCharacter.IsValid() && MasterCharacter.Name + "#" + MasterCharacter.HomeWorld.Id == Plugin.Configuration.MasterNameFull) return;

            var targetOb = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.GameObjectId == targetActorId);
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
            }
        }*/

    }
}
