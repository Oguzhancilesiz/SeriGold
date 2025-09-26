using Mapster;
using SeriGold.DTO;
using SeriGold.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Services.MapsterMap
{
    public static class MapsterConfig
    {
        private static bool _inited;
        public static void Register()
        {
            if (_inited) return;
            _inited = true;

            TypeAdapterConfig<SiteSetting, SiteSettingDto>.NewConfig();
            TypeAdapterConfig<SiteSettingUpdateDto, SiteSetting>.NewConfig();

            TypeAdapterConfig<FaqItem, FaqDto>.NewConfig();
            TypeAdapterConfig<FaqAddDto, FaqItem>.NewConfig();
            TypeAdapterConfig<FaqUpdateDto, FaqItem>.NewConfig();

            TypeAdapterConfig<ServiceItem, ServiceItemDto>.NewConfig();
            TypeAdapterConfig<ServiceItemAddDto, ServiceItem>.NewConfig();
            TypeAdapterConfig<ServiceItemUpdateDto, ServiceItem>.NewConfig();

            TypeAdapterConfig<SiteSetting, SiteSettingDto>.NewConfig();
            TypeAdapterConfig<SiteSetting, SiteSettingUpdateDto>.NewConfig();

            // UpdateDto -> Entity (Id dışındaki alanlar set edilecek)
            TypeAdapterConfig<SiteSettingUpdateDto, SiteSetting>
                .NewConfig()
                .IgnoreNullValues(true); // null gelen alanlar entity'de var olanı ezmesin
        }
    }
}
