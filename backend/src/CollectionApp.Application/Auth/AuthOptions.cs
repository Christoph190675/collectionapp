namespace CollectionApp.Application.Auth
{
    /// <summary>
    /// "Jwt" configuration section. SigningKey comes from user-secrets / environment, never from Git.
    /// </summary>
    public class AuthOptions
    {
        public const string SectionName = "Jwt";

        /// <summary>Minimum size of the HS256 signing key</summary>
        public const int MinSigningKeyBytes = 32;

        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public string SigningKey { get; set; } = "";
        public int AccessTokenMinutes { get; set; } = 15;
        public int RefreshTokenDays { get; set; } = 7;
    }
}
