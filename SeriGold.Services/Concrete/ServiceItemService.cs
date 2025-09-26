// Services/Concrete/ServiceItemService.cs
using Mapster;
using SeriGold.Core.Abstracts;
using SeriGold.DTO;
using SeriGold.Entity;
using SeriGold.Services.Abstracts;

namespace SeriGold.Services.Concrete;
public sealed class ServiceItemService : IServiceItemService
{
    private readonly IUnitOfWork _uow;
    public ServiceItemService(IUnitOfWork uow) => _uow = uow;

    public async Task Add(ServiceItemAddDto item)
    {
        var e = item.Adapt<ServiceItem>();
        await _uow.Repository<ServiceItem>().AddAsync(e);
        await _uow.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        var r = _uow.Repository<ServiceItem>();
        var e = await r.GetById(id);
        if (e != null)
        {
            await r.Delete(e);
            await _uow.SaveChangesAsync();
        }
    }

    public async Task<List<ServiceItemDto>> GetAll()
    {
        var list = await _uow.Repository<ServiceItem>().GetAll();
        return list
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.Adapt<ServiceItemDto>())
            .ToList();
    }


    public async Task<ServiceItemDto> GetById(Guid id)
    {
        var e = await _uow.Repository<ServiceItem>().GetById(id);
        if (e == null) throw new Exception("Hizmet bulunamadı");
        return e.Adapt<ServiceItemDto>();
    }

    public async Task<(IReadOnlyList<ServiceItemDto> Items, int Total)> Paged(int page, int size)
    {
        var (items, total) = await _uow.Repository<ServiceItem>()
            .PagedAsync(page, size, orderBy: q => q.OrderBy(x => x.DisplayOrder));
        return (items.Select(x => x.Adapt<ServiceItemDto>()).ToList(), total);
    }

    public async Task Update(ServiceItemUpdateDto item)
    {
        var r = _uow.Repository<ServiceItem>();
        var e = await r.GetById(item.Id);
        if (e == null) throw new Exception("Hizmet bulunamadı");
        item.Adapt(e);
        await r.Update(e);
        await _uow.SaveChangesAsync();
    }
}
