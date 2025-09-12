// WebUI/Controllers/RatesDebugController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace SeriGold.WebUI.Controllers
{
    [ApiController]
    [Route("api/rates")]
    public class RatesDebugController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        public RatesDebugController(IMemoryCache cache) => _cache = cache;

        [HttpGet("base")]
        public IActionResult Base()
        {
            var date = _cache.Get<string>("rates.base.date") ?? "(unknown)";
            var map = _cache.Get<Dictionary<string, decimal>>("rates.base.map") ?? new();
            return Ok(new { date, count = map.Count, sample = map.Take(12) });
        }
    }
}
