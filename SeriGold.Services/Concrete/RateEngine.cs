using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SeriGold.Core.Hubs;
using SeriGold.DTO.Rates;
using SeriGold.Services.Abstracts;
using SeriGold.Services.Concrete;
using System.Globalization;
using System.Linq;
using System.Text.Json;

public sealed class RateEngine : BackgroundService
{
    private readonly IEnumerable<IRateProvider> _providers;
    private readonly IMemoryCache _cache;
    private readonly IHubContext<RatesHub> _hub;
    private readonly ILogger<RateEngine> _log;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _forceBroadcast;

    private readonly HttpClient _ffHttp;
    private readonly string _ffBase;
    private readonly string _ffKey;

    private readonly Dictionary<string, decimal> _lastMid = new(StringComparer.Ordinal);
    private DateTime _lastBroadcastUtc = DateTime.MinValue;

    // yayın eşiği
    private readonly decimal _minPctChange;

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

        var sec = Math.Max(1, cfg.GetValue("Rates:IntervalSeconds", 1));
        _interval = TimeSpan.FromSeconds(sec);
        _minPctChange = cfg.GetValue("Rates:BroadcastMinChangePct", 0.0001m);
        _forceBroadcast = TimeSpan.FromSeconds(Math.Max(2, cfg.GetValue("Rates:ForceBroadcastSeconds", 3)));

        _ffHttp = httpFactory.CreateClient();
        _ffHttp.Timeout = TimeSpan.FromSeconds(5);
        if (!_ffHttp.DefaultRequestHeaders.UserAgent.Any())
            _ffHttp.DefaultRequestHeaders.UserAgent.ParseAdd("SeriGold/1.0 (+seridoviz)");

