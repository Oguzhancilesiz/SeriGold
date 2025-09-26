using SeriGold.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Services.Abstracts
{
    public interface ISiteSettingService
    {
        Task<SiteSettingDto> GetAsync();
        Task UpdateAsync(SiteSettingUpdateDto dto);
        Task EnsureSeedAsync(); // yoksa 1 kayıt oluştur
    }
}
