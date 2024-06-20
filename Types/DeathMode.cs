using System.Text.RegularExpressions;

namespace WoLightning.Types
{

    public class DeathMode
    {
        public static Regex DeathModeDieOtherRegex = new Regex("^(.*) is defeated");
        public static Regex DeathModeLiveOtherRegex = new Regex("^(.*) is revived");

        public static ChatType.ChatTypes[] deathTypes = [
            ChatType.ChatTypes.DeathOther,
            ChatType.ChatTypes.DeathSelf,
            ChatType.ChatTypes.ReviveOther,
            ChatType.ChatTypes.ReviveSelf
        ];
    }

}
