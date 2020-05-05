using Nadeko.Common.Db;

namespace ExpressionsService.Database.Models
{
    public class Expression : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string Trigger { get; set; }
        public string Response { get; set; }

        public bool AutoDelete { get; set; }
        public bool DirectMessage { get; set; }
        public bool ContainsAnywhere { get; set; }
        public bool IsQuote { get; set; }
    }
}
