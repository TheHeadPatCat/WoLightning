using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WoLightning.Types;
using System.Text.Json;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using System.Security.AccessControl;

namespace WoLightning.Classes
{
    public class ClientPishock : IDisposable
    {
        public enum ConnectionStatusPishock
        {
            NotStarted = 0,
            Unavailable = 1,

            Connecting = 199,
            Connected = 200,
        }


        private Plugin Plugin;
        public ConnectionStatusPishock Status { get; set; } = ConnectionStatusPishock.NotStarted;

        private HttpClient? Client;
        public ClientPishock(Plugin plugin)
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
        }
        public void createHttpClient()
        {
            if (Client != null) return;

            Client = new HttpClient();
            infoAll();
        }

        public void cancelPendingRequests()
        {
            Client.CancelPendingRequests();
        }

        public async void request(Trigger TriggerObject){ request(TriggerObject, null, null); }
        public async void request(Trigger TriggerObject, string overrideNotif) { request(TriggerObject, overrideNotif, null); }
        public async void request(Trigger TriggerObject, int[] overrideSettings) { request(TriggerObject, null, overrideSettings); }
        public async void request(Trigger TriggerObject, string? overrideNotif, int[]? overrideSettings) 
        {
            Plugin.Log($"{TriggerObject.Name} fired - sending request for {TriggerObject.Shockers.Count} shockers.");
            
            if(overrideSettings != null) Plugin.Log($" -> Override Parameters -  {overrideSettings[0]} {overrideSettings[1]}% for {overrideSettings[2]}s");
            else Plugin.Log($" -> Parameters -  {TriggerObject.OpMode} {TriggerObject.Intensity}% for {TriggerObject.Duration}s");

            //Validation of Data
            #region Validation
            if (Plugin.Authentification.PishockName.Length < 3
                || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.Log(" -> Aborted due to invalid Account Settings!");
                return;
            }

            if (Plugin.isFailsafeActive)
            {
                Plugin.Log(" -> Blocked request due to failsafe mode!");
                return;
            }

            if (!TriggerObject.Validate())
            {
                Plugin.Log(" -> Blocked due to invalid TriggerObject!");
                return;
            }

            if (TriggerObject.hasCooldown())
            {
                Plugin.Log($" -> Blocked due to Cooldown!\n{TriggerObject.CooldownTimer.TimeLeft / 1000}");
                return;
            }

            Plugin.Log($" -> Data Validated. Creating Requests...");

            #endregion Validation

            if (overrideNotif != null) Plugin.sendNotif(overrideNotif);
            else if (TriggerObject.NotifMessage != null) Plugin.sendNotif(TriggerObject.NotifMessage);

            TriggerObject.startCooldown();
            if (Plugin.Configuration.ActivePreset.showCooldownNotifs && TriggerObject.Cooldown > 1)
            {
                Notification result = new Notification();
                int calc = TriggerObject.Cooldown;
                if (TriggerObject.Duration <= 10) calc += TriggerObject.Duration;
                result.InitialDuration = new TimeSpan(0, 0, calc);

                result.Title = "Warrior of Lighting";
                result.Type = NotificationType.Info;
                result.Content = $"{TriggerObject.Name} Cooldown";
                Plugin.NotificationManager.AddNotification(result);
            }

            List<Shocker> saveCopy = TriggerObject.Shockers;
            foreach (var shocker in saveCopy)
            {
                StringContent jsonContent;
                if (overrideSettings != null)
                {
                    jsonContent = new(
                    JsonSerializer.Serialize(new
                    {
                        Username = Plugin.Authentification.PishockName,
                        Name = "WoLPlugin",
                        Code = shocker.Code,
                        Intensity = overrideSettings[1],
                        Duration = overrideSettings[2],
                        Apikey = Plugin.Authentification.PishockApiKey,
                        Op = (int)overrideSettings[0],
                    }),
                    Encoding.UTF8,
                    "application/json");
                }
                else
                {
                    jsonContent = new(
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
                }

                try { Client.PostAsync("https://do.pishock.com/api/apioperate", jsonContent); }
                catch (Exception ex)
                {
                    Plugin.Error(ex.ToString());
                    Plugin.Error("Error when sending post request to pishock api");
                }
            }

            Plugin.Log($" -> Requests sent!");
            

            

        }


        public async void testAll()
        {

            infoAll();

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
                    await Client.PostAsync("https://do.pishock.com/api/apioperate", jsonContent);
                }
                catch (Exception ex)
                {
                    Plugin.Error(ex.ToString());
                    Plugin.Error("Error when sending post request to pishock api");
                }
            }
            Plugin.Log($" -> Requests sent!");
        }

        public async void info(string ShareCode)
        {

            Plugin.Log($"Requesting Information for {ShareCode}...");

            Shocker? shocker = Plugin.Authentification.PishockShockers.Find(shocker => shocker.Code == ShareCode);
            if (shocker == null)
            {
                Plugin.Log(" -> Aborted as the Shocker couldnt be found!");
                return;
            }

            if (Plugin.Authentification.PishockName.Length < 3
               || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.Log(" -> Aborted due to invalid Account Settings!");
                shocker.Status = ShockerStatus.InvalidUser;
                return;
            }

            Plugin.Log($" -> Data Validated. Creating Request...");

            using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                Username = Plugin.Authentification.PishockName,
                Code = ShareCode,
                Apikey = Plugin.Authentification.PishockApiKey,

            }),
            Encoding.UTF8,
            "application/json");

            shocker.Status = ShockerStatus.Unchecked;
            try
            {
                var s = await Client.PostAsync("https://do.pishock.com/api/GetShockerInfo", jsonContent);
                switch (s.StatusCode)
                {
                    case HttpStatusCode.OK:
                        processPishockResponse(s.Content, shocker);
                        break;

                    case HttpStatusCode.NotFound:
                        shocker.Status = ShockerStatus.DoesntExist;
                        break;

                    default:
                        shocker.Status = ShockerStatus.NotAuthorized;
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Error(ex.ToString());
                Plugin.Error("Error when sending post request to pishock api");
                shocker.Status = ShockerStatus.InvalidUser;
            }
        }

        public async void infoAll()
        {
            Plugin.Log($"Requesting Information for all Shockers.");

            if (Plugin.Authentification.PishockName.Length < 3
                || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.Log(" -> Aborted due to invalid Account Settings!");
                foreach (var shocker in Plugin.Authentification.PishockShockers)
                {
                    shocker.Status = ShockerStatus.InvalidUser;
                }
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
                    var s = await Client.PostAsync("https://do.pishock.com/api/GetShockerInfo", jsonContent);
                    Status = ConnectionStatusPishock.Connected;
                    switch (s.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            processPishockResponse(s.Content, shocker);
                            break;

                        case HttpStatusCode.NotFound:
                            shocker.Status = ShockerStatus.DoesntExist;
                            break;

                        default:
                            shocker.Status = ShockerStatus.NotAuthorized;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Error(ex.ToString());
                    Plugin.Error("Error when sending post request to pishock api");
                    Status = ConnectionStatusPishock.Unavailable;
                }
            }
            Plugin.Log($" -> Requests sent!");
        }



        private void processPishockResponse(HttpContent response)
        {
            Status = ConnectionStatusPishock.Connected;
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
            Status = ConnectionStatusPishock.Connected;
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

                if (headers.ContainsKey("name")) shocker.Name = headers["name"];
                if (headers.ContainsKey("paused") && bool.Parse(headers["paused"]) == true) shocker.Status = ShockerStatus.Paused;
            }

        }

    }
}
