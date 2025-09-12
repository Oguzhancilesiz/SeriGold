// WebUI/Controllers/RatesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SeriGold.DTO.Rates;

namespace SeriGold.WebUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class RatesController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        public RatesController(IMemoryCache cache) => _cache = cache;

        [HttpGet("latest")]
        public ActionResult<IEnumerable<RateDto>> Latest()
            => Ok(_cache.TryGetValue("rates.all", out List<RateDto>? list) ? list : new List<RateDto>());
    }
}
