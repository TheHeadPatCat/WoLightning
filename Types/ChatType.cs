using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoLightning.Types
{
    public static class ChatType
    {
        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
        sealed class EnumOrderAttribute : Attribute
        {
            public int Order { get; }
            public EnumOrderAttribute(int order)
            {
                Order = order;
            }
        }

        public enum ChatTypes
        {
            [EnumOrder(0)]
            Tell_In = 0,

            [EnumOrder(1)]
            Tell = 17,

            [EnumOrder(2)]
            Say = 1,

            [EnumOrder(3)]
            Party = 2,

            [EnumOrder(4)]
            Alliance = 3,

            [EnumOrder(5)]
            Yell = 4,

            [EnumOrder(6)]
            Shout = 5,

            [EnumOrder(7)]
            FreeCompany = 6,

            [EnumOrder(8)]
            NoviceNetwork = 8,

            [EnumOrder(9)]
            CWL1 = 9,

            [EnumOrder(10)]
            CWL2 = 10,

            [EnumOrder(11)]
            CWL3 = 11,

            [EnumOrder(12)]
            CWL4 = 12,

            [EnumOrder(13)]
            CWL5 = 13,

            [EnumOrder(14)]
            CWL6 = 14,

            [EnumOrder(15)]
            CWL7 = 15,

            [EnumOrder(16)]
            CWL8 = 16,

            [EnumOrder(17)]
            LS1 = 19,

            [EnumOrder(18)]
            LS2 = 20,

            [EnumOrder(19)]
            LS3 = 21,

            [EnumOrder(20)]
            LS4 = 22,

            [EnumOrder(21)]
            LS5 = 23,

            [EnumOrder(22)]
            LS6 = 24,

            [EnumOrder(23)]
            LS7 = 25,
            [EnumOrder(24)]
            LS8 = 26,
            [EnumOrder(-1)]
            DeathOther = 4410,
            [EnumOrder(-2)]
            ReviveOther = 4154,
            [EnumOrder(-3)]
            DeathSelf = 2234,
            [EnumOrder(-4)]
            ReviveSelf = 2106
        }

        public static IEnumerable<ChatTypes> GetOrderedChannels()
        {
            return Enum.GetValues(typeof(ChatTypes))
            .Cast<ChatTypes>()
            .Where(e => e != ChatTypes.Tell_In && e != ChatTypes.NoviceNetwork)
            .Where(e => GetOrder(e) >= 0)
                    .OrderBy(e => GetOrder(e));
        }

        private static int GetOrder(ChatTypes channel)
        {
            // get the attribute of the channel
            var attribute = channel.GetType()
                .GetField(channel.ToString())
                ?.GetCustomAttributes(typeof(EnumOrderAttribute), false)
                .FirstOrDefault() as EnumOrderAttribute;
            // return the order of the channel, or if it doesnt have one, return the max value
            return attribute?.Order ?? int.MaxValue;
        }

        public static ChatTypes? GetChatTypeFromXivChatType(XivChatType type)
        {
            return type switch
            {
                XivChatType.TellIncoming => ChatTypes.Tell,
                XivChatType.TellOutgoing => ChatTypes.Tell,
                XivChatType.Say => ChatTypes.Say,
                XivChatType.Party => ChatTypes.Party,
                XivChatType.Alliance => ChatTypes.Alliance,
                XivChatType.Yell => ChatTypes.Yell,
                XivChatType.Shout => ChatTypes.Shout,
                XivChatType.FreeCompany => ChatTypes.FreeCompany,
                XivChatType.NoviceNetwork => ChatTypes.NoviceNetwork,
                XivChatType.Ls1 => ChatTypes.LS1,
                XivChatType.Ls2 => ChatTypes.LS2,
                XivChatType.Ls3 => ChatTypes.LS3,
                XivChatType.Ls4 => ChatTypes.LS4,
                XivChatType.Ls5 => ChatTypes.LS5,
                XivChatType.Ls6 => ChatTypes.LS6,
                XivChatType.Ls7 => ChatTypes.LS7,
                XivChatType.Ls8 => ChatTypes.LS8,
                XivChatType.CrossLinkShell1 => ChatTypes.CWL1,
                XivChatType.CrossLinkShell2 => ChatTypes.CWL2,
                XivChatType.CrossLinkShell3 => ChatTypes.CWL3,
                XivChatType.CrossLinkShell4 => ChatTypes.CWL4,
                XivChatType.CrossLinkShell5 => ChatTypes.CWL5,
                XivChatType.CrossLinkShell6 => ChatTypes.CWL6,
                XivChatType.CrossLinkShell7 => ChatTypes.CWL7,
                XivChatType.CrossLinkShell8 => ChatTypes.CWL8,
                (XivChatType)4410 => ChatTypes.DeathOther,
                (XivChatType)4154 => ChatTypes.ReviveOther,
                (XivChatType)2234 => ChatTypes.DeathSelf,
                (XivChatType)2106 => ChatTypes.ReviveSelf,
                _ => null
            };
        }
    }
}
