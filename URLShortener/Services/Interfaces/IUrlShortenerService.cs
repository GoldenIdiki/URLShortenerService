namespace URLShortener.Services.Interfaces
{
    public interface IUrlShortenerService
    {
        Task<string> ShortenUrlAsync(string longUrl);
        Task<string?> GetLongUrlAsync(string shortUrl);
        Task<int> GetAccessCountAsync(string shortUrl);
    }
}
