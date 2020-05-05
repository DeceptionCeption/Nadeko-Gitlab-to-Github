using Microsoft.EntityFrameworkCore;
using Nadeko.Common.Db;
using NadekoBot.Core.Services;

namespace Nadeko.Common.Services.Db
{
    public class CommonDb : ServiceDb<CommonContext>, INService
    {
        public CommonDb(CredsService creds) : base(DbOptionsHelper.BuildOptions<CommonContext>(creds))
        {
        }
    }

    public class CommonContext : ServiceDbContext
    {
        public override string SchemaName => "common";

        public DbSet<GuildConfig> GuildConfigs { get; set; }
        //public DbSet<RotatingStatus> RotatingStatuses { get; set; }
        //public DbSet<RewardedUser> PatreonRewards { get; set; }

        public CommonContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var gcs = modelBuilder.Entity<GuildConfig>();

            gcs.HasIndex(x => x.GuildId)
                .IsUnique();

            //modelBuilder.Entity<RotatingStatus>()
            //    .HasIndex(x => x.Index)
            //    .IsUnique();
        }
    }

    public class CommonContextFactory : ServiceDbContextFactory<CommonContext>
    {
        public CommonContextFactory() : base(new ServiceDbConfigBuilder<CommonContext>().Build())
        {
        }
    }
}
