namespace CollectionApp.Domain.Users
{
    /// <summary>
    /// A named right that can be granted to users. Reference data seeded by the migration.
    /// </summary>
    public class ClaimValue
    {
        /// <summary>Right granted to every new user (FR-006)</summary>
        public const int DefaultClaimId = 1;
        public const string DefaultClaimName = "User";

        public const int LogReadId = 2;
        public const string LogReadName = "LogRead";

        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        public ICollection<UserClaim> UserClaims { get; set; } = new List<UserClaim>();
    }
}
