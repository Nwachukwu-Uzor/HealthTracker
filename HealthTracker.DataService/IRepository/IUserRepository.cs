using HealthTracker.Entities.DbSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthTracker.DataService.IRepository
{
    public interface IUserRepository : IGenericRepository<User>
    {
    }
}
