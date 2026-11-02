# SeriGold

Kuyumcu ve döviz bürosu için canlı altın ve döviz kurları sitesi. Kurlar harici bir fiyat API'sinden çekilip SignalR ile tarayıcılara anlık gönderilir. Site içeriği admin panelinden yönetilir.

Mobil istemcisi: [SeriDovizaPP](https://github.com/Oguzhancilesiz/SeriDovizaPP) (.NET MAUI).

## Özellikler

- **Canlı kurlar:** `RatesHub` (SignalR) üzerinden sayfa yenilemeden fiyat güncellemesi
- **Admin paneli:** hizmetler, SSS, site ayarları, dashboard
- **Kimlik:** ASP.NET Core Identity ile admin girişi

## Teknolojiler

- ASP.NET Core MVC (.NET 9), Areas
- SignalR
- Entity Framework Core + SQL Server
- Mapster / AutoMapper
- Harici fiyat kaynakları: Harem Altın (RapidAPI) ve FastForex

## Mimari

```
SeriGold.Entity → SeriGold.Mapping → SeriGold.DAL → SeriGold.Services → SeriGold.WebUI
                       SeriGold.DTO, SeriGold.Core (hub'lar, ortak)
```

## Çalıştırma

```bash
git clone https://github.com/Oguzhancilesiz/SeriGold.git
cd SeriGold
dotnet user-secrets --project SeriGold.WebUI set "ConnectionStrings:DefaultConnection" "<connection-string>"
dotnet user-secrets --project SeriGold.WebUI set "Sources:Harem:Key" "<rapidapi-anahtari>"
dotnet user-secrets --project SeriGold.WebUI set "Sources:FastForex:ApiKey" "<fastforex-anahtari>"
dotnet run --project SeriGold.WebUI
```

Bağlantı dizesi ve API anahtarları repoda tutulmaz.
