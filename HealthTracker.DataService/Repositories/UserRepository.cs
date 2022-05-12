using HealthTracker.DataService.Data;
using HealthTracker.DataService.IRepository;
using HealthTracker.Entities.DbSet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthTracker.DataService.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context, ILogger logger) : base(context, logger)
        { }

        public override async Task<IEnumerable<User>> GetAll()
        {
            try
            {
                return await dbSet.Where(user => user.Status == 1).AsNoTracking().ToListAsync();
            } catch(Exception ex)
            {
                _logger.LogError(ex.Message, "{The UserRepository GetAll Method has generated an error}", typeof(UserRepository));
                return new List<User> { };
            }
            
        }
    }
}
