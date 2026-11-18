using CollectionApp.Application.Auth;
using CollectionApp.Domain.Exceptions;
using CollectionApp.Domain.Users;
using CollectionApp.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace CollectionApp.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        /// <summary>MySQL error "Duplicate entry"</summary>
        private const int DuplicateEntryErrorNumber = 1062;

        private readonly CollectionAppDbContext _dbContext;

        public UserRepository(CollectionAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
        }

        public Task<User?> GetByEmailWithLoginAndClaimsAsync(string email, CancellationToken cancellationToken)
        {
            return _dbContext.Users
                .Include(u => u.Login)
                .Include(u => u.UserClaims).ThenInclude(uc => uc.Claim)
                .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public Task<User?> GetByIdWithClaimsAsync(int id, CancellationToken cancellationToken)
        {
            return _dbContext.Users
                .Include(u => u.UserClaims).ThenInclude(uc => uc.Claim)
                .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public Task<ClaimValue?> GetClaimByNameAsync(string name, CancellationToken cancellationToken)
        {
            return _dbContext.ClaimValues.SingleOrDefaultAsync(c => c.Name == name, cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            await _dbContext.Users.AddAsync(user, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            // Two concurrent registrations with the same e-mail: the unique index decides (research R5)
            catch (DbUpdateException e) when (e.InnerException is MySqlException { Number: DuplicateEntryErrorNumber } mySqlException
                                               && mySqlException.Message.Contains(UserConfiguration.EmailIndexName))
            {
                throw new AlreadyExistException(RegisterUserCommandHandler.EmailAlreadyUsedMessage);
            }
        }
    }
}
