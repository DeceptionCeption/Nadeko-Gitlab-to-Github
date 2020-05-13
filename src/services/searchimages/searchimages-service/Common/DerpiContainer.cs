using Newtonsoft.Json;

namespace SearchImagesService.Common
{
    public class DerpiContainer
    {
        public DerpiImageObject[] Images { get; set; }
    }

    public class DerpiImageObject
    {
        [JsonProperty("view_url")]
        public string ViewUrl { get; set; }
        public string[] Tags { get; set; }
        public string Score { get; set; }
    }
}
