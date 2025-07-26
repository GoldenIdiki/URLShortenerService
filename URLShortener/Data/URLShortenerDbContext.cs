using Microsoft.EntityFrameworkCore;
using URLShortener.Models;

namespace URLShortener.Data
{
    public class URLShortenerDbContext : DbContext
    {
        public URLShortenerDbContext(DbContextOptions<URLShortenerDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UrlMapping>()
                .HasIndex(u => u.ShortUrl)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<UrlMapping> UrlMappings => Set<UrlMapping>();
    }
}
