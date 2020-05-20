namespace SearchesService.Models
{
    public class WikiaResponse
    {
        public class Item
        {
            public string Title { get; set; }
            public string Quality { get; set; }
            public string Url { get; set; }
        }

        public Item[] Items { get; set; }
    }

    public class GamepediaResponse
    {
        public class QueryData
        {
            public class PageData
            {
                public string Title { get; set; }
                public string Snippet { get; set; }
            }

            public PageData[] Search { get; set; }
        }

        public QueryData Query { get; set; }
    }
}
