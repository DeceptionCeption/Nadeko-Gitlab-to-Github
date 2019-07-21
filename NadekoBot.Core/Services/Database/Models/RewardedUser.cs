using System;

namespace NadekoBot.Core.Services.Database.Models
{
    public class RewardedUser : DbEntity
    {
        public string PatreonUserId { get; set; }
        public int AmountRewardedThisMonth { get; set; }
        public DateTime LastReward { get; set; }
    }
}
