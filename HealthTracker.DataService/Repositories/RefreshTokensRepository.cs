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
    public class RefreshTokensRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
    {

        public RefreshTokensRepository(AppDbContext context, ILogger logger) : base(context, logger)
        {

        }

        public override async Task<IEnumerable<RefreshToken>> GetAll()
        {
            try
            {
                return await dbSet.Where(user => user.Status == 1).AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "{The RefreshTokenRepository GetAll Method has generated an error}", typeof(RefreshTokensRepository));
                return new List<RefreshToken> { };
            }

        }

        public async Task<RefreshToken> GetByRefreshToken(string refreshToken)
        {
            try
            {
                return await _context.RefreshTokens.Where(x => x.Token.ToLower() == refreshToken.ToLower()).AsNoTracking().FirstOrDefaultAsync();
            } catch(Exception ex)
            {
                _logger.LogError(ex.Message, "{The GetByRefreshToken GetAll Method has generated an error}", typeof(RefreshTokensRepository));
                return null;
            }
        }

        public async Task<bool> MarkRefreshTokenAsUsed(RefreshToken refreshToken)
        {
            try
            {
                var token = await _context.RefreshTokens.Where(x => x.Token.ToLower() == refreshToken.Token.ToLower()).AsNoTracking().FirstOrDefaultAsync();

                if(token == null) return false;

                token.IsUsed = refreshToken.IsUsed;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "{The GetByRefreshToken GetAll Method has generated an error}", typeof(RefreshTokensRepository));
                return false;
            }
        }
    }
}
