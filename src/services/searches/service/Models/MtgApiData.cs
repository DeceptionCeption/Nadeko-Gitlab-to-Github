using Newtonsoft.Json;
using System.Collections.Generic;

namespace SearchesService.Models
{
    public class MtgApiData
    {
        public class CardData
        {
            public class ImageData
            {
                public string Normal { get; set; }
                public string Large { get; set; }
            }

            [JsonProperty("mana_cost")]
            public string ManaCost { get; set; }
            [JsonProperty("oracle_text")]
            public string OracleText { get; set; }
            [JsonProperty("image_uris")]
            public ImageData Images { get; set; }
            public string Name { get; set; }
            [JsonProperty("scryfall_uri")]
            public string ScryfallUrl { get; set; }
            [JsonProperty("type_line")]
            public string TypeLine { get; set; }
            [JsonProperty("flavor_text")]
            public string Flavor { get; set; }
            [JsonProperty("purchase_uris")]
            public Dictionary<string, string> PurchaseUrls { get; set; }
        }

        public List<CardData> Data { get; set; }
    }
}
