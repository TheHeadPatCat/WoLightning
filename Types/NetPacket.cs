using System;

namespace WoLightning.Types
{
    public enum Operation
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

    [Serializable]
    public class NetPacket
    {
        public Operation Operation { get; set; }
        public string Sender { get; set; }
        public string? Target { get; set; }
        public string? OpData { get; set; }

        #region Constructors
        public NetPacket(Operation Type, String Sender)
        {
            Operation = Type;
            this.Sender = Sender;
        }
        public NetPacket(Operation Type, String Sender, String OpData)
        {
            Operation = Type;
            this.Sender = Sender;
            this.OpData = OpData;
        }
        public NetPacket(Operation Type, String Sender, String OpData, String Target)
        {
            Operation = Type;
            this.Sender = Sender;
            this.OpData = OpData;
            this.Target = Target;
        }
        #endregion


        public override string ToString()
        {
            string output = $"[NetPacket] Op: {Operation.ToString()} as {Sender}";
            if (Target != null) output += "Targeting: " + Target;
            if (OpData != null) output += " with Data: " + OpData;
            return output;
        }

    }
}
