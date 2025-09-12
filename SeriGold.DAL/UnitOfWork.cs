using SeriGold.Core.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.DAL
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IEFContext _context;
        private readonly Dictionary<Type, object> _repos = new();
        public UnitOfWork(IEFContext context) { _context = context; }
        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync(default);

        public IBaseRepository<T> Repository<T>() where T : class, IEntity
        {
            var t = typeof(T);
            if (!_repos.TryGetValue(t, out var repo))
            {
                repo = new BaseRepository<T>(_context);
                _repos.Add(t, repo);
            }
            return (IBaseRepository<T>)repo;
        }
    }
}
