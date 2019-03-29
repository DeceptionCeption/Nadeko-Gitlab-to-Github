using Newtonsoft.Json;

namespace SearchImagesService.Common
{
    public class DapiImageObject
    {
        [JsonProperty("File_Url")]
        public string FileUrl { get; set; }
        public string Tags { get; set; }
        [JsonProperty("Tag_String")]
        public string TagString { get; set; }
        public string Rating { get; set; }
    }
}
