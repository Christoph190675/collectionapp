namespace CollectionApp.Domain.Users
{
    /// <summary>
    /// A registered person. The password is only known as a hash (FR-005).
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string LastName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";

        public ICollection<UserClaim> UserClaims { get; set; } = new List<UserClaim>();
        public Login? Login { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        /// <summary>
        /// Normalized form of an e-mail, used both at registration and at login (FR-002)
        /// </summary>
        public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        public static User Create(string lastName, string firstName, string email, string passwordHash)
        {
            return new User
            {
                LastName = lastName.Trim(),
                FirstName = firstName.Trim(),
                Email = NormalizeEmail(email),
                PasswordHash = passwordHash,
            };
        }
    }
}
