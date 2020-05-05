using ExpressionsService.Database.Models;
using Microsoft.EntityFrameworkCore;
using Nadeko.Common;
using Nadeko.Common.Db;
using System;

namespace ExpressionsService.Database
{
    public class ExpressionsDb : ServiceDb<ExpressionsContext>
    {
        public ExpressionsDb(CredsService creds) : base(DbOptionsHelper.BuildOptions<ExpressionsContext>(creds))
        {

        }
    }

    public class ExpressionsContext : ServiceDbContext
    {
        public override string SchemaName { get; } = "expressions";

        public DbSet<Expression> Expressions { get; set; }
        public ExpressionsContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var expr = modelBuilder.Entity<Expression>();
            expr.HasIndex(x => x.GuildId);
            expr.HasIndex(x => x.Trigger);


            expr.HasData(new Expression
            {
                Id = -1,
                AuthorId = 105635576866156544,
                AuthorName = "Kwoth#2452",
                AutoDelete = false,
                DateAdded = DateTime.UtcNow,
                DirectMessage = false,
                GuildId = 0,
                IsQuote = false,
                Response = @"{
  ""title"": ""How to donate"",
  ""description"": ""%user.fullname%, If you want to support NadekoBot, you can do so through these links"",
                ""color"": 53380,
                ""fields"": [
    {
                ""name"": ""Patreon"",
      ""value"": "" https://patreon.com/nadekobot"",
      ""inline"": true
    },
    {
                ""name"": ""Paypal"",
      ""value"": "" https://paypal.me/Kwoth"",
      ""inline"": true
    }
  ]
}",
                Trigger = ".donate"
            }, new Expression
            {
                Id = -2,
                AuthorId = 105635576866156544,
                AuthorName = "Kwoth#2452",
                AutoDelete = false,
                DateAdded = DateTime.UtcNow,
                DirectMessage = false,
                GuildId = 0,
                IsQuote = false,
                Response = @"{
                ""color"": 53380,
                ""fields"": [
                {
                    ""name"": ""List of commands"",
                    ""value"": ""https://nadeko.bot/commands"",
                    ""inline"": false
                },
                {
                    ""name"": ""Hosting guides and docs"",
                    ""value"": ""http://nadekobot.readthedocs.io/en/latest/"",
                    ""inline"": false
                }]
}",
                Trigger = ".guide"
            });
        }
    }

    public class ExpressionsContextFactory : ServiceDbContextFactory<ExpressionsContext>
    {
        public ExpressionsContextFactory() : base(new ServiceDbConfigBuilder<ExpressionsContext>().Build())
        {
        }
    }
}
