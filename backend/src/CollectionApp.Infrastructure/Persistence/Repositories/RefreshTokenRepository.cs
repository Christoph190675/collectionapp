using CollectionApp.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CollectionApp.Infrastructure.Persistence.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly CollectionAppDbContext _dbContext;

        public RefreshTokenRepository(CollectionAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return _dbContext.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        }

        public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
