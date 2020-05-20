using Nadeko.Common.Yml;
using Newtonsoft.Json;

namespace SearchesService.Models
{
    public struct SearchesCreds : IInitializable
    {
        [Comment(@"Api key obtained on https://rapidapi.com (go to MyApps -> Add New App -> Enter Name -> Application key)")]
        [JsonProperty("mashape_api_key")]
        public string RapidApiKey { get; set; }

        [Comment(@"https://locationiq.com api key (register and you will receive the token in the email).
Used only for .time command.")]
        public string LocationIqApiKey { get; set; }

        [Comment(@"https://timezonedb.com api key (register and you will receive the token in the email).
Used only for .time command")]
        public string TimezoneDbApiKey { get; set; }

        [Comment(@"https://pro.coinmarketcap.com/account/ api key. There is a free plan for personal use.
Used for cryptocurrency related commands.")]
        public string CoinmarketcapApiKey { get; set; }

        public void Initialize()
        {
            RapidApiKey = string.Empty;
            LocationIqApiKey = string.Empty;
            TimezoneDbApiKey = string.Empty;
            CoinmarketcapApiKey = string.Empty;
        }
    }
}
