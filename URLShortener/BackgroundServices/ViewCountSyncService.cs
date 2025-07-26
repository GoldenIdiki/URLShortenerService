using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using URLShortener.Data;

namespace URLShortener.BackgroundServices
{
    public class ViewCountSyncService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<ViewCountSyncService> _logger;

        public ViewCountSyncService(IServiceScopeFactory scopeFactory, IConnectionMultiplexer redis, ILogger<ViewCountSyncService> logger)
        {
            _scopeFactory = scopeFactory;
            _redis = redis;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var db = _redis.GetDatabase();

                while (!stoppingToken.IsCancellationRequested)
                {
                    var server = _redis.GetServer(_redis.GetEndPoints().First());
                    var viewKeys = server.Keys(pattern: "short:*:views").ToArray();
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<URLShortenerDbContext>();

                    foreach (var key in viewKeys)
                    {
                        var keyArray = key.ToString().Split(':');
                        var shortCode = keyArray[1];
                        var cacheKey = $"{keyArray[0]}:{keyArray[1]}";
                        var viewKey = $"{keyArray[0]}:{keyArray[1]}:{keyArray[2]}";
                        var count = (int)await db.StringGetAsync(key).ConfigureAwait(false);

                        if (count == 0) continue;

                        var record = await context.UrlMappings.FirstOrDefaultAsync(x => x.ShortUrl == shortCode).ConfigureAwait(false);
                        if (record != null)
                        {
                            record.UrlAccessCount += count;
                            record.DateModified = DateTime.UtcNow;
                        }

                        var ttl = await db.KeyTimeToLiveAsync(key).ConfigureAwait(false);
                        var isFinalSync = ttl.HasValue && ttl.Value <= TimeSpan.FromMinutes(15);

                        if (isFinalSync)
                        {
                            await db.KeyDeleteAsync(cacheKey).ConfigureAwait(false);
                        }
                        await db.StringDecrementAsync(viewKey, count).ConfigureAwait(false);
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while syncing view counts");
                throw;
            }
        }
    }
}
