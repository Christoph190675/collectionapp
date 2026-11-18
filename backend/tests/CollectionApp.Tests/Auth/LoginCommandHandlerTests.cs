using CollectionApp.Application.Auth;
using CollectionApp.Domain.Exceptions;
using CollectionApp.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace CollectionApp.Tests.Auth;

/// <summary>
/// Login rules without database (research R7, R8).
/// </summary>
public class LoginCommandHandlerTests
{
    private const string Email = "christoph@example.fr";
    private const string GoodHash = "good-hash";

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 18, 10, 0, 0, TimeSpan.Zero));
    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _tokens = new();
    private readonly FakePasswordHasher _hasher = new();

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    private LoginCommandHandler CreateHandler() =>
        new(_users, _tokens, _hasher, new FakeTokenService(), _clock, Options.Create(new AuthOptions()));

    private User AddUser(Login? login = null)
    {
        var user = new User { Id = 7, Email = Email, PasswordHash = GoodHash, Login = login };
        user.UserClaims.Add(new UserClaim { Claim = new ClaimValue { Name = ClaimValue.DefaultClaimName } });
        _users.User = user;
        return user;
    }

    [Fact]
    public async Task UnknownEmail_VerifiesDummyHashThenUnauthorized()
    {
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new LoginCommand(Email, "Collec2026"), CancellationToken.None));

        Assert.Equal(LoginCommandHandler.InvalidCredentialsMessage, exception.Message);
        Assert.Equal([FakePasswordHasher.Dummy], _hasher.VerifiedHashes);
        Assert.Equal(0, _users.SaveCount);
    }

    [Fact]
    public async Task WrongPassword_SameMessageAsUnknownEmail()
    {
        AddUser();

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new LoginCommand(Email, "wrong"), CancellationToken.None));

        Assert.Equal(LoginCommandHandler.InvalidCredentialsMessage, exception.Message);
    }

    [Fact]
    public async Task LockedAccount_RefusedWithoutVerifyingNorUpdating()
    {
        var lockedAt = Now.AddMinutes(-5);
        AddUser(new Login { NbrTries = Login.MaxFailures, DateLogin = lockedAt });

        var exception = await Assert.ThrowsAsync<LockedException>(() =>
            CreateHandler().Handle(new LoginCommand(Email, "Collec2026"), CancellationToken.None));

        Assert.Equal(TimeSpan.FromMinutes(10), exception.RetryAfter);
        Assert.Empty(_hasher.VerifiedHashes);
        Assert.Equal(lockedAt, _users.User!.Login!.DateLogin);
        Assert.Equal(Login.MaxFailures, _users.User.Login.NbrTries);
        Assert.Equal(0, _users.SaveCount);
    }

    [Fact]
    public async Task FirstAttempt_CreatesLoginState()
    {
        AddUser();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new LoginCommand(Email, "wrong"), CancellationToken.None));

        Assert.NotNull(_users.User!.Login);
        Assert.Equal(1, _users.User.Login!.NbrTries);
        Assert.Equal(Now, _users.User.Login.DateLogin);
        Assert.Equal(1, _users.SaveCount);
    }

    [Fact]
    public async Task Success_StoresOnlyTheRefreshTokenHash()
    {
        AddUser();

        var session = await CreateHandler().Handle(new LoginCommand(" Christoph@Example.fr ", "Collec2026"), CancellationToken.None);

        var stored = Assert.Single(_tokens.Added);
        Assert.Equal("hash:" + session.RefreshToken, stored.TokenHash);
        Assert.Equal(Now.AddDays(7), stored.ExpiresAt);
        Assert.Equal(Now.AddDays(7), session.RefreshTokenExpiresAt);
        Assert.Equal([ClaimValue.DefaultClaimName], session.User.Claims);
    }

    [Fact]
    public async Task SuccessRehashNeeded_UpdatesHash()
    {
        var user = AddUser();
        _hasher.NextResult = PasswordCheck.SuccessRehashNeeded;

        await CreateHandler().Handle(new LoginCommand(Email, "Collec2026"), CancellationToken.None);

        Assert.Equal("rehashed:Collec2026", user.PasswordHash);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? User { get; set; }
        public int SaveCount { get; private set; }

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(User?.Email == email);
        public Task<User?> GetByEmailWithLoginAndClaimsAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(User?.Email == email ? User : null);
        public Task<User?> GetByIdWithClaimsAsync(int id, CancellationToken cancellationToken) => Task.FromResult(User?.Id == id ? User : null);
        public Task<ClaimValue?> GetClaimByNameAsync(string name, CancellationToken cancellationToken) => Task.FromResult<ClaimValue?>(null);
        public Task AddAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> Added { get; } = [];

        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
            Task.FromResult(Added.SingleOrDefault(t => t.TokenHash == tokenHash));
        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            Added.Add(refreshToken);
            return Task.CompletedTask;
        }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public const string Dummy = "dummy-hash";

        public List<string> VerifiedHashes { get; } = [];
        public PasswordCheck NextResult { get; set; } = PasswordCheck.Success;

        public string DummyHash => Dummy;
        public string Hash(string password) => "rehashed:" + password;
        public PasswordCheck Verify(string hash, string password)
        {
            VerifiedHashes.Add(hash);
            return hash == GoodHash && password == "Collec2026" ? NextResult : PasswordCheck.Failed;
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AccessToken CreateAccessToken(User user, IEnumerable<string> claims, DateTime now) => new("access", now.AddMinutes(15));
        public string CreateRefreshTokenValue() => Guid.NewGuid().ToString("N");
        public string HashRefreshToken(string value) => "hash:" + value;
    }
}
