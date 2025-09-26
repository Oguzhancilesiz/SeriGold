// Services/Concrete/FaqService.cs
using Mapster;
using Microsoft.EntityFrameworkCore;
using SeriGold.Core.Abstracts;
using SeriGold.DTO;
using SeriGold.Entity;
using SeriGold.Services.Abstracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SeriGold.Services.Concrete
{
    public sealed class FaqService : IFaqService
    {
        private readonly IUnitOfWork _uow;
        public FaqService(IUnitOfWork uow) => _uow = uow;

        public async Task Add(FaqAddDto item)
        {
            var e = item.Adapt<FaqItem>();
            await _uow.Repository<FaqItem>().AddAsync(e);
            await _uow.SaveChangesAsync();
        }

        public async Task Delete(Guid id)
        {
            var r = _uow.Repository<FaqItem>();
            var e = await r.GetById(id);
            if (e != null)
            {
                await r.Delete(e);
                await _uow.SaveChangesAsync();
            }
        }

        public async Task<List<FaqDto>> GetAll()
        {
            var list = await _uow.Repository<FaqItem>().GetAllAsync();
            // görüntü her zaman stabil olsun:
            return list.OrderBy(x => x.DisplayOrder).Select(x => x.Adapt<FaqDto>()).ToList();
        }

        public async Task<FaqDto> GetById(Guid id)
        {
            var e = await _uow.Repository<FaqItem>().GetById(id);
            if (e == null) throw new Exception("SSS bulunamadı");
            return e.Adapt<FaqDto>();
        }

        public async Task<(IReadOnlyList<FaqDto> Items, int Total)> Paged(int page, int size)
        {
            var (items, total) = await _uow.Repository<FaqItem>()
                .PagedAsync(page, size, orderBy: q => q.OrderBy(x => x.DisplayOrder));
            return (items.Select(x => x.Adapt<FaqDto>()).ToList(), total);
        }

        public async Task Update(FaqUpdateDto item)
        {
            var r = _uow.Repository<FaqItem>();
            var e = await r.GetById(item.Id);
            if (e == null) throw new Exception("SSS bulunamadı");
            item.Adapt(e);
            await r.Update(e);
            await _uow.SaveChangesAsync();
        }

        // YENİ: toplu sıra güncelle
        public async Task ReorderAsync(IEnumerable<FaqOrderUpdateDto> items)
        {
            var map = items?.ToDictionary(x => x.Id, x => x.DisplayOrder) ?? new Dictionary<Guid, int>();
            if (map.Count == 0) return;

            var repo = _uow.Repository<FaqItem>();
            var ids = map.Keys.ToList();

            var q = await repo.GetBy(x => ids.Contains(x.Id));
            var list = await q.ToListAsync();

            foreach (var e in list)
            {
                if (map.TryGetValue(e.Id, out var order))
                    e.DisplayOrder = order;
                await repo.Update(e);
            }
            await _uow.SaveChangesAsync();
        }
    }
}
