using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WoLightning.Types;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace WoLightning.Classes
{
    public enum ConnectionStatusWebserver
    {
        NotStarted = 0,
        NotConnected = 1,
        Unavailable = 2,
        EulaNotAccepted = 3,

        WontRespond = 101,
        Outdated = 102,
        UnknownUser = 103,
        InvalidKey = 104,
        FatalError = 105,
        DevMode = 106,

        Connecting = 199,
        Connected = 200,
    }

    public class ClientWebserver : IDisposable // Todo - Rewrite.
    {
        private readonly Plugin Plugin;
        public string ServerVersion = string.Empty;
        private List<double> lastPings { get; set; } = new(); // Todo implement proper Ping class instead

        private readonly double maxPingsStored = 5;
        public ConnectionStatusWebserver Status { get; set; } = ConnectionStatusWebserver.NotStarted;
        public readonly TimerPlus PingTimer = new TimerPlus();
        private readonly double pingSpeed = new TimeSpan(0, 0, 3).TotalMilliseconds;
        private readonly double retrySpeed = new TimeSpan(0, 3, 0).TotalMilliseconds;

        public bool failsafe { get; set; } = false;

        private HttpClient? Client;
        public ClientWebserver(Plugin plugin)
        {
            Plugin = plugin;
            lastPings.EnsureCapacity(6);
        }
        public void Dispose()
        {
            if (Client != null)
            {
                Client.CancelPendingRequests();
                Client.Dispose();
                Client = null;
            }
            PingTimer.Stop();
            PingTimer.Dispose();
        }

        public int Ping()
        {
            double avg = 0;
            if (lastPings.Count > 5) lastPings = lastPings.Slice(0, 5);
            foreach (double time in lastPings) avg += time;
            return (int)(avg / lastPings.Count / 3); // adjusted due to the 3 second pinging
        }

        public void createHttpClient()
        {
            if (Client != null) return;

            if(Plugin.Authentification.DevKey.Length == 0)
            {
                Plugin.Log("No Devkey detected - Stopping ClientWebserver creation.");
                Status = ConnectionStatusWebserver.DevMode;
                return;
            }

            if (!Plugin.Authentification.acceptedEula)
            {
                Plugin.Log("Eula isn't accepted - Stopping ClientWebserver creation.");
                Status = ConnectionStatusWebserver.EulaNotAccepted;
                return;
            }

            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.SslProtocols = SslProtocols.Tls12;
            handler.ClientCertificates.Add(Plugin.Authentification.getCertificate());
            handler.AllowAutoRedirect = true;
            handler.MaxConnectionsPerServer = 2;

            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, error) => { return cert != null && handler.ClientCertificates.Contains(cert); };
            Client = new(handler) { Timeout = TimeSpan.FromSeconds(10) };
            Plugin.Log("ClientWebserver successfully created!");

            connect();
        }

        public void connect()
        {
            PingTimer.Interval = pingSpeed;
            PingTimer.Elapsed -= sendPing; // make sure we only have one ping event
            PingTimer.Elapsed += sendPing;
            PingTimer.Start();
            request(OperationCode.Login);
        }
        public void disconnect()
        {
            PingTimer.Interval = retrySpeed;
            PingTimer.Refresh();
        }
        public void disconnect(bool force)
        {
            PingTimer.Interval = retrySpeed;
            if (PingTimer.Enabled) PingTimer.Elapsed -= sendPing;
            if (PingTimer.Enabled) PingTimer.Stop();
        }

        public void request(OperationCode Op) { request(Op, null, null); }
        public void request(OperationCode Op, String? OpData) { request(Op, OpData, null); }
        public async void request(OperationCode Op, String? OpData, Player? Target)
        {
            if (Status == ConnectionStatusWebserver.Unavailable) return;

            if (Plugin.ClientState.LocalPlayer == null ||
                Client == null) return;




            NetPacket packet = new NetPacket(Op, Plugin.LocalPlayer, OpData, Target);

            using StringContent jsonContent = new(
                JsonSerializer.Serialize(new
                {
                    hash = "n982093c09209jg0920g", // Plugin.Authentification.getHash()
                    devKey = Plugin.Authentification.DevKey,
                    packet,
                }),
            Encoding.UTF8,
            "application/json");

            try
            {

                Stopwatch timeTaken = Stopwatch.StartNew();
                var s = await Client.PostAsync($"https://theheadpatcat.ddns.net/post/WoLightning", jsonContent);
                timeTaken.Stop();
                lastPings.Insert(0, timeTaken.ElapsedMilliseconds);
                switch (s.StatusCode)
                {

                    case HttpStatusCode.OK:
                        if (PingTimer.Interval != pingSpeed) // todo add more precise logic
                        {
                            PingTimer.Interval = pingSpeed;
                            if (PingTimer.Enabled) PingTimer.Refresh();
                            else PingTimer.Start();
                            Plugin.Log("Reset Timer");
                        }
                        Status = ConnectionStatusWebserver.Connected;
                        if (s.Content != null) processResponse(packet, s.Content.ReadAsStringAsync());
                        break;

                    case HttpStatusCode.Locked:
                        Status = ConnectionStatusWebserver.DevMode;
                        Plugin.Log("The Server is currently in DevMode.");
                        disconnect();
                        break;

                    // Softerrors DEPRECATED
                    case HttpStatusCode.Unauthorized:
                        Status = ConnectionStatusWebserver.UnknownUser;
                        Plugin.Log("The Server dídnt know us, so we got registered.");
                        if (s.Content != null) processResponse(packet, s.Content.ReadAsStringAsync());
                        break;

                    case HttpStatusCode.UpgradeRequired:
                        Status = ConnectionStatusWebserver.Outdated;
                        Plugin.Log("We are running a outdated Version.");
                        if (s.Content != null) processResponse(packet, s.Content.ReadAsStringAsync());
                        break;

                    case HttpStatusCode.Forbidden:
                        Status = ConnectionStatusWebserver.InvalidKey;
                        Plugin.Error("Our Key does not match the key on the Serverside.", packet);
                        break;


                    // Harderrors
                    case HttpStatusCode.NotFound:
                        Status = ConnectionStatusWebserver.FatalError;
                        Plugin.Error("We sent a invalid Request to the Server.", packet);
                        break;
                    case HttpStatusCode.InternalServerError:
                        Status = ConnectionStatusWebserver.FatalError;
                        Plugin.Error("We sent a invalid Packet to the Server.", packet);
                        break;

                    default:
                        Status = ConnectionStatusWebserver.FatalError;
                        Plugin.Error($"Unknown Response {s.StatusCode}", packet);
                        return;
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {

                Status = ConnectionStatusWebserver.WontRespond;
                Plugin.Log("The Server is not responding.");
                disconnect();
                return;
            }
            catch (TaskCanceledException ex)
            {
                Plugin.Error("Running Request was Cancelled.", ex);
                return;
            }
            catch (HttpRequestException)
            {

                Status = ConnectionStatusWebserver.WontRespond;
                Plugin.Log("The Server is online, but refused the connection.");
                disconnect();
                return;
            }
            catch (Exception ex)
            {

                Status = ConnectionStatusWebserver.FatalError;
                Client.CancelPendingRequests();
                Plugin.Error(ex.ToString());
                disconnect(true);
                return;
            }

        }

        private async void processResponse(NetPacket originalPacket, Task<String> responseString)
        {
            try
            {
                String? s = await responseString;
                if (s == null) return;
                NetPacket? re = JsonSerializer.Deserialize<NetPacket>(s);
                if (re == null) return;

                if (!re.validate())
                {
                    Plugin.Error("We have received a invalid packet.", re);
                    return;
                }

                if (!re.Sender.equals(Plugin.LocalPlayer) && !re.Target.equals(Plugin.LocalPlayer))
                {
                    Plugin.Error("The received packet is neither from nor for us.", re);
                    return;
                }

                if (re.OpData != null && re.OpData.Equals("Fail-Unauthorized"))
                {
                    Plugin.Error("The server does not remember us sending a request.", re);
                    return;
                }

                if (re.Operation != OperationCode.Ping) Plugin.Log(re);

                String? result = Plugin.Operation.execute(originalPacket, re);
                if (result != null)
                {
                    Plugin.Error(result, re);
                    return;
                }

            }
            catch (Exception ex)
            {
                Plugin.Error(ex.ToString());
            }
        }

        internal void sendPing(object? o, ElapsedEventArgs? e)
        {
            if (Status != ConnectionStatusWebserver.Connected) request(OperationCode.Login);
            else request(OperationCode.Ping);
        }

    }
}
