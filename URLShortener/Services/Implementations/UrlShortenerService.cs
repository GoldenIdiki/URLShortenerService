using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using URLShortener.Data;
using URLShortener.Helpers;
using URLShortener.Models;
using URLShortener.Services.Interfaces;

namespace URLShortener.Services.Implementations
{
    public class UrlShortenerService : IUrlShortenerService
    {
        private readonly URLShortenerDbContext _DbContext;
        private readonly IDistributedCache _cache;
        private readonly ILogger<UrlShortenerService> _logger;
        private readonly IConnectionMultiplexer _redis;

        public UrlShortenerService(URLShortenerDbContext dbContext, IDistributedCache cache,
            ILogger<UrlShortenerService> logger, IConnectionMultiplexer redis)
        {
            _DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redis = redis;
        }

        public async Task<int> GetAccessCountAsync(string shortUrl)
        {
            try
            {
                var redisKey = $"short:{shortUrl}:views";
                var redisCount = await _cache.GetStringAsync(redisKey).ConfigureAwait(false);
                var unsyncedViews = string.IsNullOrWhiteSpace(redisCount) ? 0 : Convert.ToInt32(redisCount);

                var entity = await _DbContext.UrlMappings.FirstOrDefaultAsync(x => x.ShortUrl == shortUrl).ConfigureAwait(false);
                var syncedViews = entity?.UrlAccessCount ?? 0;
                return syncedViews + unsyncedViews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting access count for short URL {ShortUrl}", shortUrl);
                throw;
            }  
        }

        public async Task<string> ShortenUrlAsync(string longUrl)
        {
            try
            {
                var existing = await _DbContext.UrlMappings.FirstOrDefaultAsync(x => x.OriginalUrl == longUrl).ConfigureAwait(false);
                if (existing != null) return existing.ShortUrl;

                var shortUrl = Utils.GenerateSecureAndUniqueShortCode();
                var entity = new UrlMapping { OriginalUrl = longUrl, ShortUrl = shortUrl };
                _DbContext.UrlMappings.Add(entity);
                await _DbContext.SaveChangesAsync().ConfigureAwait(false);

                return entity.ShortUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while shortening URL {LongUrl}", longUrl);
                throw;
            } 
        }

        public async Task<string?> GetLongUrlAsync(string shortCode)
        {
            try
            {
                var redisDb = _redis.GetDatabase();

                var cacheKey = $"short:{shortCode}";
                var viewsKey = $"{cacheKey}:views";
                var cachedUrl = await _cache.GetStringAsync(cacheKey).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(cachedUrl))
                {
                    _logger.LogInformation("Cache value gotten for {ShortCode}", shortCode);
                    await redisDb.StringIncrementAsync(viewsKey).ConfigureAwait(false);
                    return cachedUrl;
                }

                var entity = await _DbContext.UrlMappings.FirstOrDefaultAsync(x => x.ShortUrl == shortCode).ConfigureAwait(false);
                if (entity == null) return null;

                await _cache.SetStringAsync(cacheKey, entity.OriginalUrl, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                }).ConfigureAwait(false);

                await redisDb.StringSetAsync(viewsKey, 1).ConfigureAwait(false);
                return entity.OriginalUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving Original URL for short code {ShortCode}", shortCode);
                throw;
            } 
        }
    }
}
