using HealthTracker.DataService.Data;
using HealthTracker.DataService.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthTracker.DataService.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected AppDbContext _context;
        internal DbSet<T> dbSet;
        protected readonly ILogger _logger;

        public GenericRepository(AppDbContext context, ILogger logger)
        {
            _context = context;
            dbSet = context.Set<T>();
            _logger = logger;
        }

        public async virtual Task<bool> Add(T entity)
        {
            await dbSet.AddAsync(entity);
            return (await _context.SaveChangesAsync()) > 0;
        }

        public virtual Task<bool> Delete(Guid id, string userId)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<IEnumerable<T>> GetAll()
        {
            return await dbSet.ToListAsync();
        }

        public virtual async Task<T> GetById(Guid id)
        {
            return await dbSet.FindAsync(id);
        }

        public virtual Task<bool> Upsert(T entity)
        {
            throw new NotImplementedException();
        }
    }
}
