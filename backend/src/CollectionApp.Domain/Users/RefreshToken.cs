namespace CollectionApp.Domain.Users
{
    /// <summary>
    /// A session on one device, revocable by logout. Only the hash of the token value is stored.
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TokenHash { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public User User { get; set; } = null!;

        /// <param name="expiresAt">Absolute end of the session: kept unchanged by each rotation (FR-013)</param>
        public static RefreshToken Issue(int userId, string tokenHash, DateTime now, DateTime expiresAt)
        {
            return new RefreshToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiresAt = expiresAt,
            };
        }

        public bool IsActive(DateTime now) => RevokedAt == null && now < ExpiresAt;

        /// <summary>
        /// Idempotent: revoking again keeps the first revocation date
        /// </summary>
        public void Revoke(DateTime now)
        {
            RevokedAt ??= now;
        }
    }
}
