using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CollectionApp.Application.Auth;
using CollectionApp.Application.Security;
using CollectionApp.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CollectionApp.Infrastructure.Security
{
    /// <summary>
    /// Issues HS256 access tokens and opaque refresh tokens (research R2, R3)
    /// </summary>
    public class JwtTokenService : ITokenService
    {
        private const int RefreshTokenBytes = 64;

        private readonly AuthOptions _options;
        private readonly JsonWebTokenHandler _handler = new();

        public JwtTokenService(IOptions<AuthOptions> options)
        {
            _options = options.Value;
        }

        /// <summary>
        /// Signing key shared by the token issuer and the JwtBearer validation
        /// </summary>
        public static SymmetricSecurityKey CreateSigningKey(AuthOptions options) =>
            new(Encoding.UTF8.GetBytes(options.SigningKey));

        public AccessToken CreateAccessToken(User user, IEnumerable<string> claims, DateTime now)
        {
            var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
            var id = user.Id.ToString();

            var tokenClaims = new List<Claim>
            {
                new(ClaimPrincipalTypes.Subject, id),
                // Read by ClaimPrincipalExtension.GetUserId()
                new(ClaimPrincipalTypes.UserId, id),
                new(ClaimPrincipalTypes.Email, user.Email),
                new(ClaimPrincipalTypes.Firstname, user.FirstName),
                new(ClaimPrincipalTypes.Lastname, user.LastName),
            };
            tokenClaims.AddRange(claims.Select(c => new Claim(ClaimPrincipalTypes.Claim, c)));

            var token = _handler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Subject = new ClaimsIdentity(tokenClaims),
                IssuedAt = now,
                NotBefore = now,
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(CreateSigningKey(_options), SecurityAlgorithms.HmacSha256),
            });

            return new AccessToken(token, expiresAt);
        }

        public string CreateRefreshTokenValue() =>
            Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));

        public string HashRefreshToken(string value) =>
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
