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
using WoLightning.Classes;
using WoLightning.Types;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace WoLightning
{
    public enum ConnectionStatus
    {
        NotStarted = 0,
        NotConnected = 1,
        Unavailable = 2, // temporarily used as server isnt active

        WontRespond = 101,
        Outdated = 102,
        UnknownUser = 103,
        InvalidKey = 104,
        FatalError = 105,
        DevMode = 106,

        Connecting = 199,
        Connected = 200,
    }

    public class WebClient : IDisposable
    {
        private readonly Plugin Plugin;
        public string ServerVersion = string.Empty;
        private List<double> lastPings { get; set; } = new(); // Todo implement proper Ping class instead

        private readonly double maxPingsStored = 5;
        public ConnectionStatus Status { get; set; } = ConnectionStatus.NotStarted;
        public readonly TimerPlus PingTimer = new TimerPlus();
        private readonly double pingSpeed = new TimeSpan(0, 0, 3).TotalMilliseconds;
        private readonly double retrySpeed = new TimeSpan(0, 3, 0).TotalMilliseconds;

        public bool failsafe { get; set; } = false;

        private HttpClient? Client;
        private HttpClient? ClientClean;
        public WebClient(Plugin plugin)
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


            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.SslProtocols = SslProtocols.Tls12;
            handler.ClientCertificates.Add(Plugin.Authentification.getCertificate());
            handler.AllowAutoRedirect = true;
            handler.MaxConnectionsPerServer = 2;

            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, error) => { return cert != null && handler.ClientCertificates.Contains(cert); };
            Client = new(handler) { Timeout = TimeSpan.FromSeconds(10) };
            Plugin.Log("HttpClient successfully created!");

            establishWebserverConnection();
            ClientClean = new HttpClient();
            requestPishockInfoAll();
        }


        public async void sendPishockRequest(Trigger TriggerObject)
        {

            Plugin.Log($"{TriggerObject.Name} fired - sending request for {TriggerObject.Shockers.Count} shockers.");
            Plugin.Log($" -> Parameters -  {TriggerObject.OpMode} {TriggerObject.Intensity}% for {TriggerObject.Duration}s");

            //Validation of Data
            if (Plugin.Authentification.PishockName.Length < 3
                || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.Log(" -> Aborted due to invalid Account Settings!");
                return;
            }

            if (failsafe)
            {
                Plugin.Log(" -> Blocked request due to failsafe mode!");
                return;
            }

            if (!TriggerObject.Validate())
            {
                Plugin.Log(" -> Blocked due to invalid TriggerObject!");
                return;
            }


            Plugin.Log($" -> Data Validated. Creating Requests...");

            foreach (var shocker in TriggerObject.Shockers)
            {
                using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                Username = Plugin.Authentification.PishockName,
                Name = "WoLPlugin",
                Code = shocker.Code,
                Intensity = TriggerObject.Intensity,
                Duration = TriggerObject.Duration,
                Apikey = Plugin.Authentification.PishockApiKey,
                Op = (int)TriggerObject.OpMode,
            }),
            Encoding.UTF8,
            "application/json");

                try
                {
                    await ClientClean.PostAsync("https://do.pishock.com/api/apioperate", jsonContent);
                }
                catch (Exception ex)
                {
                    Plugin.Error(ex.ToString());
                    Plugin.Error("Error when sending post request to pishock api");
                }
            }
            Plugin.Log($" -> Requests sent!");
        }

        public async void sendPishockRequest(Trigger TriggerObject, int[] overrideSettings)
        {

            Plugin.Log($"{TriggerObject.Name} fired - sending request for {TriggerObject.Shockers.Count} shockers.");
            Plugin.Log($" -> Parameters using Override -  {overrideSettings[0]} {overrideSettings[1]}% for {overrideSettings[2]}s");

            //Validation of Data
            if (Plugin.Authentification.PishockName.Length < 3
                || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.Log(" -> Aborted due to invalid Account Settings!");
                return;
            }

            if (failsafe)
            {
                Plugin.Log(" -> Blocked request due to failsafe mode!");
                return;
            }

            if (!TriggerObject.Validate())
            {
                Plugin.Log(" -> Blocked due to invalid TriggerObject!");
                return;
            }

            if (overrideSettings.Length != 3 || overrideSettings[0] < 0 || overrideSettings[0] > 2)
            {
                Plugin.Log(" -> Blocked due to invalid OverrideSettings!");
                return;
            }

            // Clamp Settings
            if (overrideSettings[1] < 1) overrideSettings[1] = 1;
            if (overrideSettings[1] > 100) overrideSettings[1] = 100;
            if (overrideSettings[2] < 1) overrideSettings[2] = 1;
            if (overrideSettings[2] > 10) overrideSettings[2] = 10;

            Plugin.Log($" -> Data Validated. Creating Requests...");

            foreach (var shocker in TriggerObject.Shockers)
            {
                using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                Username = Plugin.Authentification.PishockName,
                Name = "WoLPlugin",
                Code = shocker.Code,
                Intensity = overrideSettings[0],
                Duration = overrideSettings[1],
                Apikey = Plugin.Authentification.PishockApiKey,
                Op = (int)TriggerObject.OpMode,
            }),
            Encoding.UTF8,
            "application/json");

                try
                {
                    await ClientClean.PostAsync("https://do.pishock.com/api/apioperate", jsonContent);
                }
                catch (Exception ex)
                {
                    Plugin.Error(ex.ToString());
                    Plugin.Error("Error when sending post request to pishock api");
                }
            }
            Plugin.Log($" -> Requests sent!");
        }

        public async void sendPishockTestAll()
        {

            Plugin.Log($"Sending Test request for {Plugin.Authentification.PishockShockers.Count} shockers.");

            if (Plugin.Authentification.PishockName.Length < 3
                || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.Log(" -> Aborted due to invalid Account Settings!");
                return;
            }
            Plugin.Log($" -> Data Validated. Creating Requests...");


            foreach (var shocker in Plugin.Authentification.PishockShockers)
            {
                using StringContent jsonContent = new(
                JsonSerializer.Serialize(new
                {
                    Username = Plugin.Authentification.PishockName,
                    Name = "WoLPlugin",
                    Code = shocker.Code,
                    Intensity = 35,
                    Duration = 3,
                    Apikey = Plugin.Authentification.PishockApiKey,
                    Op = 1,
                }),
                Encoding.UTF8,
                "application/json");

                try
                {
                    await ClientClean.PostAsync("https://do.pishock.com/api/apioperate", jsonContent);
                }
                catch (Exception ex)
                {
                    Plugin.Error(ex.ToString());
                    Plugin.Error("Error when sending post request to pishock api");
                }
            }
            Plugin.Log($" -> Requests sent!");
        }

        public async void requestPishockInfo(string ShareCode)
        {

            Plugin.Log($"Requesting Information for {ShareCode}...");

            if (Plugin.Authentification.PishockName.Length < 3
               || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.Log(" -> Aborted due to invalid Account Settings!");
                return;
            }
            Plugin.Log($" -> Data Validated. Creating Requests...");


            using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                Username = Plugin.Authentification.PishockName,
                Code = ShareCode,
                Apikey = Plugin.Authentification.PishockApiKey,

            }),
            Encoding.UTF8,
            "application/json");

            Shocker shocker = Plugin.Authentification.PishockShockers.Find(shocker => shocker.Code == ShareCode);
            if (shocker == null) return;
            shocker.Status = ShockerStatus.Unchecked;
            try
            {
                var s = await ClientClean.PostAsync("https://do.pishock.com/api/GetShockerInfo", jsonContent);
                if (s.StatusCode == HttpStatusCode.OK)
                    processPishockResponse(s.Content, shocker);
                else shocker.Status = ShockerStatus.Offline;
            }
            catch (Exception ex)
            {
                Plugin.Error(ex.ToString());
                Plugin.Error("Error when sending post request to pishock api");
            }
        }

        public async void requestPishockInfoAll()
        {
            Plugin.Log($"Requesting Information for all Shockers.");

            if (Plugin.Authentification.PishockName.Length < 3
                || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.Log(" -> Aborted due to invalid Account Settings!");
                return;
            }
            Plugin.Log($" -> Data Validated. Creating Requests...");


            foreach (var shocker in Plugin.Authentification.PishockShockers)
            {
                Plugin.Log($" -> Requesting Information for {shocker.Code}...");
                shocker.Status = ShockerStatus.Unchecked;
                using StringContent jsonContent = new(
                JsonSerializer.Serialize(new
                {
                    Username = Plugin.Authentification.PishockName,
                    Code = shocker.Code,
                    Apikey = Plugin.Authentification.PishockApiKey,
                }),
                Encoding.UTF8,
                "application/json");

                try
                {
                    var s = await ClientClean.PostAsync("https://do.pishock.com/api/GetShockerInfo", jsonContent);

                    if (s.StatusCode == HttpStatusCode.OK)
                        processPishockResponse(s.Content, shocker);
                    else shocker.Status = ShockerStatus.Offline;
                }
                catch (Exception ex)
                {
                    Plugin.Error(ex.ToString());
                    Plugin.Error("Error when sending post request to pishock api");
                }
            }
            Plugin.Log($" -> Requests sent!");
        }


        public void sendWebserverRequest(OperationCode Op) { sendWebserverRequest(Op, null, null); }
        public void sendWebserverRequest(OperationCode Op, String? OpData) { sendWebserverRequest(Op, OpData, null); }

        public async void sendWebserverRequest(OperationCode Op, String? OpData, Player? Target)
        {
            if (Status == ConnectionStatus.Unavailable) return;

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
                        Status = ConnectionStatus.Connected;
                        if (s.Content != null) processResponse(packet, s.Content.ReadAsStringAsync());
                        break;

                    case HttpStatusCode.Locked:
                        Status = ConnectionStatus.DevMode;
                        Plugin.Log("The Server is currently in DevMode.");
                        break;

                    // Softerrors DEPRECATED
                    case HttpStatusCode.Unauthorized:
                        Status = ConnectionStatus.UnknownUser;
                        Plugin.Log("The Server dídnt know us, so we got registered.");
                        if (s.Content != null) processResponse(packet, s.Content.ReadAsStringAsync());
                        break;

                    case HttpStatusCode.UpgradeRequired:
                        Status = ConnectionStatus.Outdated;
                        Plugin.Log("We are running a outdated Version.");
                        if (s.Content != null) processResponse(packet, s.Content.ReadAsStringAsync());
                        break;

                    case HttpStatusCode.Forbidden:
                        Status = ConnectionStatus.InvalidKey;
                        Plugin.Error("Our Key does not match the key on the Serverside.", packet);
                        break;


                    // Harderrors
                    case HttpStatusCode.NotFound:
                        Status = ConnectionStatus.FatalError;
                        Plugin.Error("We sent a invalid Request to the Server.", packet);
                        break;
                    case HttpStatusCode.InternalServerError:
                        Status = ConnectionStatus.FatalError;
                        Plugin.Error("We sent a invalid Packet to the Server.", packet);
                        break;

                    default:
                        Status = ConnectionStatus.FatalError;
                        Plugin.Error($"Unknown Response {s.StatusCode}", packet);
                        return;
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {

                Status = ConnectionStatus.WontRespond;
                Plugin.Log("The Server is not responding.");
                severWebserverConnection();
                return;
            }
            catch (TaskCanceledException ex)
            {
                Plugin.Error("Running Request was Cancelled.",ex);
                return;
            }
            catch (HttpRequestException)
            {

                Status = ConnectionStatus.WontRespond;
                Plugin.Log("The Server is online, but refused the connection.");
                severWebserverConnection();
                return;
            }
            catch (Exception ex)
            {

                Status = ConnectionStatus.FatalError;
                Client.CancelPendingRequests();
                Plugin.Error(ex.ToString());
                severWebserverConnection(true);
                return;
            }

        }



        public void establishWebserverConnection()
        {
            PingTimer.Interval = pingSpeed;
            PingTimer.Elapsed -= sendPing; // make sure we only have one ping event
            PingTimer.Elapsed += sendPing;
            PingTimer.Start();
            sendWebserverRequest(OperationCode.Login);
        }

        public void severWebserverConnection()
        {
            PingTimer.Interval = retrySpeed;
            PingTimer.Refresh();
        }

        public void severWebserverConnection(bool force)
        {
            PingTimer.Interval = retrySpeed;
            if (PingTimer.Enabled) PingTimer.Elapsed -= sendPing;
            if (PingTimer.Enabled) PingTimer.Stop();
        }

        internal void sendPing(object? o, ElapsedEventArgs? e)
        {
            if (Status != ConnectionStatus.Connected) sendWebserverRequest(OperationCode.Login);
            else sendWebserverRequest(OperationCode.Ping);
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
                    Plugin.Error("We have received a invalid packet.",re);
                    return;
                }

                if(!re.Sender.equals(Plugin.LocalPlayer) && !re.Target.equals(Plugin.LocalPlayer))
                {
                    Plugin.Error("The received packet is neither from nor for us.",re);
                    return;
                }

                if (re.OpData != null && re.OpData.Equals("Fail-Unauthorized"))
                {
                    Plugin.Error("The server does not remember us sending a request.",re);
                    return;
                }

                if(re.Operation != OperationCode.Ping) Plugin.Log(re);

                String? result = Plugin.Operation.execute(originalPacket, re);
                if (result != null)
                {
                    Plugin.Error(result,re);
                    return;
                }
                
            }
            catch (Exception ex)
            {
                Plugin.Error(ex.ToString());
            }
        }

        private void processPishockResponse(HttpContent response)
        {

            using (var reader = new StreamReader(response.ReadAsStream()))
            {
                string message = reader.ReadToEnd();
                Plugin.Log(message);
                message = message.Replace("\"", "");
                message = message.Replace("{", "");
                message = message.Replace("}", "");
                string[] partsRaw = message.Split(',');
                Dictionary<String, String> headers = new Dictionary<String, String>();
                foreach (var part in partsRaw) headers.Add(part.Split(':')[0], part.Split(':')[1]);

                foreach (var (key, value) in headers)
                {
                    Plugin.Log($"{key}: {value}");
                }
            }

        }

        private void processPishockResponse(HttpContent response, Shocker shocker)
        {

            shocker.Status = ShockerStatus.Online;
            using (var reader = new StreamReader(response.ReadAsStream()))
            {
                string message = reader.ReadToEnd();
                message = message.Replace("\"", "");
                message = message.Replace("{", "");
                message = message.Replace("}", "");
                string[] partsRaw = message.Split(',');
                Dictionary<String, String> headers = new Dictionary<String, String>();
                foreach (var part in partsRaw) headers.Add(part.Split(':')[0], part.Split(':')[1]);

                if (headers.ContainsKey("name")) shocker.Name = headers["name"];
                if (headers.ContainsKey("paused") && bool.Parse(headers["paused"]) == true) shocker.Status = ShockerStatus.Paused;
            }

        }

        public bool toggleFailsafe()
        {
            failsafe = !failsafe;
            //todo: kill all threads in here
            if (failsafe) Plugin.NetworkWatcher.Dispose();
            else Plugin.NetworkWatcher.Start();

            return failsafe;
        }



    }
}
