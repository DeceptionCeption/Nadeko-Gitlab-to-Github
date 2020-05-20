using Newtonsoft.Json;

namespace SearchesService.Models
{
    /*
     {
        "status": "OK",
        "message": "",
        "countryCode": "RS",
        "countryName": "Serbia",
        "zoneName": "Europe/Belgrade",
        "abbreviation": "CET",
        "gmtOffset": 3600,
        "dst": "0",
        "zoneStart": 1572138000,
        "zoneEnd": 1585443600,
        "nextAbbreviation": "CEST",
        "timestamp": 1577885870,
        "formatted": "2020-01-01 13:37:50"
    }
     */
    public class TimeZoneResult
    {
        [JsonProperty("abbreviation")]
        public string TimezoneName { get; set; }
        [JsonProperty("timestamp")]
        public int Timestamp { get; set; }
    }

    public class LocationIqResponse
    {
        public float Lat { get; set; }
        public float Lon { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }
    }
}
