using Microsoft.EntityFrameworkCore;
using Nadeko.Db;
using SearchImagesService.Common.Db.Models;

namespace SearchImagesService.Common.Db
{
    public class SearchImageContext : ServiceDbContext
    {
        public static ServiceDbConfigBuilder<SearchImageContext> BaseOptions => new ServiceDbConfigBuilder<SearchImageContext>()
            .WithDatabase("searchimages");

        public DbSet<BlacklistedTags> BlacklistedTags { get; set; }
        public SearchImageContext(DbContextOptions options) : base(options)
        {
        }
    }

    public class SearchImageContextFactory : ServiceDbContextFactory<SearchImageContext>
    {
        public SearchImageContextFactory() : base(SearchImageContext.BaseOptions.Build())
        {
        }
    }
}
