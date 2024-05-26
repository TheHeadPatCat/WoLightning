using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Network;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;

namespace WoLightning
{
    public class NetworkWatcher
    {
        //bool cOnPat,cOnDamage,cOnRescue,cOnRandom = false;   //unused - maybe remove?
        public bool running = false;
        Plugin Plugin;
        PlayerCharacter PlayerCharacter;
        public NetworkWatcher(Plugin plugin)
        {
            Plugin = plugin;
            
            /*
            cOnPat = plugin.Configuration.ShockOnPat;
            cOnDamage = plugin.Configuration.ShockOnDamage;
            cOnRescue = plugin.Configuration.ShockOnRescue;
            cOnRandom = plugin.Configuration.ShockOnRandom;
            */
            

        }

        public void Start()
        {
            running = true;
            //Plugin.GameNetwork.NetworkMessage += HandleNetworkMessage;
            Plugin.ChatGui.ChatMessage += HandleChatMessage;
            if(Plugin.Configuration.ShockOnWipe)Plugin.DutyState.DutyWiped += HandleWipe;
            Plugin.ClientState.Login += HandleLogin;
            Plugin.ClientState.TerritoryChanged += HandlePlayerTerritoryChange;
        }

       public void Dispose()
        {
            //Plugin.GameNetwork.NetworkMessage -= HandleNetworkMessage;
            Plugin.ChatGui.ChatMessage -= HandleChatMessage;
            Plugin.DutyState.DutyWiped -= HandleWipe;
            Plugin.ClientState.Login -= HandleLogin;
            Plugin.ClientState.TerritoryChanged -= HandlePlayerTerritoryChange;
            running = false;
        }

        private unsafe void HandleNetworkMessage(nint dataPtr, ushort OpCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            Plugin.PluginLog.Info($"(Net) dataPtr: {dataPtr} - OpCode: {OpCode} - ActorId: {sourceActorId} - TargetId: {targetActorId} - direction: ${direction.ToString()}");
        }


        private void HandleChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            Plugin.PluginLog.Info($"(Chat) type: {type} - SenderId: {senderId} - Sender SE: {sender} - Message: {message} - isHandled: ${isHandled}");
            if (message == null) return; //sanity check in case we get sent bad data

            if (Plugin.Configuration.ShockOnBadWord && Plugin.ClientState.LocalPlayer.Name.ToString() == sender.ToString()  && (int)type <= 70) // its proooobably a social message
            {
                foreach ( var (word, settings) in Plugin.Configuration.ShockBadWordSettings)
                {

                    if (message.ToString().ToLower().Contains(word.ToLower()))
                    {
                        Plugin.PluginLog.Info($"Found bad word: {word}");
                        Plugin.WebClient.sendRequest(settings);
                        return;
                    }
                }
                return;
            }

            if (Plugin.Configuration.ShockOnDamage && (int)type >= 800 && message.TextValue.Contains("You take") && message.TextValue.Contains("damage."))// Damage Taken
            {
                Plugin.PluginLog.Info("Damage Taken");
                Plugin.WebClient.sendRequest(Plugin.Configuration.ShockDamageSettings);
                return;
            }

            if(Plugin.Configuration.ShockOnVuln && (int)type >= 800 && message.TextValue.Contains("You suffer the effect of ") && message.TextValue.Contains("Vulnerability Up.")) //Suffered Debuff
            {
                Plugin.PluginLog.Info("Vulnerability Up debuff taken");
                Plugin.WebClient.sendRequest(Plugin.Configuration.ShockVulnSettings);
                return;
            }

            if (Plugin.Configuration.ShockOnPat && type == XivChatType.StandardEmote && message.TextValue.Contains("gently pats you."))
            {
                Plugin.PluginLog.Info("Headpatted");
                Plugin.WebClient.sendRequest(Plugin.Configuration.ShockPatSettings);
                return;
            }

            if (Plugin.Configuration.ShockOnDeathroll && ((int)type) == 2122) // check for /random command
            {
                if (message.TextValue.Contains("You roll a 1"))
                {
                    Plugin.PluginLog.Info("Deathroll lost");
                    Plugin.WebClient.sendRequest(Plugin.Configuration.ShockDeathrollSettings);
                }
                return;
            }

            if (Plugin.Configuration.ShockOnDeath && (int)type == 2874) //Death
            {
                Plugin.PluginLog.Info("Player Died");
                Plugin.WebClient.sendRequest(Plugin.Configuration.ShockDeathSettings);
                return;
            }
        }

        private void HandleWipe(object? e, ushort i)
        {
            if (Plugin.Configuration.ShockOnWipe)
            {
                Plugin.PluginLog.Info("Team Wiped");
                Plugin.WebClient.sendRequest(Plugin.Configuration.ShockWipeSettings);
                return;
            }
        }

        private void HandleLogin()
        {
            if(Plugin.ClientState.LocalPlayer == null)
            {
                Plugin.PluginLog.Error("Wtf, LocalPlayer is null?");
                return;
            }
            PlayerCharacter = Plugin.ClientState.LocalPlayer;
        }

        private void HandlePlayerTerritoryChange(ushort obj)
        {
            PlayerCharacter = Plugin.ClientState.LocalPlayer;
        }

    }
}
