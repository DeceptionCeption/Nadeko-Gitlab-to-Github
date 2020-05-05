using Nadeko.Common.Db;

namespace Nadeko.Common.Services.Db
{
    public class GuildConfig : DbEntity
    {
        public ulong GuildId { get; set; }
        public string CommandString { get; set; } = null;
        public bool? CommandStringIsSuffix { get; set; } = null;
        public string TimeZoneId { get; set; } = null;
        public string Locale { get; set; } = null;
    }
}
