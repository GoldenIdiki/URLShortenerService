using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using URLShortener.Services.Interfaces;

namespace URLShortener.Controllers
{
    [Route("")]
    [ApiController]
    public class UrlShortenerController : ControllerBase
    {
        private readonly IUrlShortenerService _urlShortenerService;

        public UrlShortenerController(IUrlShortenerService urlShortenerService)
        {
            _urlShortenerService = urlShortenerService;
        }

        [SwaggerOperation(Summary = "Shorten Long URL", Description = "For shortening long URLs and returning the short code with the scheme and host")]
        [SwaggerResponse(StatusCodes.Status200OK, "Request Successful", typeof(string))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, null, typeof(string))]
        [SwaggerResponse(StatusCodes.Status429TooManyRequests, null, typeof(string))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, null, typeof(string))]
        [HttpPost("shorten")]
        public async Task<IActionResult> ShortenLongURL([FromBody] string longUrl)
        {
            if (!Uri.IsWellFormedUriString(longUrl, UriKind.Absolute))
                return BadRequest("URL is invalid");

            var shortCode = await _urlShortenerService.ShortenUrlAsync(longUrl).ConfigureAwait(false);
            var shortUrl = $"{Request.Scheme}://{Request.Host}/{shortCode}";
            return Ok(shortUrl);
        }

        [SwaggerOperation(Summary = "Redirect to the Original URL", Description = "Gets the short code as a path from the URL and redirects to the original long URL")]
        [SwaggerResponse(StatusCodes.Status200OK, "Request Successful", typeof(string))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, null, typeof(string))]
        [SwaggerResponse(StatusCodes.Status404NotFound, null, typeof(string))]
        [SwaggerResponse(StatusCodes.Status429TooManyRequests, null, typeof(string))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, null, typeof(string))]
        [HttpGet("{shortUrl}")]
        public async Task<IActionResult> RedirectToLongUrl(string shortUrl)
        {
            var longUrl = await _urlShortenerService.GetLongUrlAsync(shortUrl).ConfigureAwait(false);
            if (longUrl == null) return NotFound();
            return Redirect(longUrl);
        }

        [SwaggerOperation(Summary = "Calling Post Switch", Description = "For posting payment to the post switch endpoint")]
        [SwaggerResponse(StatusCodes.Status200OK, "Request Successful", typeof(object))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, null, typeof(object))]
        [SwaggerResponse(StatusCodes.Status429TooManyRequests, null, typeof(string))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, null, typeof(object))]
        [HttpGet("stats/{shortUrl}")]
        public async Task<IActionResult> GetStatsOfShortUrl(string shortUrl)
        {
            var count = await _urlShortenerService.GetAccessCountAsync(shortUrl).ConfigureAwait(false);
            return Ok(new { shortUrl, accessCount = count });
        }
    }
}
