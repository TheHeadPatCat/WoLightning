
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
        public long Ping { get; set; } = -1;
        public ConnectionStatus Status { get; set; } = ConnectionStatus.Unavailable;
        public readonly TimerPlus UpdateTimer = new TimerPlus();
        private readonly double fast = new TimeSpan(0, 0, 2).TotalMilliseconds;
        private readonly double normal = new TimeSpan(0, 0, 15).TotalMilliseconds;
        private readonly double slow = new TimeSpan(0, 5, 0).TotalMilliseconds;

        public bool failsafe { get; set; } = false;

        private HttpClient? Client;
        private HttpClient? ClientClean;
        public WebClient(Plugin plugin)
        {
            Plugin = plugin;
        }
        public void Dispose()
        {
            if (Client != null)
            {
                Client.CancelPendingRequests();
                Client.Dispose();
                Client = null;
            }
            UpdateTimer.Stop();
            UpdateTimer.Dispose();
        }

        public void createHttpClient()
        {
            if (Client != null) return;

            /* Temporarily Disabled
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.SslProtocols = SslProtocols.Tls12;
            handler.ClientCertificates.Add(Plugin.Authentification.getCertificate());
            handler.AllowAutoRedirect = true;
            handler.MaxConnectionsPerServer = 2;

            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, error) => { return cert != null && handler.ClientCertificates.Contains(cert); };
            Client = new(handler) { Timeout = TimeSpan.FromSeconds(10) };
            Plugin.PluginLog.Verbose("HttpClient successfully created!");
            UpdateTimer.Interval = normal;

            //UpdateTimer.Elapsed += (sender, e) => sendServerRequest();
            //sendServerLogin();
            */

            ClientClean = new HttpClient();
        }


        public async void sendPishockRequest(Trigger TriggerObject)
        {

            Plugin.PluginLog.Verbose($"{TriggerObject.Name} fired - sending request for {TriggerObject.Shockers.Count} shockers.");
            Plugin.PluginLog.Verbose($" -> Parameters -  {TriggerObject.OpMode} {TriggerObject.Intensity}% for {TriggerObject.Duration}s");

            //Validation of Data
            if (Plugin.Authentification.PishockName.Length < 3
                || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.PluginLog.Verbose(" -> Aborted due to invalid Account Settings!");
                return;
            }

            if (failsafe)
            {
                Plugin.PluginLog.Verbose(" -> Blocked request due to failsafe mode!");
                return;
            }

            if (!TriggerObject.Validate())
            {
                Plugin.PluginLog.Verbose(" -> Blocked due to invalid TriggerObject!");
                return;
            }


            Plugin.PluginLog.Verbose($" -> Data Validated. Creating Requests...");

            foreach (var shocker in TriggerObject.Shockers)
            {
                using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                Username = Plugin.Authentification.PishockName,
                Name = "WoLPlugin",
                Code = shocker,
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
                    Plugin.PluginLog.Error(ex.ToString());
                    Plugin.PluginLog.Error("Error when sending post request to pishock api");
                }
            }
            Plugin.PluginLog.Verbose($" -> Requests sent!");
        }

        public async void sendPishockRequest(Trigger TriggerObject, int[] overrideSettings)
        {

            Plugin.PluginLog.Verbose($"{TriggerObject.Name} fired - sending request for {TriggerObject.Shockers.Count} shockers.");
            Plugin.PluginLog.Verbose($" -> Parameters using Override -  {overrideSettings[0]} {overrideSettings[1]}% for {overrideSettings[2]}s");

            //Validation of Data
            if (Plugin.Authentification.PishockName.Length < 3
                || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.PluginLog.Verbose(" -> Aborted due to invalid Account Settings!");
                return;
            }

            if (failsafe)
            {
                Plugin.PluginLog.Verbose(" -> Blocked request due to failsafe mode!");
                return;
            }

            if (!TriggerObject.Validate())
            {
                Plugin.PluginLog.Verbose(" -> Blocked due to invalid TriggerObject!");
                return;
            }


            Plugin.PluginLog.Verbose($" -> Data Validated. Creating Requests...");

            foreach (var shocker in TriggerObject.Shockers)
            {
                using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                Username = Plugin.Authentification.PishockName,
                Name = "WoLPlugin",
                Code = shocker,
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
                    Plugin.PluginLog.Error(ex.ToString());
                    Plugin.PluginLog.Error("Error when sending post request to pishock api");
                }
            }
            Plugin.PluginLog.Verbose($" -> Requests sent!");
        }

        public async void sendPishockTestAll()
        {
            if (Plugin.Authentification.PishockName.Length < 3
                || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.PluginLog.Verbose(" -> Aborted due to invalid Account Settings!");
                return;
            }
            Plugin.PluginLog.Verbose($" -> Data Validated. Creating Requests...");


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
                    Plugin.PluginLog.Error(ex.ToString());
                    Plugin.PluginLog.Error("Error when sending post request to pishock api");
                }
            }
            Plugin.PluginLog.Verbose($" -> Requests sent!");
        }

        public async void requestPishockInfo(string ShareCode)
        {

            if (Plugin.Authentification.PishockName.Length < 3
               || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.PluginLog.Verbose(" -> Aborted due to invalid Account Settings!");
                return;
            }
            Plugin.PluginLog.Verbose($" -> Data Validated. Creating Requests...");


            using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                Username = Plugin.Authentification.PishockName,
                Code = ShareCode,
                Apikey = Plugin.Authentification.PishockApiKey,

            }),
            Encoding.UTF8,
            "application/json");

            Shocker shocker = Plugin.Authentification.PishockShockers.Find(shocker => shocker.Code ==  ShareCode);
            if (shocker == null ) return;
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
                Plugin.PluginLog.Error(ex.ToString());
                Plugin.PluginLog.Error("Error when sending post request to pishock api");
            }
        }

        public async void requestPishockInfoAll()
        {
            if (Plugin.Authentification.PishockName.Length < 3
                || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.PluginLog.Verbose(" -> Aborted due to invalid Account Settings!");
                return;
            }
            Plugin.PluginLog.Verbose($" -> Data Validated. Creating Requests...");


            foreach (var shocker in Plugin.Authentification.PishockShockers)
            {
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
                    Plugin.PluginLog.Error(ex.ToString());
                    Plugin.PluginLog.Error("Error when sending post request to pishock api");
                }
            }
            Plugin.PluginLog.Verbose($" -> Requests sent!");
        }

        public async void sendWebserverRequest(NetPacket packet)
        {
            Plugin.PluginLog.Verbose(packet.ToString());

        }

        public async void establishWebseverConnection()
        {

            if (Status == ConnectionStatus.Unavailable) return;

            if (Status == ConnectionStatus.Connected ||
                Plugin.ClientState.LocalPlayer == null ||
                Client == null) return;


            NetPacket packet = new NetPacket(Operation.RequestServerState, Plugin.LocalPlayerNameFull);

            string key = Plugin.Authentification.ServerKey;
            if (key.Length == 0) key = "None";

            using StringContent jsonContent = new(
                JsonSerializer.Serialize(new
                {
                    hash = Plugin.Authentification.getHash(),
                    key,
                    devKey = Plugin.Authentification.DevKey,
                    packet,
                }),
            Encoding.UTF8,
            "application/json");

            try
            {
                Plugin.PluginLog.Verbose($"Sending Package");
                Plugin.PluginLog.Verbose(jsonContent.Headers.ToString());
                Stopwatch timeTaken = Stopwatch.StartNew();
                var s = await Client.PostAsync($"https://theheadpatcat.ddns.net/post/WoLightning", jsonContent);
                timeTaken.Stop();
                Ping = timeTaken.ElapsedMilliseconds;
                switch (s.StatusCode)
                {

                    case HttpStatusCode.Accepted:
                        Status = ConnectionStatus.Connected;
                        Plugin.PluginLog.Verbose($"Connection to Server has been Established!");
                        if (s.Content != null) processResponse(packet, s.Content.ToString());
                        break;


                    // Softerrors
                    case HttpStatusCode.Unauthorized:
                        Status = ConnectionStatus.UnknownUser;
                        Plugin.PluginLog.Error("The Server dídnt know us, so we got registered.");
                        if (s.Content != null) processResponse(packet, s.Content.ToString());
                        break;

                    case HttpStatusCode.UpgradeRequired:
                        Status = ConnectionStatus.Outdated;
                        Plugin.PluginLog.Warning("We are running a outdated Version.");
                        break;

                    case HttpStatusCode.Forbidden:
                        Status = ConnectionStatus.InvalidKey;
                        Plugin.PluginLog.Error("Our Key does not match the key on the Serverside.");
                        break;

                    case HttpStatusCode.Locked:


                    // Harderrors
                    case HttpStatusCode.NotFound:
                        Plugin.PluginLog.Error("We sent a invalid Request to the Server.");
                        Status = ConnectionStatus.FatalError;
                        break;
                    case HttpStatusCode.InternalServerError:
                        Status = ConnectionStatus.FatalError;
                        Plugin.PluginLog.Error("We sent a invalid Packet to the Server.");

                        break;

                    default:
                        Status = ConnectionStatus.FatalError;
                        Plugin.PluginLog.Error($"Unknown Response {s.StatusCode}");
                        return;
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Ping = 0;
                Status = ConnectionStatus.WontRespond;
                Plugin.PluginLog.Info("The Server is not responding.");
                return;
            }
            catch (TaskCanceledException ex)
            {
                Plugin.PluginLog.Warning("Running Request was Cancelled.");
                return;
            }
            catch (HttpRequestException)
            {
                Ping = 0;
                Status = ConnectionStatus.WontRespond;
                Plugin.PluginLog.Info("The Server refused the connection.");
                return;
            }
            catch (Exception ex)
            {
                Ping = 0;
                Status = ConnectionStatus.FatalError;
                Client.CancelPendingRequests();
                Plugin.PluginLog.Error(ex.ToString());
                Plugin.PluginLog.Error("A Request threw an error");
                return;
            }

        }

        private void processResponse(NetPacket originalMessage, string jsonString)
        {
            Plugin.PluginLog.Verbose(jsonString);
        }


        private void processPishockResponse(HttpContent response, Shocker shocker)
        {

            shocker.Status = ShockerStatus.Online;
            using (var reader = new StreamReader(response.ReadAsStream()))
            {
                string message = reader.ReadToEnd();
                Plugin.PluginLog.Verbose(message);
                message = message.Replace("\"", "");
                message = message.Replace("{", "");
                message = message.Replace("}", "");
                string[] partsRaw = message.Split(',');
                Dictionary<String, String> headers = new Dictionary<String, String>();
                foreach (var part in partsRaw) headers.Add(part.Split(':')[0], part.Split(':')[1]);

                foreach (var (key, value) in headers) Plugin.PluginLog.Verbose($"{key}:{value}");


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
