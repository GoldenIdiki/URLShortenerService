using System.Security.Cryptography;
using System.Text;

namespace URLShortener.Helpers
{
    public class Utils
    {
        public static string GenerateSecureAndUniqueShortCode()
        {
            var entropy = $"{Guid.NewGuid()}{DateTime.UtcNow.Ticks}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(entropy));
            return Convert.ToBase64String(hash)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .Substring(0, 8);
        }
    }
}
