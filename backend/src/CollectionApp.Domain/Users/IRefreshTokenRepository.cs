namespace CollectionApp.Domain.Users
{
    public interface IRefreshTokenRepository
    {
        /// <summary>
        /// Session matching the hash of a refresh token value, null if unknown
        /// </summary>
        Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

        Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
