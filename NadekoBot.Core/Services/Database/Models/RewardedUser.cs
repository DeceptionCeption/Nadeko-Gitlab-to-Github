using System;

namespace NadekoBot.Core.Services.Database.Models
{
    public class RewardedUser : DbEntity
    {
        /// <summary>
        /// OBSOLETE, DO NOT USE
        /// </summary>
        public ulong UserId { get; set; } = 0;
        public string PatreonUserId { get; set; }
        public int AmountRewardedThisMonth { get; set; }
        public DateTime LastReward { get; set; }
    }
}
