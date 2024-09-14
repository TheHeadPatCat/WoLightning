using System;
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
        Reset = 502,

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

        public Operation(Plugin Plugin)
        {
            this.Plugin = Plugin;
        }

        public static int[] allOpcodesInt()
        {
            int[] result = new int[Enum.GetValues(typeof(OperationCode)).Length];
            int i = 0;
            foreach (int op in Enum.GetValues(typeof(OperationCode)))
            {
                result[i] = op;
                i++;
            }
            return result;
        }
        public static string[] allOpCodesString()
        {
            string[] result = new string[Enum.GetNames(typeof(OperationCode)).Length];
            int i = 0;
            foreach (string op in Enum.GetNames(typeof(OperationCode)))
            {
                result[i] = op;
                i++;
            }
            return result;
        }

        public static string[] allOpCodesString(bool includeCode)
        {
            string[] result = new string[Enum.GetNames(typeof(OperationCode)).Length];
            int i = 0;
            foreach (OperationCode op in Enum.GetValues(typeof(OperationCode)))
            {
                result[i] = $"({(int)op}) - " + op.ToString();
                i++;
            }
            return result;
        }

        public static OperationCode[] allOpCodes()
        {
            OperationCode[] result = new OperationCode[Enum.GetValues(typeof(OperationCode)).Length];
            int i = 0;
            foreach (OperationCode op in Enum.GetValues(typeof(OperationCode)))
            {
                result[i] = op;
                i++;
            }
            return result;
        }

        public static OperationCode getOperationCode(string name)
        {
            foreach (OperationCode op in Enum.GetValues(typeof(OperationCode)))
            {
                if (name == op.ToString()) return op;
            }
            throw new Exception("Invalid OperationCode"); // maybe not throw this...? 
        }

        public String? execute(NetPacket originalPacket, NetPacket responsePacket) // null is success - String returned is Error message
        {

            // So, you might look at this and think to yourself... dear god this is terrible
            // But believe it or not, this is more effective and less overengineered than going with a Class based approach - especially due to Permission Layout.
            // It is by no means the best way to do it, but it is a way to do it.

            switch (responsePacket.Operation)
            {

                // General
                case OperationCode.Ping:
                    // We pinged the server, and it did not have anything for us.
                    return null;

                case OperationCode.RequestUpdate:
                    return "Not Implemented";
                case OperationCode.RequestVersion:
                    Plugin.ClientWebserver.ServerVersion = responsePacket.OpData;
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

                    // We logged in!
                    if (responsePacket.OpData != null && responsePacket.OpData.Split("-")[0] == "Success")
                    {
                        Plugin.ClientWebserver.Status = ConnectionStatusWebserver.Connected;
                        Plugin.Log("Logged into the Webserver!");

                        return null;
                    }

                    // We arent known to the server - register us.
                    if (responsePacket.OpData != null && (responsePacket.OpData.Split("-")[1] == "NotRegistered"))
                    {
                        Plugin.ClientWebserver.sendWebserverRequest(OperationCode.Register);
                        return null;
                    }

                    if (responsePacket.OpData != null && (responsePacket.OpData.Equals("Fail-InvalidKey")))
                    {
                        Plugin.ClientWebserver.severWebserverConnection();
                        Plugin.ClientWebserver.Status = ConnectionStatusWebserver.InvalidKey;
                        return "Cannot Login - Invalid Key";
                    }
                    return "Received Invalid OpData";

                case OperationCode.Register:
                    if (responsePacket.OpData != null && responsePacket.OpData.Split("-")[0] == "Success")
                    {
                        Plugin.Authentification.ServerKey = responsePacket.Sender.Key;
                        Plugin.Log("We have been registered to the Server - Key: " + responsePacket.Sender.Key);
                        return null;
                    }
                    else if (responsePacket.OpData != null && responsePacket.OpData == "Fail-AlreadyExists")
                    {
                        Plugin.ClientWebserver.severWebserverConnection();
                        Plugin.ClientWebserver.Status = ConnectionStatusWebserver.InvalidKey;
                        return "Cannot Register - We already exist.";
                    }


                    return responsePacket.OpData;
                case OperationCode.Reset:
                    if (responsePacket.OpData != null && responsePacket.OpData == "Success-Removed")
                    {
                        Plugin.Authentification.ServerKey = string.Empty;
                        Plugin.Log("Reset Userdata on Webserver.");
                        Plugin.ClientWebserver.sendWebserverRequest(OperationCode.Login);
                        return null;
                    }
                    else
                    {
                        Plugin.Log("Failed Reset");
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
                    // Update Authentification with Master



                    return "Not Implemented";
                case OperationCode.RequestUpdateSubs:
                    // Update Authentification with Subs



                    return "Not Implemented";


                case OperationCode.RequestBecomeSub:
                    // We received the request of another playing becoming our sub
                    if (responsePacket.Sender.equals(Plugin.LocalPlayer))
                    { // confirmation
                        return null;
                    }
                    if (responsePacket.Sender == null || !responsePacket.Sender.validate())
                    {
                        Plugin.ClientWebserver.sendWebserverRequest(OperationCode.AnswerSub, "Fail-InvalidSender");
                        return "Invalid Sender";
                    }

                    if (Plugin.Authentification.OwnedSubs.ContainsKey(responsePacket.Sender.getFullName()))
                    {
                        Plugin.ClientWebserver.sendWebserverRequest(OperationCode.AnswerSub, "Fail-AlreadyExists");
                        return "Already Exists";
                    }

                    //The Request is valid - show it to the master and let them decide how to respond.

                    Plugin.Authentification.targetSub = responsePacket.Sender;
                    Plugin.Authentification.gotRequest = true;
                    Plugin.ShowMasterUI();
                    return null;
                case OperationCode.AnswerSub:
                    if (responsePacket.OpData == null) return null; // Confirmation that the packet worked on Master side.

                    // We received the validation from the requested Master
                    if (!responsePacket.Target.equals(Plugin.LocalPlayer)) return "Invalid Target";
                    if (responsePacket.Sender == null || !responsePacket.Sender.validate()) return "Invalid Sender";
                    if (!responsePacket.OpData.StartsWith("Success"))
                    { // Validation Failed - let the user know
                        Plugin.Authentification.isRequesting = false;
                        Plugin.Authentification.errorStringMaster = responsePacket.OpData;
                        Plugin.Authentification.targetMaster = null;
                        return responsePacket.OpData;
                    }
                    // The Master responded to our request!
                    // Check if its accepted or rejected
                    if (responsePacket.OpData.Equals("Success-Rejected"))
                    {
                        Plugin.Authentification.targetMaster = null;
                        return "Master Rejected Request";
                    }
                    else if (responsePacket.OpData.Equals("Success-Accepted"))
                    {
                        Plugin.ClientWebserver.sendWebserverRequest(OperationCode.RegisterMaster, null, responsePacket.Sender);
                        return null;
                    }

                    return "Invalid Response Received";
                case OperationCode.RegisterMaster:

                    if (responsePacket.Target == null)
                    {
                        return "No Target Given";
                    }
                    if (Plugin.Authentification.Master != null)
                    {
                        Plugin.ClientWebserver.sendWebserverRequest(OperationCode.RegisterSub, "Fail-AlreadyBound");
                        return "Already bound to a Master";
                    }
                    if (!Plugin.Authentification.targetMaster.equals(responsePacket.Target)) // Make sure we cannot get spoofed
                    {
                        return "Invalid Master Given";
                    }

                    Plugin.Authentification.Master = responsePacket.Target;
                    Plugin.Authentification.HasMaster = true;
                    Plugin.Authentification.isRequesting = false;
                    Plugin.Authentification.targetMaster = null;
                    return null;

                case OperationCode.RegisterSub:
                    if (Plugin.Authentification.targetSub == null) return "Invalid Request"; // We are not anticipating anyone - Dont accept the packet
                    if (responsePacket.Target == null) return "No Sub given";
                    if (!Plugin.Authentification.targetSub.equals(responsePacket.Target)) return "Requested Sub and Saved Sub are not equal"; // This is not the Sub we expected to accept - Dont accept the packet

                    if (Plugin.Authentification.OwnedSubs.ContainsKey(responsePacket.Sender.getFullName()))
                    {
                        Plugin.ClientWebserver.sendWebserverRequest(OperationCode.RegisterSub, "Fail-AlreadyExists", responsePacket.Target);
                        return "Already Exists";
                    }

                    Plugin.Authentification.IsMaster = true;
                    Plugin.Authentification.OwnedSubs.Add(responsePacket.Target.getFullName(), responsePacket.Target);
                    return null;

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
