using SeriGold.Core.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.DAL
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly IEFContext _ctx;
        private readonly Dictionary<Type, object> _repos = new();

        public UnitOfWork(IEFContext ctx) => _ctx = ctx;

        public IBaseRepository<T> Repository<T>() where T : class, IEntity
        {
            var t = typeof(T);
            if (_repos.TryGetValue(t, out var r)) return (IBaseRepository<T>)r;

            var repo = new BaseRepository<T>(_ctx);
            _repos[t] = repo;
            return repo;
        }

        public Task<int> SaveChangesAsync() => _ctx.SaveChangesAsync(default);
    }
}
