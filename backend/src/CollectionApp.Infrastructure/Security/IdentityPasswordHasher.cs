using CollectionApp.Application.Auth;
using CollectionApp.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace CollectionApp.Infrastructure.Security
{
    /// <summary>
    /// ASP.NET Core Identity password hasher (V3: PBKDF2-HMAC-SHA512, random salt), used alone
    /// </summary>
    public class IdentityPasswordHasher : IPasswordHasher
    {
        private static readonly PasswordHasher<User> Hasher = new();

        // Computed once: same cost as a real verification
        private static readonly Lazy<string> Dummy = new(() => Hasher.HashPassword(new User(), Guid.NewGuid().ToString()));

        public string DummyHash => Dummy.Value;

        public string Hash(string password) => Hasher.HashPassword(new User(), password);

        public PasswordCheck Verify(string hash, string password)
        {
            return Hasher.VerifyHashedPassword(new User(), hash, password) switch
            {
                PasswordVerificationResult.Success => PasswordCheck.Success,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SuccessRehashNeeded,
                _ => PasswordCheck.Failed,
            };
        }
    }
}
