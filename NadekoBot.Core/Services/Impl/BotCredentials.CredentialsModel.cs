namespace NadekoBot.Core.Services.Impl
{
    public partial class BotCredentials
    {
        private class CredentialsModel
        {
            public ulong ClientId { get; set; } = 123123123;
            public string Token { get; set; } = "";
            public ulong[] OwnerIds { get; set; } = new ulong[1];
            public string LoLApiKey { get; set; } = "";
            public string GoogleApiKey { get; set; } = "";
            public string MashapeKey { get; set; } = "";
            public string OsuApiKey { get; set; } = "";
            public string SoundCloudClientId { get; set; } = "";
            public string CleverbotApiKey { get; } = "";
            public string CarbonKey { get; set; } = "";
            public DBConfig Db { get; set; } = new DBConfig("sqlite", "Data Source=data/NadekoBot.db");
            public int TotalShards { get; set; } = 1;
            public string PatreonAccessToken { get; set; } = "";
            public string PatreonCampaignId { get; set; } = "334038";
            public string RestartCommand { get; set; } = null;

            public string ShardRunCommand { get; set; } = "";
            public string ShardRunArguments { get; set; } = "";
            public int? ShardRunPort { get; set; } = null;
            public string MiningProxyUrl { get; set; } = null;
            public string MiningProxyCreds { get; set; } = null;

            public string BotListToken { get; set; }
            public string TwitchClientId { get; set; }
            public string VotesToken { get; set; }
            public string VotesUrl { get; set; }
            public string RedisOptions { get; set; }
            public string ServicesIp { get; set; }
        }
    }
}
