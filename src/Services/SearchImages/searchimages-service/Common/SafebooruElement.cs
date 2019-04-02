namespace SearchImagesService.Common
{
    public class SafebooruElement
    {
        public string Directory { get; set; }
        public string Image { get; set; }


        public string FileUrl => $"https://safebooru.org/images/{Directory}/{Image}";
        public string Rating { get; set; }
        public string Tags { get; set; }
    }
}
