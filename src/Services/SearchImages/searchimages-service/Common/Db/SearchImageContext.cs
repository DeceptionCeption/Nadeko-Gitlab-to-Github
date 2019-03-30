using Microsoft.EntityFrameworkCore;
using NadekoDb;
using SearchImagesService.Common.Db.Models;

namespace SearchImagesService.Common.Db
{
    public class SearchImageContext : ServiceDbContext
    {
        public DbSet<BlacklistedTags> BlacklistedTags { get; set; }
        public SearchImageContext(DbContextOptions options) : base("searchimages", options)
        {
        }
    }

    public class SearchImageContextFactory : ServiceDbContextFactory<SearchImageContext>
    {

    }
}
