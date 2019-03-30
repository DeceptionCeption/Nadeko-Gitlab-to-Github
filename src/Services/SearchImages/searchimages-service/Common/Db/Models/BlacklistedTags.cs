using NadekoDb;

namespace SearchImagesService.Common.Db.Models
{
    public class BlacklistedTags : DbEntity
    {
        public ulong GuildId { get; set; }
        public string[] Tags { get; set; }
    }
}
