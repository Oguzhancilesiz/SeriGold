using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Core.Abstracts
{
    public interface IBaseRepository<TEntity> where TEntity : IEntity
    {
        Task<TEntity> GetById(Guid id, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            Expression<Func<TEntity, bool>> predicate = null, bool ignoreQueryFilter = false);
        Task<List<TEntity>> GetAll();
        Task<IQueryable<TEntity>> GetBy(Expression<Func<TEntity, bool>> exp);
        Task<IQueryable<TEntity>> GetAllActives();
        Task AddAsync(TEntity item);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        Task Update(TEntity item, bool isComeFromDelete = false);
        Task Delete(TEntity item);
        Task HardDeleteRangeAsync();
        Task<bool> Save(CancellationToken cancellationToken);
        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null, bool ignoreQueryFilter = false);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate = null, bool ignoreQueryFilter = false);
        Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector,
                    Expression<Func<TEntity, bool>> predicate = null, bool ignoreQueryFilter = false);

        Task<(IReadOnlyList<TEntity> Items, int Total)> PagedAsync(
         int page, int pageSize,
         Expression<Func<TEntity, bool>> predicate = null,
         Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
         bool ignoreQueryFilter = false);
    }
}