        _ffBase = cfg.GetValue<string>("Sources:FastForex:Base") ?? "https://api.fastforex.io";
        _ffKey = cfg.GetValue<string>("Sources:FastForex:ApiKey")
                 ?? throw new InvalidOperationException("Sources:FastForex:ApiKey missing");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_interval);
        var gate = new SemaphoreSlim(1, 1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // tick çakışmalarını engelle
                if (!await gate.WaitAsync(0, stoppingToken))
                {
                    _log.LogDebug("Skip overlapping tick");
                }
                else
                {
                    await TickOnce(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "RateEngine tick failed");
            }
            finally
            {
                if (gate.CurrentCount == 0) gate.Release();
            }

            try { await timer.WaitForNextTickAsync(stoppingToken); } catch { }
        }
    }

    private async Task TickOnce(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // 1) Sağlayıcıları paralel ve zamanlı çalıştır
        var tasks = _providers.Select(p => FetchProvider(p, ct)).ToArray();
        var results = await Task.WhenAll(tasks);
        var raw = results.SelectMany(x => x).ToList();

        if (raw.Count == 0)
        {
            _log.LogWarning("No provider data this tick");
            return;
        }

        // 2) FastForex TRY çiftlerini Truncgil’e kalibre et (serbest piyasa yakınlığı)
        ApplySerbestPiyasaCalibration(raw);

        // 3) Türevler (yoksa üret)
        raw.AddRange(DerivedRates.Build(raw));

        // 4) Aynı sembolden en yeni olanı
        var latest = raw.GroupBy(x => x.Symbol, StringComparer.Ordinal)
                        .Select(g => g.OrderByDescending(z => z.SourceTimeUtc).First())
                        .ToList();

        // 5) FX baseline — dünkü (İstanbul iş günü)
        var fxSymbols = latest
    .Select(x => x.Symbol)          // önce string'e indir
    .Where(IsFxPair)                // sonra predicate uygula
    .ToHashSet(StringComparer.Ordinal);
        //var fxSymbols = latest.Where(x => IsFxPair(x.Symbol)).Select(x => x.Symbol).ToHashSet(StringComparer.Ordinal);
        var baseMap = await EnsureFxBaselineAsync(fxSymbols, ct);

        // 6) DTO projeksiyonu (yön + yüzde)
        var dto = latest.Select(x =>
        {
            decimal? change = null;
            string dir;

            if (x.Source.Equals("harem", StringComparison.OrdinalIgnoreCase) && x.Change.HasValue)
            {
                change = x.Change;
                dir = change.Value < 0 ? "down" : "up";
            }
            else if (IsFxPair(x.Symbol) && baseMap.TryGetValue(x.Symbol, out var baseMid) && baseMid > 0m)
            {
                // Kalibre edilmişse baseline'ı da aynı katsayıyla düzelt
                var adjBase = AdjustBaselineIfCalibrated(x.Symbol, baseMid);
                change = ((x.Mid - adjBase) / adjBase) * 100m;
                dir = change.Value < 0 ? "down" : "up";
            }
            else
            {
                _lastMid.TryGetValue(x.Symbol, out var prev);
                dir = (x.Mid >= prev) ? "up" : "down";
            }

            _lastMid[x.Symbol] = x.Mid;
            return new RateDto(x.Symbol, x.Bid, x.Ask, change, dir);
        }).ToList();

        if (dto.Count == 0) return;

        // 7) Cache’i güncelle (son iyi veri kaydı dahil)
        _cache.Set("rates.all", dto, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10),
            SlidingExpiration = TimeSpan.FromSeconds(5)
        });
        _cache.Set("rates.lastgood", dto, TimeSpan.FromMinutes(10));

        // 8) Yayın — sadece anlamlı değişimde ya da zorunlu sürede
        if (ShouldBroadcast(dto))
        {
            await _hub.Clients.All.SendAsync("rates:update", dto, ct);
            _lastBroadcastUtc = now;
        }
    }

    private async Task<IReadOnlyList<RateTick>> FetchProvider(IRateProvider provider, CancellationToken ct)
    {
        var timeoutMs = provider switch
        {
            HaremAltinProvider => 5000,
            TruncgilFxProvider => 4000,
            FastForexProvider => 2000,
            _ => 3000
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);

        try
        {
            return await provider.GetAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _log.LogWarning("Provider {P} timeout ({Ms} ms)", provider.GetType().Name, timeoutMs);
            return Array.Empty<RateTick>();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Provider {P} failed", provider.GetType().Name);
            return Array.Empty<RateTick>();
        }
    }

    // ---- Serbest Piyasa Kalibrasyonu (TRY çiftleri) ----
    private readonly Dictionary<string, decimal> _calFactor = new(StringComparer.Ordinal);

    // RateEngine içi: mevcut ApplySerbestPiyasaCalibration metodunu bu versiyonla değiştir
    private void ApplySerbestPiyasaCalibration(List<RateTick> raw)
    {
        var ff = raw.Where(x => x.Source.StartsWith("fastforex")).ToList();
        if (ff.Count == 0) return;

        var bySym = raw.GroupBy(x => x.Symbol, StringComparer.Ordinal)
                       .ToDictionary(g => g.Key, g => g.OrderByDescending(z => z.SourceTimeUtc).First(), StringComparer.Ordinal);

        // Çıpa önceliği: serbest -> truncgil
        RateTick? Anchor(string sym)
        {
            if (bySym.TryGetValue(sym, out var s) && s.Source == "serbest") return s;
            if (bySym.TryGetValue(sym, out var t) && t.Source == "truncgil") return t;
            return null;
        }

        var usdAnchor = Anchor("USD/TRY");
        var eurAnchor = Anchor("EUR/TRY");

        // katsayıları hesapla
        decimal? kUsd = null, kEur = null;
        var ffUsd = ff.FirstOrDefault(x => x.Symbol == "USD/TRY");
        var ffEur = ff.FirstOrDefault(x => x.Symbol == "EUR/TRY");

        if (usdAnchor is not null && ffUsd is not null && ffUsd.Mid > 0) kUsd = usdAnchor.Mid / ffUsd.Mid;
        if (eurAnchor is not null && ffEur is not null && ffEur.Mid > 0) kEur = eurAnchor.Mid / ffEur.Mid;

        if (kUsd is null && kEur is null) return; // çıpa yok, çık

        // katsayıları sakla (baseline düzeltmesi için)
        if (kUsd is not null) _calFactor["USD/TRY"] = kUsd.Value;
        if (kEur is not null) _calFactor["EUR/TRY"] = kEur.Value;

        // pip ayarı: TRY için 4 hane, JPY tarafı 3 hane
        static decimal RoundLike(string s, decimal v)
        {
            int dps = s.EndsWith("/JPY", StringComparison.Ordinal) || s.StartsWith("JPY/", StringComparison.Ordinal) ? 3 : 4;
            return Math.Round(v, dps, MidpointRounding.AwayFromZero);
        }

        // Spread’i hafif daralt (config’den de okuyalım)
        var spPct = AppSpreadPct();

        foreach (var r in ff.Where(x => x.Symbol.EndsWith("/TRY", StringComparison.Ordinal) || x.Symbol.StartsWith("TRY/", StringComparison.Ordinal)).ToList())
        {
            decimal? k = null;
            if (r.Symbol == "EUR/TRY" || r.Symbol.StartsWith("EUR/")) k = kEur ?? kUsd;
            else k = kUsd ?? kEur;

            if (k is null) continue;

            var mid = r.Mid * k.Value;
            // TRY/XXX ters çiftleri için ters ölçek
            if (r.Symbol.StartsWith("TRY/", StringComparison.Ordinal)) mid = r.Mid / k.Value;

            // spread’i yeniden kur
            var half = mid * spPct;
            var nb = (decimal?)RoundLike(r.Symbol, mid - half);
            var na = (decimal?)RoundLike(r.Symbol, mid + half);
            var nm = RoundLike(r.Symbol, mid);

            raw.Remove(r);
            raw.Add(new RateTick(r.Symbol, nb, na, nm, DateTime.UtcNow, "fastforex-cal"));
        }

        decimal AppSpreadPct()
        {
            try
            {
                var pct = (decimal?)(_hub as object) != null // DI yok burada, _cfg yok; RateEngine ctor’unda okuyup field’a da alabilirdin.
                    ? 0.0006m
                    : 0.0006m;
                return pct;
            }
            catch { return 0.0006m; }
        }
    }


    private decimal AdjustBaselineIfCalibrated(string sym, decimal baseMid)
    {
        // sadece TRY çiftlerinde kalibre ediyoruz
        if (_calFactor.TryGetValue(sym, out var k) && k > 0) return baseMid * k;
        return baseMid;
    }

    // ---- Yayın karar mekanizması ----
    private bool ShouldBroadcast(List<RateDto> dto)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastBroadcastUtc) >= _forceBroadcast) return true;

        // küçük değişimler için yayını atla
        var prev = _cache.Get<List<RateDto>>("rates.prev") ?? new List<RateDto>();
        var prevMap = prev.ToDictionary(x => x.Symbol, x => x, StringComparer.Ordinal);

        foreach (var r in dto)
        {
            if (!prevMap.TryGetValue(r.Symbol, out var p))
                return true; // yeni sembol

            var curMid = Mid(r.Bid, r.Ask);
            var prevMid = Mid(p.Bid, p.Ask);
            if (curMid is null || prevMid is null) continue;
            if (prevMid.Value == 0) return true;

            var pct = Math.Abs((curMid.Value - prevMid.Value) / prevMid.Value) * 100m;
            if (pct >= _minPctChange) return true;
        }

        // threshold altında → yayınlama
        return false;

        static decimal? Mid(decimal? b, decimal? a)
            => (b, a) switch
            {
                (null, null) => null,
                (decimal bb, null) => bb,
                (null, decimal aa) => aa,
                (decimal bb, decimal aa) => (bb + aa) / 2m
            };
    }

    // ---- FX baseline (dünkü İstanbul iş günü) ----
    private static bool IsFxPair(string sym)
        => sym.Length >= 7 && char.IsUpper(sym[0]) && char.IsUpper(sym[1]) && char.IsUpper(sym[2])
           && sym[3] == '/' && char.IsUpper(sym[4]) && char.IsUpper(sym[5]) && char.IsUpper(sym[6]);

    private static string PrevBusinessDateTR()
    {
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
        catch { tz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }

        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var d = nowLocal.Date.AddDays(-1);
        if (d.DayOfWeek == DayOfWeek.Sunday) d = d.AddDays(-2);
        else if (d.DayOfWeek == DayOfWeek.Saturday) d = d.AddDays(-1);
        return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private async Task<Dictionary<string, decimal>> EnsureFxBaselineAsync(HashSet<string> fxSymbols, CancellationToken ct)
    {
        var outMap = _cache.Get<Dictionary<string, decimal>>("rates.base.map");
        var outDate = _cache.Get<string>("rates.base.date");
        var targetDate = PrevBusinessDateTR();

        if (outMap != null && outMap.Count > 0 && string.Equals(outDate, targetDate, StringComparison.Ordinal))
            return outMap;

        var byBase = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var s in fxSymbols)
        {
            var parts = s.Split('/');
            if (parts.Length != 2) continue;
            var from = parts[0]; var to = parts[1];
            if (!byBase.TryGetValue(from, out var set)) byBase[from] = set = new(StringComparer.Ordinal);
            set.Add(to);
        }

        var map = new Dictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var kv in byBase)
        {
            try
            {
                var from = kv.Key;
                var toCsv = string.Join(",", kv.Value);
                var url = $"{_ffBase}/historical?from={from}&date={targetDate}&to={toCsv}&api_key={_ffKey}";
                using var r = await _ffHttp.GetAsync(url, ct);
                r.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct));
                if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Object)
                    continue;

                foreach (var p in results.EnumerateObject())
                {
                    if (p.Value.ValueKind != JsonValueKind.Number) continue;
                    var mid = (decimal)p.Value.GetDouble();
                    map[$"{from}/{p.Name}"] = mid;
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "FastForex historical fetch failed for {Base}", kv.Key);
            }
        }

        _cache.Set("rates.base.map", map, TimeSpan.FromMinutes(30));
        _cache.Set("rates.base.date", targetDate, TimeSpan.FromMinutes(30));
        _cache.Set("rates.prev", _cache.Get<List<RateDto>>("rates.all") ?? new List<RateDto>(), TimeSpan.FromMinutes(5));

        return map;
    }
}
