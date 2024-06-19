using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WoLightning
{
    public class NetworkPacket
    {

        public string encodedData = "";


        public NetworkPacket() {
        }
        public NetworkPacket(string data) //received packet
        {
            encodedData = data;
        }

        public NetworkPacket(string type, string data) //created packet
        {
            encodedData = encodeMessage(type, data);
        }

        public NetworkPacket(string[] type, string[] data) //created packet in big
        {
            encodedData = encodeMessage(type, data);
        }

        public void append(string encodedData)
        {
            if (encodedData == "") this.encodedData = encodedData;
            else this.encodedData += "@@" + encodedData;
        }

        public void append(string type, string data)
        {
            if (encodedData == "") encodedData = encodeMessage(type, data);
            else encodedData += "@@" + encodeMessage(type, data);
        }

        public void append(NetworkPacket net)
        {
            if (encodedData == "") encodedData = net.encodedData;
            else encodedData += "@@" + net.encodedData;
        }

        /*Structure:
         * Packet@Reason
         * 
         * Preset@ShareString
         * Serverkey@ReceivedKey
         * Refplayer@playerNameFull
         * 
         * RequestMaster@targetNameFull
         * AnswerMaster@masterNameFull
         * 
         */


        public void resolve(Plugin Plugin)
        {
            if (encodedData == null) return;
            if (encodedData.Length < 3) return;
            string refPlayer = "";
            foreach (string packet in encodedData.Split("@@"))
            {
                Plugin.PluginLog.Info("Resolving Part: " + packet);
                if (packet.Length < 3) break;
                string[] dis = packet.Split("@");
                if (dis.Length != 2) break; //invalid packet

                Plugin.PluginLog.Info("Passed Validation.");
                switch (dis[0])
                {
                    case "packet": Plugin.PluginLog.Info("Name of Packet: " + packet); refPlayer = ""; break;
                    case "serverkey": Plugin.Authentification.ServerKey = dis[1]; Plugin.Authentification.Save(); break;
                    case "refplayer": refPlayer = dis[1]; break;


                    // Master Stuff
                    case "requestmaster": Plugin.handleMasterRequest(refPlayer); break;
                    case "answermaster": Plugin.handleMasterAnswer(dis[1]); break;
                    case "unbindsub": Plugin.handleSubUnbind(); break;
                    case "importpreset": Plugin.Configuration.importPreset(dis[1]); break;
                    case "swappreset": Plugin.Configuration.swapPreset(dis[1]); break;
                    case "updatesetting": Plugin.Configuration.updateBoolSetting(dis[1]); break;
                    case "updatesubstatus": Plugin.updateMasterWindow(refPlayer, bool.Parse(dis[1])); break;
                    case "setpluginstate": if (bool.Parse(dis[1])) Plugin.NetworkWatcher.Start(); else Plugin.NetworkWatcher.Stop(); break;

                    default: Plugin.PluginLog.Error("Received Packet that has an Invalid type!", dis); break;
                }
                Plugin.PluginLog.Info("Done resolving packet.");
            }
            Plugin.PluginLog.Info("Finished decoding.");
            
        }

        private string encodeMessage(string type, string input)
        {
            return encodeMessage(["packet", type], ["quick", input]);
        }

        private string encodeMessage(string[] type, string[] input)
        {
            if (type.Length != input.Length) return "";
            int x = 0;
            string result = "";
            foreach (string packet in input)
            {
                if (x > 0) result += "@@";
                result += type[x] + "@" + packet;
                x++;
            }
            return result;
        }


        override public string ToString()
        {
            return encodedData;
        }

    }
}
