// RateEngine.cs
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SeriGold.Core.Hubs;
using SeriGold.DTO.Rates;
using SeriGold.Services.Abstracts;
using SeriGold.Services.Concrete;

public sealed class RateEngine : BackgroundService
{
    private readonly IEnumerable<IRateProvider> _providers;
    private readonly IMemoryCache _cache;
    private readonly IHubContext<RatesHub> _hub;
    private readonly ILogger<RateEngine> _log;
    private readonly TimeSpan _interval;
    private readonly HttpClient _http;

    private readonly Dictionary<string, decimal> _baseMid = new(StringComparer.Ordinal);
    private readonly Dictionary<string, decimal> _lastMid = new(StringComparer.Ordinal);
    private DateTime _baseDayTr = DateTime.MinValue;

    private static readonly string TrZoneId =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Turkey Standard Time" : "Europe/Istanbul";
    private static readonly TimeZoneInfo TrTz = TimeZoneInfo.FindSystemTimeZoneById(TrZoneId);

    public RateEngine(IEnumerable<IRateProvider> providers,
                      IMemoryCache cache,
                      IHubContext<RatesHub> hub,
                      ILogger<RateEngine> log,
                      IConfiguration cfg,
                      IHttpClientFactory httpFactory)
    {
        _providers = providers;
        _cache = cache;
        _hub = hub;
        _log = log;
        _interval = TimeSpan.FromSeconds(cfg.GetValue<int>("Rates:IntervalSeconds", 1));

        _http = httpFactory.CreateClient();
        _http.Timeout = TimeSpan.FromSeconds(10);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("SeriGold/1.0 (+serigold)");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var trToday = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TrTz).Date;
                if (_baseDayTr != trToday)
                {
                    _baseMid.Clear();
                    await BootstrapDailyBaseAsync(trToday, stoppingToken);
                    _baseDayTr = trToday;
                }

                // 1) Sağlayıcılar
                var raw = new List<RateTick>();
                foreach (var p in _providers)
                {
                    try
                    {
                        var part = await p.GetAsync(stoppingToken);
                        _log.LogInformation("Provider {Provider} -> {Count} rows", p.GetType().Name, part.Count);
                        raw.AddRange(part);
                    }
                    catch (Exception ex) { _log.LogError(ex, "Provider {Provider} failed", p.GetType().Name); }
                }

                // 2) Türetilmişler
                raw.AddRange(DerivedRates.Build(raw));

                // 3) En güncel semboller
                var latest = raw.GroupBy(x => x.Symbol, StringComparer.Ordinal)
                                .Select(g => g.OrderByDescending(x => x.SourceTimeUtc).First())
                                .ToList();

                // 4) DTO (dünkü TR kapanışa göre %)
                var dto = latest.Select(x =>
                {
                    if (!_baseMid.TryGetValue(x.Symbol, out var baseV) || baseV <= 0m)
                    {
                        _baseMid[x.Symbol] = x.Mid;
                        baseV = x.Mid;
                    }

                    var daily = baseV == 0m ? 0m : (x.Mid - baseV) / baseV * 100m;

                    _lastMid.TryGetValue(x.Symbol, out var prev);
                    var dir = (prev == 0m || x.Mid >= prev) ? "up" : "down";
                    _lastMid[x.Symbol] = x.Mid;

                    return new RateDto(x.Symbol, x.Bid, x.Ask, daily, dir);
                }).ToList();

