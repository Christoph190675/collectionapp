namespace CollectionApp.Domain.Users
{
    /// <summary>
    /// Right granted to a user (n:n join between users and claimValues)
    /// </summary>
    public class UserClaim
    {
        public int UserId { get; set; }
        public int ClaimId { get; set; }

        public User User { get; set; } = null!;
        public ClaimValue Claim { get; set; } = null!;
    }
}
