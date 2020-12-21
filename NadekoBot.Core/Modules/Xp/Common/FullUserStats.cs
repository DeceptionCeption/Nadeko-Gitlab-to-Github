using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Modules.Xp.Common
{
    public class FullUserStats
    {
        public DiscordUser User { get; }
        public UserXpStats FullGuildStats { get; }
        public LevelStats Global { get; }
        public LevelStats Guild { get; }
        public LevelStats GuildVoice { get; }
        public int GlobalRanking { get; }
        public int GuildRanking { get; }

        public FullUserStats(DiscordUser usr,
            UserXpStats fullGuildStats, LevelStats global,
            LevelStats guild, LevelStats guildVoice, int globalRanking, int guildRanking)
        {
            this.User = usr;
            this.Global = global;
            this.Guild = guild;
            this.GuildVoice = guildVoice;
            this.GlobalRanking = globalRanking;
            this.GuildRanking = guildRanking;
            this.FullGuildStats = fullGuildStats;
        }
    }
}