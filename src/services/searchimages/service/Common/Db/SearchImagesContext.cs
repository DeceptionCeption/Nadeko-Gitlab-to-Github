using Microsoft.EntityFrameworkCore;
using Nadeko.Common;
using Nadeko.Common.Db;
using SearchImagesService.Common.Db.Models;

namespace SearchImagesService.Common.Db
{
    public class SearchImagesDb : ServiceDb<SearchImagesContext>
    {
        public SearchImagesDb(CredsService creds) : base(DbOptionsHelper.BuildOptions<SearchImagesContext>(creds))
        {
        }
    }

    public class SearchImagesContext : ServiceDbContext
    {
        public override string SchemaName { get; } = "searchimages";
        public DbSet<BlacklistedTags> BlacklistedTags { get; set; }
        public SearchImagesContext(DbContextOptions options) : base(options)
        {
        }
    }

    public class SearchImageContextFactory : ServiceDbContextFactory<SearchImagesContext>
    {
        public SearchImageContextFactory() : base(new ServiceDbConfigBuilder<SearchImagesContext>().Build())
        {
        }
    }
}
