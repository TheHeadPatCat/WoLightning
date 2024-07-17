using Dalamud.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WoLightning
{
    public class WebClient : IDisposable
    {
        private readonly Plugin Plugin;
        public long Ping { get; set; } = -1;
        private DateTime lastShock = DateTime.MinValue;
        private DateTime passThroughLeniancy = DateTime.MinValue;
        public string ConnectionStatus { get; set; } = "not started";
        public readonly TimerPlus UpdateTimer = new TimerPlus();
        private readonly double fast = new TimeSpan(0, 0, 2).TotalMilliseconds;
        private readonly double normal = new TimeSpan(0,0,15).TotalMilliseconds;
        private readonly double slow = new TimeSpan(0, 5, 0).TotalMilliseconds;
        
        public bool failsafe { get; set; } = false;
        private bool isFirstMessage = true;

        private HttpClient? Client;
        private HttpClient? ClientClean;
        public WebClient(Plugin plugin)
        {
            this.Plugin = plugin;
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

            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.SslProtocols = SslProtocols.Tls12;
            handler.ClientCertificates.Add(Plugin.Authentification.getCertificate());
            handler.AllowAutoRedirect = true;
            handler.MaxConnectionsPerServer = 2;

            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, error) =>{ return (cert != null) && handler.ClientCertificates.Contains(cert); };
            Client = new(handler){Timeout = TimeSpan.FromSeconds(10)};
            Plugin.PluginLog.Verbose("HttpClient successfully created!");
            UpdateTimer.Interval = normal;
            UpdateTimer.Elapsed += (sender, e) => sendServerRequest();
            sendServerLogin();
            ClientClean = new HttpClient();
        }


        public async void sendRequestShock(int[] settings) // todo redo this
        {
            Plugin.PluginLog.Verbose($"Sending Pishock Request: Mode {settings[0]} Intensity/Duration: {settings[1]}|{settings[2]}");
            if (failsafe)
            {
                Plugin.PluginLog.Verbose(" -> Blocked request due to failsafe mode!");
                return;
            }
            if (settings.Length < 3 || settings[0] < 0 || settings[0] > 2 || settings[1] < 1 || settings[2] < 1) return; // dont send bad data
            if (Plugin.Authentification.PishockName.Length < 3 || Plugin.Authentification.PishockShareCode.Length < 3 || Plugin.Authentification.PishockApiKey.Length < 16) return;

            if (lastShock.Ticks + Plugin.Configuration.globalTriggerCooldown * 10000000  > DateTime.Now.Ticks // Cooldown
                && lastShock.Ticks + 7500000 < DateTime.Now.Ticks) // 0.75 Second leniancy to allow passthrough
            {
                Plugin.PluginLog.Verbose(" -> Blocked due to Cooldown!");
                return;
            }
            lastShock = DateTime.Now;

            using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                Username = Plugin.Authentification.PishockName,
                Name = "WoLPlugin",
                Code = Plugin.Authentification.PishockShareCode,
                Intensity = settings[1],
                Duration = settings[2],
                Apikey = Plugin.Authentification.PishockApiKey,
                Op = settings[0], //0 = shock, 1 = vibrate, 2 = beep
            }),
            Encoding.UTF8,
            "application/json");

            try
            {
                Plugin.PluginLog.Verbose(" -> Sent!");
                Stopwatch timeTaken = Stopwatch.StartNew();
                //await ClientClean.PostAsync("https://do.pishock.com/api/apioperate", jsonContent);
                timeTaken.Stop();
                Plugin.PluginLog.Verbose(" -> Response Time: " + timeTaken.ElapsedMilliseconds + "ms.");
                
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error(ex.ToString());
                Plugin.PluginLog.Error("Error when sending post request to pishock api");
            }
        }




        public void sendServerData(string type, string data)
        {
            sendServerData(new NetworkPacket(type, data));
        }
        public void sendServerData(string[] type, string[] data)
        {
            sendServerData(new NetworkPacket(type, data));
        }

        public async void sendServerData(NetworkPacket? packet)
        {
            Plugin.PluginLog.Info("Sending Data to Server:");
            Plugin.PluginLog.Info(packet.ToString());
            if (ConnectionStatus != "connected")
            {
                Plugin.PluginLog.Warning("Attempted to send a Request, while we arent connected to the Server!");
                sendServerLogin();
                return;
            }

            if (Plugin.ClientState.LocalPlayer == null)
            {
                Plugin.PluginLog.Info($"Aborting because Player doesnt exist yet.");
                return;
            }
            if (Client == null)
            {
                ConnectionStatus = "cant connect";
                Plugin.PluginLog.Error($"We cant send data without a Client!!");
                return;
            }

            using StringContent jsonContent = new(
                JsonSerializer.Serialize(new
                {
                    hash = Plugin.Authentification.getHash(),
                    key = Plugin.Authentification.ServerKey,
                    playerNameFull = Plugin.ClientState.LocalPlayer.Name + "#" + Plugin.ClientState.LocalPlayer.HomeWorld.Id,
                    masterNameFull = Plugin.Configuration.MasterNameFull,
                    data = packet.ToString(),
                    isFirstMessage
                }),
            Encoding.UTF8,
            "application/json");

            try
            {
                
                Stopwatch timeTaken = Stopwatch.StartNew();
                var s = await Client.PostAsync($"https://theheadpatcat.ddns.net/post/", jsonContent);
                timeTaken.Stop();
                isFirstMessage = false;
                Ping = timeTaken.ElapsedMilliseconds;
                if (UpdateTimer.Interval > normal){ConnectionStatus = "connected"; UpdateTimer.Interval = normal;}

                switch (s.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        Plugin.PluginLog.Error("We sent a invalid Request to the Server.");
                        break;

                    case HttpStatusCode.UpgradeRequired:
                        ConnectionStatus = "outdated";
                        Plugin.PluginLog.Warning("We are running a outdated Version.");
                        UpdateTimer.Stop();
                        break;

                    case HttpStatusCode.Unauthorized:
                        ConnectionStatus = "cant connect";
                        Plugin.PluginLog.Error("Server doesnt know us. Did we skip the login?");
                        UpdateTimer.Stop();
                        break;

                    case HttpStatusCode.Forbidden:
                        ConnectionStatus = "invalid key";
                        Plugin.PluginLog.Error("Our Key does not match the key on the Serverside");
                        UpdateTimer.Stop();
                        break;

                    case HttpStatusCode.InternalServerError:
                        Plugin.PluginLog.Error("We sent a invalid Packet to the Server.");
                        break;

                    case HttpStatusCode.Accepted:
                        Plugin.PluginLog.Verbose($"The Server Accepted our Request.");
                        break;

                    default:
                        ConnectionStatus = "cant connect";
                        Plugin.PluginLog.Error($"Unknown Response {s.StatusCode}");
                        UpdateTimer.Stop();
                        return;
                }

            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Ping = 0;
                ConnectionStatus = "disconnected";
                UpdateTimer.Interval = slow;
                UpdateTimer.Refresh();
                Client.CancelPendingRequests();
                Plugin.PluginLog.Info("The Server is not responding.");
                return;
            }
            catch (TaskCanceledException ex)
            {
                Plugin.PluginLog.Info("Running Request was Cancelled.");
                return;
            }
            catch (HttpRequestException)
            {
                Ping = 0;
                ConnectionStatus = "disconnected";
                UpdateTimer.Interval = slow;
                UpdateTimer.Refresh();
                Client.CancelPendingRequests();
                Plugin.PluginLog.Info("The Server refused the connection.");
                return;
            }
            catch (Exception ex)
            {
                Ping = 0;
                ConnectionStatus = "cant connect";
                UpdateTimer.Stop();
                Client.CancelPendingRequests();
                Plugin.PluginLog.Error(ex.ToString());
                Plugin.PluginLog.Error("A Request threw an error");
                return;
            }
            return;
        }



        public async void sendServerRequest()
        {
            if (ConnectionStatus != "connected")
            {
                Plugin.PluginLog.Warning("Attempted to send a Request, while we arent connected to the Server!");
                sendServerLogin();
                return;
            }

            if (Plugin.ClientState.LocalPlayer == null)
            {
                Plugin.PluginLog.Info($"Aborting because Player doesnt exist yet.");
                return;
            }
            if (Client == null)
            {
                ConnectionStatus = "cant connect";
                Plugin.PluginLog.Error($"We cant send data without a Client!!");
                return;
            }

            using StringContent jsonContent = new(
                JsonSerializer.Serialize(new
                {
                    hash = Plugin.Authentification.getHash(),
                    key = Plugin.Authentification.ServerKey,
                    playerNameFull = Plugin.ClientState.LocalPlayer.Name + "#" + Plugin.ClientState.LocalPlayer.HomeWorld.Id,
                    masterNameFull = Plugin.Configuration.MasterNameFull,
                    pStatus = Plugin.NetworkWatcher.running
                }),
            Encoding.UTF8,
            "application/json");

            try
            {
                Stopwatch timeTaken = Stopwatch.StartNew();
                var s = await Client.PostAsync($"https://theheadpatcat.ddns.net/request/", jsonContent);
                timeTaken.Stop();
                isFirstMessage = false;
                Ping = timeTaken.ElapsedMilliseconds;

                if (UpdateTimer.Interval > normal) { ConnectionStatus = "connected"; UpdateTimer.Interval = normal; }
                UpdateTimer.Refresh();

                if (s == null) return;
                if (s.Content == null) return;
                switch (s.StatusCode)
                {
                    case HttpStatusCode.OK: // Update - Received Data
                        string response = Encoding.UTF8.GetString(await s.Content.ReadAsByteArrayAsync());
                        Plugin.PluginLog.Info($"Code: {s.StatusCode}   Response: {response}");
                        new NetworkPacket(response).resolve(Plugin);
                        break;

                    case HttpStatusCode.NoContent: // Update - No Data
                        // Do Nothing
                        break;

                    case HttpStatusCode.Accepted:
                        Plugin.PluginLog.Info("Successfully registered!");
                        break;

                    case HttpStatusCode.Forbidden:
                        ConnectionStatus = "invalid key";
                        Plugin.PluginLog.Error("Our Key does not match the Key on the Serverside.");
                        UpdateTimer.Stop();
                        break;

                    case HttpStatusCode.Unauthorized:
                        Plugin.PluginLog.Info("We are still processing the Key...");
                        UpdateTimer.Refresh();
                        break;

                    case HttpStatusCode.UpgradeRequired:
                        ConnectionStatus = "outdated";
                        Plugin.PluginLog.Warning("We are running a outdated version.");
                        UpdateTimer.Stop();
                        break;

                    default:
                        ConnectionStatus = "cant connect";
                        Plugin.PluginLog.Error($"Unknown Response {s.StatusCode}");
                        UpdateTimer.Stop();
                        return;
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Ping = 0;
                ConnectionStatus = "disconnected";
                UpdateTimer.Interval = slow;
                UpdateTimer.Refresh();
                Client.CancelPendingRequests();
                Plugin.PluginLog.Info("The Server is not responding.");
                return;
            }
            catch (TaskCanceledException ex)
            {
                Plugin.PluginLog.Info("Running Request was Cancelled.");
                return;
            }
            catch (HttpRequestException)
            {
                Ping = 0;
                ConnectionStatus = "disconnected";
                UpdateTimer.Interval = slow;
                UpdateTimer.Refresh();
                Client.CancelPendingRequests();
                Plugin.PluginLog.Info("The Server refused the connection.");
                return;
            }
            catch (Exception ex)
            {
                Ping = 0;
                ConnectionStatus = "cant connect";
                UpdateTimer.Stop();
                Client.CancelPendingRequests();
                Plugin.PluginLog.Error(ex.ToString());
                Plugin.PluginLog.Error("A Request threw an error");
                return;
            }
            return;
        }


        public async void sendServerLogin()
        {
            ConnectionStatus = "connecting";

            if (Plugin.ClientState.LocalPlayer == null)
            {
                ConnectionStatus = "cant connect";
                Plugin.PluginLog.Info($"Aborting because Player doesnt exist yet.");
                return;
            }
            if (Client == null)
            {
                ConnectionStatus = "cant connect";
                Plugin.PluginLog.Error($"We cant send data without a Client!!");
                return;
            }

            using StringContent jsonContent = new(
                JsonSerializer.Serialize(new
                {
                    hash = Plugin.Authentification.getHash(),
                    key = Plugin.Authentification.ServerKey,
                    playerNameFull = Plugin.ClientState.LocalPlayer.Name + "#" + Plugin.ClientState.LocalPlayer.HomeWorld.Id,
                    masterNameFull = Plugin.Configuration.MasterNameFull,
                    pStatus = Plugin.NetworkWatcher.running
                }),
            Encoding.UTF8,
            "application/json");

            try
            {
                Stopwatch timeTaken = Stopwatch.StartNew();
                var s = await Client.PostAsync($"https://theheadpatcat.ddns.net/login/", jsonContent);
                timeTaken.Stop();
                Ping = timeTaken.ElapsedMilliseconds;

                if (s == null) return;
                if (s.Content == null) return;
                string response = Encoding.UTF8.GetString(await s.Content.ReadAsByteArrayAsync());
                switch (s.StatusCode)
                {
                    case HttpStatusCode.Found: // Logged in!
                        Plugin.PluginLog.Info($"Successfully Logged in!");
                        ConnectionStatus = "connected";
                        UpdateTimer.Interval = normal;
                        UpdateTimer.Start();
                        break;

                    case HttpStatusCode.Created:
                        Plugin.PluginLog.Info("Successfully registered!");
                        ConnectionStatus = "connected";
                        Plugin.PluginLog.Info($"Code: {s.StatusCode}   Response: {response}");
                        new NetworkPacket(response).resolve(Plugin);
                        UpdateTimer.Interval = normal;
                        UpdateTimer.Start();
                        break;

                    case HttpStatusCode.Forbidden:
                        ConnectionStatus = "invalid key";
                        Plugin.PluginLog.Error("Our Key does not match the Key on the Serverside.");
                        break;

                    case HttpStatusCode.UpgradeRequired:
                        ConnectionStatus = "outdated";
                        Plugin.PluginLog.Error("We are running a outdated version.");
                        break;

                    default:
                        ConnectionStatus = "cant connect";
                        Plugin.PluginLog.Error($"Unknown Response {s.StatusCode}");
                        return;
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Ping = 0;
                ConnectionStatus = "disconnected";
                UpdateTimer.Interval = slow;
                UpdateTimer.Refresh();
                Client.CancelPendingRequests();
                Plugin.PluginLog.Info("The Server is not responding.");
                return;
            }
            catch (TaskCanceledException ex)
            {
                Plugin.PluginLog.Info("Running Request was Cancelled.");
                return;
            }
            catch (HttpRequestException)
            {
                Ping = 0;
                ConnectionStatus = "disconnected";
                UpdateTimer.Interval = slow;
                UpdateTimer.Refresh();
                Client.CancelPendingRequests();
                Plugin.PluginLog.Info("The Server refused the connection.");
                return;
            }
            catch (Exception ex)
            {
                Ping = 0;
                ConnectionStatus = "cant connect";
                UpdateTimer.Stop();
                Client.CancelPendingRequests();
                Plugin.PluginLog.Error(ex.ToString());
                Plugin.PluginLog.Error("A Request threw an error");
                return;
            }
            return;
        }



        // This is debug function
        public async void sendUpdateHash()
        {
            using StringContent jsonContent2 = new(
                   JsonSerializer.Serialize(new
                   {
                       hash = Plugin.Authentification.getHash(),
                       key = Plugin.devHash(),
                   }),
               Encoding.UTF8,
               "application/json");

            try
            {
                Stopwatch timeTaken = Stopwatch.StartNew();
                var s = await Client.PostAsync($"https://theheadpatcat.ddns.net/hashUpdate/", jsonContent2);
                timeTaken.Stop();
                Plugin.PluginLog.Info("Took " + timeTaken.ElapsedMilliseconds + "ms for the request.");
                Plugin.PluginLog.Info($"Code: {s.StatusCode}  Response: {s}");
                return;
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error(ex.ToString());
                Plugin.PluginLog.Error("WoL Error when sending Post reqeuest", ex);
                return;
            }
        }

        // This is debug function
        public async void sendResetUserdata(string input)
        {
            using StringContent jsonContent2 = new(
                   JsonSerializer.Serialize(new
                   {
                       hash = Plugin.Authentification.getHash(),
                       key = input,
                       playerNameFull = Plugin.Configuration.LocalPlayerNameFull
                   }),
               Encoding.UTF8,
               "application/json");

            try
            {
                Stopwatch timeTaken = Stopwatch.StartNew();
                var s = await Client.PostAsync($"https://theheadpatcat.ddns.net/resetUserdata/", jsonContent2);
                timeTaken.Stop();
                if(s.StatusCode == HttpStatusCode.OK)
                {
                    Client.CancelPendingRequests();
                    Plugin.Authentification.ServerKey = "";
                    isFirstMessage = true;
                    sendServerData("hello", "hello");
                }
                Plugin.PluginLog.Info("Took " + timeTaken.ElapsedMilliseconds + "ms for the request.");
                Plugin.PluginLog.Info($"Code: {s.StatusCode}  Response: {s}");
                return;
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error(ex.ToString());
                Plugin.PluginLog.Error("WoL Error when sending Post reqeuest", ex);
                return;
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
