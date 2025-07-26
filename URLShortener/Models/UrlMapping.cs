namespace URLShortener.Models
{
    public class UrlMapping
    {
        public int Id { get; set; }
        public required string OriginalUrl { get; set; }
        public required string ShortUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime DateModified { get; set; } = DateTime.UtcNow;
        public int UrlAccessCount { get; set; } = 0;
    }
}