                // 5) Cache + yayın
                if (dto.Count > 0)
                {
                    _cache.Set("rates.all", dto, TimeSpan.FromMinutes(2));
                    await _hub.Clients.All.SendAsync("rates:update", dto, stoppingToken);
                }
            }
            catch (Exception ex) { _log.LogError(ex, "RateEngine tick failed"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    // ---------- Dünkü (TR) kapanıştan bazları kur ----------
    private sealed class ExHostResp { public Dictionary<string, decimal>? rates { get; set; } }

    private async Task BootstrapDailyBaseAsync(DateTime trToday, CancellationToken ct)
    {
        DateTime targetTr = trToday.AddDays(-1);
        Dictionary<string, decimal>? fxRates = null;
        decimal xauUsd = 0, xagUsd = 0, xptUsd = 0, xpdUsd = 0;
        string? usedDateStr = null;

        for (int i = 0; i < 5; i++)
        {
            var ds = targetTr.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            try
            {
                var fx = await Get<ExHostResp>($"https://api.exchangerate.host/{ds}?base=USD", ct);
                if (fx?.rates is { Count: > 0 }) fxRates = fx.rates;

                var rXau = await Get<ExHostResp>($"https://api.exchangerate.host/{ds}?base=XAU&symbols=USD", ct);
                if (rXau?.rates?.TryGetValue("USD", out var vXau) == true) xauUsd = vXau;

                var rXag = await Get<ExHostResp>($"https://api.exchangerate.host/{ds}?base=XAG&symbols=USD", ct);
                if (rXag?.rates?.TryGetValue("USD", out var vXag) == true) xagUsd = vXag;
                var rXpt = await Get<ExHostResp>($"https://api.exchangerate.host/{ds}?base=XPT&symbols=USD", ct);
                if (rXpt?.rates?.TryGetValue("USD", out var vXpt) == true) xptUsd = vXpt;
                var rXpd = await Get<ExHostResp>($"https://api.exchangerate.host/{ds}?base=XPD&symbols=USD", ct);
                if (rXpd?.rates?.TryGetValue("USD", out var vXpd) == true) xpdUsd = vXpd;

                if (fxRates != null && fxRates.TryGetValue("TRY", out var usdTryY) && usdTryY > 0m && xauUsd > 0m)
                {
                    usedDateStr = ds;
                    var ticks = new List<RateTick>();

                    foreach (var (ccy, per) in fxRates)
                    {
                        if (ccy == "USD" || per <= 0) continue;
                        ticks.Add(new RateTick($"USD/{ccy}", per, per, per, targetTr, "yday"));
                        ticks.Add(new RateTick($"{ccy}/USD", 1m / per, 1m / per, 1m / per, targetTr, "yday"));
                        if (ccy != "TRY")
                        {
                            var midTry = usdTryY / per;       // ← kritik doğrulama
                            ticks.Add(new RateTick($"{ccy}/TRY", midTry, midTry, midTry, targetTr, "yday"));
                        }
                    }
                    ticks.Add(new RateTick("USD/TRY", usdTryY, usdTryY, usdTryY, targetTr, "yday"));
                    ticks.Add(new RateTick("TRY/USD", 1m / usdTryY, 1m / usdTryY, 1m / usdTryY, targetTr, "yday"));

                    ticks.Add(new RateTick("XAU/USD", xauUsd, xauUsd, xauUsd, targetTr, "yday"));
                    if (xagUsd > 0) ticks.Add(new RateTick("XAG/USD", xagUsd, xagUsd, xagUsd, targetTr, "yday"));
                    if (xptUsd > 0) ticks.Add(new RateTick("XPT/USD", xptUsd, xptUsd, xptUsd, targetTr, "yday"));
                    if (xpdUsd > 0) ticks.Add(new RateTick("XPD/USD", xpdUsd, xpdUsd, xpdUsd, targetTr, "yday"));

                    ticks.AddRange(DerivedRates.Build(ticks));

                    foreach (var t in ticks)
                        _baseMid[t.Symbol] = t.Mid;

                    _cache.Set("rates.base.map", new Dictionary<string, decimal>(_baseMid), TimeSpan.FromHours(36));
                    _cache.Set("rates.base.date", usedDateStr!, TimeSpan.FromHours(36));

                    _log.LogInformation("Daily bases loaded (TR day {TrDay}) using {UsedDate} — {Count} symbols",
                        trToday.ToString("yyyy-MM-dd"), usedDateStr, _baseMid.Count);
                    return;
                }
            }
            catch (Exception ex) { _log.LogWarning(ex, "Bootstrap try {Date} failed", ds); }

            targetTr = targetTr.AddDays(-1);
        }

        _log.LogError("BootstrapDailyBase failed (no suitable historical day). Falling back to first-seen prices.");
    }

    private async Task<T?> Get<T>(string url, CancellationToken token)
    {
        using var r = await _http.GetAsync(url, token);
        r.EnsureSuccessStatusCode();
        var j = await r.Content.ReadAsStringAsync(token);
        return JsonSerializer.Deserialize<T>(j, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
