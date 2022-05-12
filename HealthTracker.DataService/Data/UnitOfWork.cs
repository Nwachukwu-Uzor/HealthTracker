using HealthTracker.DataService.IConfiguration;
using HealthTracker.DataService.IRepository;
using HealthTracker.DataService.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthTracker.DataService.Data
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly ILogger _logger;
        private readonly AppDbContext _context;

        public IUserRepository Users { get; private set; }

        public IRefreshTokenRepository RefreshTokens { get; private set; }

        public UnitOfWork(AppDbContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger("dbs_log");
            Users = new UserRepository(context, _logger);
            RefreshTokens = new RefreshTokensRepository(context, _logger);
        }

        

        public async Task CompletedAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
