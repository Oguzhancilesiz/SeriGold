using Mapster;
using SeriGold.Core.Abstracts;
using SeriGold.Core.Enums;
using SeriGold.DTO;
using SeriGold.Entity;
using SeriGold.Services.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Services.Concrete
{
    public sealed class SiteSettingService : ISiteSettingService
    {
        private readonly IUnitOfWork _uow;

        public SiteSettingService(IUnitOfWork uow) => _uow = uow;

        public async Task EnsureSeedAsync()
        {
            var repo = _uow.Repository<SiteSetting>();
            var list = await repo.GetAll();
            if (list.Count == 0)
            {
                var s = new SiteSetting { Status = Status.Active };
                await repo.AddAsync(s);
                await _uow.SaveChangesAsync();
            }
        }

        public async Task<SiteSettingDto> GetAsync()
        {
            var repo = _uow.Repository<SiteSetting>();
            var all = await repo.GetAll();
            var s = all.OrderBy(x => x.CreatedDate).FirstOrDefault();
            if (s == null)
            {
                await EnsureSeedAsync();
                all = await repo.GetAll();
                s = all.First();
            }
            return s.Adapt<SiteSettingDto>();
        }

        public async Task UpdateAsync(SiteSettingUpdateDto dto)
        {
            var repo = _uow.Repository<SiteSetting>();
            var e = await repo.GetById(dto.Id);
            if (e == null) throw new Exception("Ayar bulunamadı");
            dto.Adapt(e);
            await repo.Update(e);
            await _uow.SaveChangesAsync();
        }
    }

}
