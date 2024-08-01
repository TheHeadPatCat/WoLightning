using System;
using WoLightning.Classes;

namespace WoLightning.Types
{
    

    [Serializable]
    public class NetPacket
    {
        public OperationCode? Operation { get; set; } // Operation will be echoe'd by the Server, with fitting Opdata for the Result
        public Player? Sender { get; set; } // If we get a packet with us as the Sender, its a Server Answer.
        public Player? Target { get; set; } // Target might not be needed, so nullable
        public string? OpData { get; set; } // Not all Operations need arguments, server will include a message for the log

        #region Constructors
        public NetPacket() { }
        public NetPacket(OperationCode Type, Player Sender)
        {
            Operation = Type;
            this.Sender = Sender;
        }
        public NetPacket(OperationCode Type, Player Sender, String? OpData)
        {
            Operation = Type;
            this.Sender = Sender;
            this.OpData = OpData;
        }
        public NetPacket(OperationCode Type, Player Sender, String? OpData, Player? Target)
        {
            Operation = Type;
            this.Sender = Sender;
            this.OpData = OpData;
            this.Target = Target;
        }
        #endregion


        public bool validate()
        {
            return (Operation >= 0 && Sender != null && Sender.validate() && (Target == null || Target.validate()));

        }

        public override string ToString()
        {
            string output = $"[NetPacket] Op: {Operation.ToString()} as {Sender.ToString()}";
            if (Target != null) output += "Targeting: " + Target.ToString();
            if (OpData != null) output += " with Data: " + OpData;
            return output;
        }
    }
}
