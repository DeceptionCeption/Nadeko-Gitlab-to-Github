using Discord;
using Microsoft.Extensions.Configuration;
using NadekoBot.Common;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace NadekoBot.Core.Services.Impl
{
    public partial class BotCredentials : IBotCredentials
    {
        private Logger _log;

        public ulong ClientId { get; private set; }
        public string GoogleApiKey { get; private set; }
        public string MashapeKey { get; private set; }
        public string Token { get; private set; }

        public ImmutableArray<ulong> OwnerIds { get; private set; }

        public string LoLApiKey { get; private set; }
        public string OsuApiKey { get; private set; }
        public string CleverbotApiKey { get; private set; }
        public RestartConfig RestartCommand { get; private set; }
        public DBConfig Db { get; private set; }
        public int TotalShards { get; private set; }
        public string CarbonKey { get; private set; }

        private readonly string _credsFileName = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");
        public string PatreonAccessToken { get; private set; }
        public string ShardRunCommand { get; private set; }
        public string ShardRunArguments { get; private set; }
        public int ShardRunPort { get; private set; }

        public string PatreonCampaignId { get; private set; }
        public string MiningProxyUrl { get; private set; }
        public string MiningProxyCreds { get; private set; }

        public string TwitchClientId { get; private set; }

        public string VotesUrl { get; private set; }
        public string VotesToken { get; private set; }
        public string BotListToken { get; private set; }
        public string RedisOptions { get; private set; }
        public string LocationIqApiKey { get; private set; }
        public string TimezoneDbApiKey { get; private set; }
        public string ServicesIp { get; private set; }
        public string CoinmarketcapApiKey { get; private set; }

        public BotCredentials()
        {
            _log = LogManager.GetCurrentClassLogger();

            Reload();
        }

        public void Reload()
        {
            try { File.WriteAllText("./credentials_example.json", JsonConvert.SerializeObject(new CredentialsModel(), Formatting.Indented)); } catch { }
            if (!File.Exists(_credsFileName))
                _log.Warn($"credentials.json is missing. Attempting to load creds from environment variables prefixed with 'NadekoBot_'. Example is in {Path.GetFullPath("./credentials_example.json")}");
            try
            {
                var configBuilder = new ConfigurationBuilder();
                configBuilder.AddJsonFile(_credsFileName, true)
                    .AddEnvironmentVariables("NadekoBot_");

                var data = configBuilder.Build();

                Token = data[nameof(Token)];
                if (string.IsNullOrWhiteSpace(Token))
                {
                    _log.Error("Token is missing from credentials.json or Environment varibles. Add it and restart the program.");
                    if (!Console.IsInputRedirected)
                        Console.ReadKey();
                    Environment.Exit(3);
                }
                OwnerIds = data.GetSection("OwnerIds").GetChildren().Select(c => ulong.Parse(c.Value)).ToImmutableArray();
                LoLApiKey = data[nameof(LoLApiKey)];
                GoogleApiKey = data[nameof(GoogleApiKey)];
                MashapeKey = data[nameof(MashapeKey)];
                OsuApiKey = data[nameof(OsuApiKey)];
                PatreonAccessToken = data[nameof(PatreonAccessToken)];
                PatreonCampaignId = data[nameof(PatreonCampaignId)] ?? "334038";
                ShardRunCommand = data[nameof(ShardRunCommand)];
                ShardRunArguments = data[nameof(ShardRunArguments)];
                CleverbotApiKey = data[nameof(CleverbotApiKey)];
                MiningProxyUrl = data[nameof(MiningProxyUrl)];
                MiningProxyCreds = data[nameof(MiningProxyCreds)];
                LocationIqApiKey = data[nameof(LocationIqApiKey)];
                TimezoneDbApiKey = data[nameof(TimezoneDbApiKey)];
                CoinmarketcapApiKey = data[nameof(CoinmarketcapApiKey)];
                if(string.IsNullOrWhiteSpace(CoinmarketcapApiKey))
                {
                    CoinmarketcapApiKey = "e79ec505-0913-439d-ae07-069e296a6079";
                }
                if (!string.IsNullOrWhiteSpace(data[nameof(RedisOptions)]))
                    RedisOptions = data[nameof(RedisOptions)];
                else
                    RedisOptions = "127.0.0.1,syncTimeout=3000";

                VotesToken = data[nameof(VotesToken)];
                VotesUrl = data[nameof(VotesUrl)];
                BotListToken = data[nameof(BotListToken)];
                ServicesIp = data[nameof(ServicesIp)];

                var restartSection = data.GetSection(nameof(RestartCommand));
                var cmd = restartSection["cmd"];
                var args = restartSection["args"];
                if (!string.IsNullOrWhiteSpace(cmd))
                    RestartCommand = new RestartConfig(cmd, args);

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    if (string.IsNullOrWhiteSpace(ShardRunCommand))
                        ShardRunCommand = "dotnet";
                    if (string.IsNullOrWhiteSpace(ShardRunArguments))
                        ShardRunArguments = "run -c Release --no-build -- {0} {1}";
                }
                else //windows
                {
                    if (string.IsNullOrWhiteSpace(ShardRunCommand))
                        ShardRunCommand = "NadekoBot.exe";
                    if (string.IsNullOrWhiteSpace(ShardRunArguments))
                        ShardRunArguments = "{0} {1}";
                }

                var portStr = data[nameof(ShardRunPort)];
                if (string.IsNullOrWhiteSpace(portStr))
                    ShardRunPort = new NadekoRandom().Next(5000, 6000);
                else
                    ShardRunPort = int.Parse(portStr);

                if (!int.TryParse(data[nameof(TotalShards)], out var ts))
                    ts = 0;
                TotalShards = ts < 1 ? 1 : ts;

                if (!ulong.TryParse(data[nameof(ClientId)], out ulong clId))
                    clId = 0;
                ClientId = clId;

                CarbonKey = data[nameof(CarbonKey)];
                var dbSection = data.GetSection("db");
                Db = new DBConfig(string.IsNullOrWhiteSpace(dbSection["Type"])
                                ? "sqlite"
                                : dbSection["Type"],
                            string.IsNullOrWhiteSpace(dbSection["ConnectionString"])
                                ? "Data Source=data/NadekoBot.db"
                                : dbSection["ConnectionString"]);

                TwitchClientId = data[nameof(TwitchClientId)];
                if (string.IsNullOrWhiteSpace(TwitchClientId))
                {
                    TwitchClientId = "67w6z9i09xv2uoojdm9l0wsyph4hxo6";
                }
            }
            catch (Exception ex)
            {
                _log.Fatal(ex.Message);
                _log.Fatal(ex);
                throw;
            }

        }
        public bool IsOwner(IUser u) => OwnerIds.Contains(u.Id);
    }
}
