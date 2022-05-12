using HealthTracker.Entities.DbSet;
using System.Threading.Tasks;

namespace HealthTracker.DataService.IRepository
{
    public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
    {
        Task<RefreshToken> GetByRefreshToken(string refreshToken);
        Task<bool> MarkRefreshTokenAsUsed(RefreshToken refreshToken);
    }
}
