using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using System;
using System.Linq;


namespace WoLightning
{
    public class EmoteReaderHooks : IDisposable
    {
        private Plugin Plugin;
        public Action<PlayerCharacter, GameObject, ushort> OnEmoteUnrelated;
        public Action<PlayerCharacter, ushort> OnEmoteIncoming;
        public Action<GameObject, ushort> OnEmoteOutgoing;
        public Action<ushort> OnEmoteSelf;

        public delegate void OnEmoteFuncDelegate(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2);
        private readonly Hook<OnEmoteFuncDelegate> hookEmote;

        public bool IsValid = false;

        public EmoteReaderHooks(Plugin plugin)
        {
            Plugin = plugin;
            try
            {
                hookEmote = Plugin.GameInteropProvider.HookFromSignature<OnEmoteFuncDelegate>("48 89 5c 24 08 48 89 6c 24 10 48 89 74 24 18 48 89 7c 24 20 41 56 48 83 ec 30 4c 8b 74 24 60 48 8b d9 48 81 c1 80 2f 00 00", OnEmoteDetour);
                hookEmote.Enable();
                Plugin.PluginLog.Info("Started EmoteReaderHook!");
                IsValid = true;
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error(ex + "");
            }
        }

        public void Dispose()
        {
            hookEmote?.Dispose();
            IsValid = false;
        }

        void OnEmoteDetour(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2)
        {

            try
            {
                if (Plugin.ClientState.LocalPlayer != null)
                {
                    if (targetId == Plugin.ClientState.LocalPlayer.ObjectId) // we are the target
                    {
                        var instigatorOb = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.Address == instigatorAddr);
                        if (instigatorOb != null) OnEmoteIncoming?.Invoke((PlayerCharacter)instigatorOb, emoteId); // someone is sending a emote targeting us
                    }
                    else // We are not the target
                    {
                        var targetOb = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.ObjectId == targetId);
                        var instigatorOb = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.Address == instigatorAddr);
                        if (instigatorOb == null || targetOb == null) //bad data
                        {
                            hookEmote.Original(unk, instigatorAddr, emoteId, targetId, unk2);
                            return;
                        }
                        if (targetOb.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player) // No Target can be found
                        {
                            if (instigatorOb.ObjectId == Plugin.ClientState.LocalPlayer.ObjectId) OnEmoteSelf?.Invoke(emoteId); // we are sending an emote without target
                            else OnEmoteUnrelated?.Invoke((PlayerCharacter)instigatorOb, targetOb, emoteId); // seomeone is sending a emote without target
                        }
                        else
                        {
                            if (instigatorOb.ObjectId == Plugin.ClientState.LocalPlayer.ObjectId) OnEmoteOutgoing?.Invoke(targetOb, emoteId); // we are sending an emote
                            else OnEmoteUnrelated?.Invoke((PlayerCharacter)instigatorOb, targetOb, emoteId); // someone is sending a emote to someone else
                        }

                    }
                }
            }
            catch (Exception ex) { Plugin.PluginLog.Error(ex.ToString()); }

            hookEmote.Original(unk, instigatorAddr, emoteId, targetId, unk2);
        }
    }
}