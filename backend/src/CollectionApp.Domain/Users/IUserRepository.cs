namespace CollectionApp.Domain.Users
{
    public interface IUserRepository
    {
        /// <param name="email">A normalized e-mail (see User.NormalizeEmail)</param>
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

        /// <summary>
        /// User with its login state and its rights, null if unknown
        /// </summary>
        /// <param name="email">A normalized e-mail (see User.NormalizeEmail)</param>
        Task<User?> GetByEmailWithLoginAndClaimsAsync(string email, CancellationToken cancellationToken);

        Task<User?> GetByIdWithClaimsAsync(int id, CancellationToken cancellationToken);

        Task<ClaimValue?> GetClaimByNameAsync(string name, CancellationToken cancellationToken);

        Task AddAsync(User user, CancellationToken cancellationToken);

        /// <summary>
        /// Save the changes; a duplicated e-mail raises an AlreadyExistException
        /// </summary>
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
