using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WoLightning
{
    public class WebClient
    {
        private Plugin Plugin;
        public WebClient(Plugin plugin) {
            this.Plugin = plugin;
        }

        public bool failsafe = false;

        private static readonly HttpClient Client = new HttpClient();

        public async void sendRequest(int[] settings)
        {
            Plugin.PluginLog.Info($"(WebClient) Sending Request from {Plugin.Configuration.PishockName} with Mode {settings[0]} and Intensity/Duration: {settings[1]}|{settings[2]}");
            if (failsafe)
            {
                Plugin.PluginLog.Info("Blocked request due to failsafe mode!");
                return;
            }
            using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                Username = Plugin.Configuration.PishockName,
                Name = "WoLPlugin",
                Code = Plugin.Configuration.PishockShareCode,
                Intensity = settings[1],
                Duration = settings[2],
                Apikey = Plugin.Configuration.PishockApiKey,
                Op = settings[0], //0 = shock, 1 = vibrate, 2 = beep
            }),
            Encoding.UTF8,
            "application/json");



            try
            {
                Stopwatch timeTaken = Stopwatch.StartNew();
                await Client.PostAsync("https://do.pishock.com/api/apioperate", jsonContent);
                timeTaken.Stop();
                Plugin.PluginLog.Info("Took " + timeTaken.ElapsedMilliseconds + "ms for the request.");
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error("WoL Error when sending Post reqeuest", ex);
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
