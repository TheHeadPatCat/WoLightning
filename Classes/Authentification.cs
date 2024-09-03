using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WoLightning.Types;

namespace WoLightning.Classes
{

    public class Authentification : IDisposable // This class is here to make sure the data that gets received from the server, is actually from this plugin (well not entirely, but it helps)
    {

        public int Version { get; set; } = 401;
        private string? ConfigurationDirectoryPath { get; init; }

        // Webserver things
        private string Hash = string.Empty;
        public string ServerKey { get; set; } = string.Empty;

        // Pishock things
        public string PishockName { get; set; } = string.Empty;
        public string PishockShareCode { get; set; } = string.Empty;
        public List<Shocker> PishockShockers { get; set; } = [];
        public string PishockApiKey { get; set; } = string.Empty;





        // Mastermode - Authentification Process

        [NonSerialized]
        public string? errorStringMaster = string.Empty;

        [NonSerialized]
        public bool isRequesting = false;
        [NonSerialized]
        public Player? targetMaster;

        [NonSerialized]
        public bool gotRequest = false;
        [NonSerialized]
        public Player? targetSub;
        



        // Mastermode - Master Settings
        public bool IsMaster { get; set; } = false;
        public Dictionary<string, Player> OwnedSubs { get; set; } = [];

        // Mastermode - Sub Settings
        public bool HasMaster { get; set; } = false;
        public Player? Master { get; set; }
        public bool isDisallowed { get; set; } = false; //locks the interface


        public string DevKey { get; set; } = string.Empty;


        public Authentification() { }
        public Authentification(string ConfigDirectoryPath)
        {
            ConfigurationDirectoryPath = ConfigDirectoryPath;

            string f = "";
            if (File.Exists(ConfigurationDirectoryPath + "authentification.json")) f = File.ReadAllText(ConfigurationDirectoryPath + "authentification.json");
            Authentification s = DeserializeAuthentification(f);
            foreach (PropertyInfo property in typeof(Authentification).GetProperties().Where(p => p.CanWrite)) property.SetValue(this, property.GetValue(s, null), null);
            Save();
        }
        public Authentification(string ConfigDirectoryPath, bool delete)
        {
            ConfigurationDirectoryPath = ConfigDirectoryPath;
            Save();
        }

        public string getHash()
        {
            if (Hash.Length > 0) return Hash;

            byte[] arr = Encoding.ASCII.GetBytes(Plugin.currentVersion + Plugin.randomKey);
            byte[] hashed = SHA256.Create().ComputeHash(arr);


            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hashed.Length; i++)
            {
                builder.Append(hashed[i].ToString("x2"));
            }

            Hash = builder.ToString();

            return Hash;
        }

        public X509Certificate2 getCertificate()
        {
            X509Certificate2 certificate = new();
            if (!File.Exists(ConfigurationDirectoryPath + "/cert.pem"))
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://theheadpatcat.ddns.net");

                    request.ServerCertificateValidationCallback = (sender, cert, chain, error) =>
                    {
                        File.WriteAllBytes(ConfigurationDirectoryPath + "/cert.pem", cert.GetRawCertData());
                        certificate = new X509Certificate2(cert.GetRawCertData());
                        return true;
                    };
                    WebResponse response = request.GetResponse(); //will be 404
                }
                catch
                {
                }
            }
            else
            {
                certificate = new X509Certificate2(File.ReadAllBytes(ConfigurationDirectoryPath + "/cert.pem"));
            }
            return certificate;
        }

        public void Save()
        {
            File.WriteAllText(ConfigurationDirectoryPath + "Authentification.json", SerializeAuthentification(this));
        }

        public void Dispose()
        {
            Save();
        }

        internal static string SerializeAuthentification(object? config)
        {
            return JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        private Authentification DeserializeAuthentification(string input)
        {
            if (input == "") return new Authentification();
            return JsonConvert.DeserializeObject<Authentification>(input);
        }
    }
}
