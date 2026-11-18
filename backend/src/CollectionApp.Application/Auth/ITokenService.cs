using CollectionApp.Domain.Users;

namespace CollectionApp.Application.Auth
{
    public sealed record AccessToken(string Value, DateTime ExpiresAt);

    public interface ITokenService
    {
        /// <summary>
        /// Signed JWT carrying the user identity and rights
        /// </summary>
        AccessToken CreateAccessToken(User user, IEnumerable<string> claims, DateTime now);

        /// <summary>
        /// New random refresh token value, sent to the client only
        /// </summary>
        string CreateRefreshTokenValue();

        /// <summary>
        /// Hash stored in database instead of the refresh token value
        /// </summary>
        string HashRefreshToken(string value);
    }
}
