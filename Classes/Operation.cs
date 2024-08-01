using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WoLightning.Types;

namespace WoLightning.Classes
{
    public enum OperationCode
    {
        // General
        Ping = 0,
        RequestUpdate = 1,
        RequestVersion = 2,
        RequestServerState = 3,

        // Presets Sharing
        RequestPresetLinked = 100,
        PostPresetLinked = 101,

        RequestPresetsPublic = 102, // Public Presets
        PostPresetPublíc = 103,

        // Account
        Login = 500,
        Register = 501,

        UploadBackup = 510, // Backups
        RequestBackup = 511,
        RequestReset = 520,

        // Master Mode
        RequestUpdateMaster = 600,
        RequestUpdateSubs = 601,

        RequestBecomeSub = 610, // Registering new Master/Sub
        AnswerSub = 611,
        RegisterMaster = 612,
        RegisterSub = 613,

        UnbindMaster = 620,
        UnbindSub = 621,

        // Master Mode Orders
        OrderPresetLoad = 630,
        OrderPresetImport = 631,
        OrderSettingChange = 632,
        OrderEnabledChange = 633,

        // Dev
        PostVersion = 900,
        PostCommand = 901,

    }


    public class Operation
    {
        private readonly Plugin Plugin;
        
        public Operation(Plugin Plugin) { 
            this.Plugin = Plugin;
        }

        public String? execute(NetPacket originalPacket,NetPacket responsePacket) // null is success - String returned is Error message
        {

            // So, you might look at this and think to yourself... dear god this is terrible
            // But believe it or not, this is more effective and less overengineered than going with a Class based approach - especially due to Permission Layout.
            // It is by no means the best way to do it, but it is a way to do it.

            switch (responsePacket.Operation)
            {

                // General
                case OperationCode.Ping:
                    return null;
                    
                case OperationCode.RequestUpdate:
                    return "Not Implemented";
                case OperationCode.RequestVersion:
                    Plugin.WebClient.ServerVersion = responsePacket.OpData;
                    return null;
                case OperationCode.RequestServerState:
                    return "Not Implemented";

                // Presets
                case OperationCode.RequestPresetLinked:
                    return "Not Implemented";
                case OperationCode.PostPresetLinked:
                    return "Not Implemented";
                case OperationCode.RequestPresetsPublic:
                    return "Not Implemented";
                case OperationCode.PostPresetPublíc:
                    return "Not Implemented";

                // Account
                case OperationCode.Login:
                    if (responsePacket.OpData != null && responsePacket.OpData.Split("-")[0] == "Success")
                    {
                        Plugin.WebClient.Status = ConnectionStatus.Connected;
                        Plugin.PluginLog.Verbose("Logged into the Webserver!");
                        return null;
                    }
                    Plugin.WebClient.Status = ConnectionStatus.UnknownUser;
                    return responsePacket.OpData;
                case OperationCode.Register:
                    if (responsePacket.OpData != null && responsePacket.OpData.Split("-")[0] == "Success")
                    {
                        Plugin.Authentification.ServerKey = responsePacket.Sender.Key;
                        Plugin.PluginLog.Verbose("We have been registered to the Server.", responsePacket.Sender.Key);
                        return null;
                    }
                    return responsePacket.OpData;

                case OperationCode.UploadBackup:
                    return "Not Implemented";
                case OperationCode.RequestBackup:
                    return "Not Implemented";
                case OperationCode.RequestReset:
                    return "Not Implemented";

                // Master Mode
                case OperationCode.RequestUpdateMaster:
                    return "Not Implemented";
                case OperationCode.RequestUpdateSubs:
                    return "Not Implemented";
                case OperationCode.RequestBecomeSub:
                    return "Not Implemented";
                case OperationCode.AnswerSub:
                    return "Not Implemented";
                case OperationCode.RegisterMaster:
                    return "Not Implemented";
                case OperationCode.RegisterSub:
                    return "Not Implemented";

                case OperationCode.UnbindMaster:
                    return "Not Implemented";
                case OperationCode.UnbindSub:
                    return "Not Implemented";


                // Master Mode Orders
                case OperationCode.OrderPresetLoad:
                    return "Not Implemented";
                case OperationCode.OrderPresetImport:
                    return "Not Implemented";
                case OperationCode.OrderSettingChange:
                    return "Not Implemented";
                case OperationCode.OrderEnabledChange:
                    return "Not Implemented";



                // Dev
                case OperationCode.PostVersion:
                    return "Not Implemented";
                case OperationCode.PostCommand:
                    return "Not Implemented";

                default:
                    return "Operation is Invalid";
                    
                    
            }
        }

    }
}
