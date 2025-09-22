using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SeriGold.DTO.Rates;

namespace SeriGold.WebUI.Controllers
{
    [ApiController, Route("api/[controller]")]
    public sealed class RatesController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        public RatesController(IMemoryCache cache) => _cache = cache;

        [HttpGet("latest")]
        public ActionResult<IEnumerable<RateDto>> Latest()
        {
            // her koşulda son iyi veriyi döndür; boş liste döndürme
            if (_cache.TryGetValue("rates.all", out List<RateDto>? list) && list is { Count: > 0 })
                return Ok(list);
            if (_cache.TryGetValue("rates.lastgood", out List<RateDto>? last) && last is { Count: > 0 })
                return Ok(last);
            return Ok(new List<RateDto>());
        }
    }
}
