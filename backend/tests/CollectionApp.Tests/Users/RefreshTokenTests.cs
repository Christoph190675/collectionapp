using CollectionApp.Domain.Users;

namespace CollectionApp.Tests.Users;

/// <summary>
/// Session rules (research R3, R4).
/// </summary>
public class RefreshTokenTests
{
    private static readonly DateTime Start = new(2026, 11, 18, 10, 0, 0, DateTimeKind.Utc);

    private static RefreshToken Issue() => RefreshToken.Issue(1, new string('a', 64), Start, Start.AddDays(7));

    [Fact]
    public void Issue_SetsDates()
    {
        var token = Issue();

        Assert.Equal(Start, token.CreatedAt);
        Assert.Equal(Start.AddDays(7), token.ExpiresAt);
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public void IsActive_UntilExpiry()
    {
        var token = Issue();

        Assert.True(token.IsActive(Start.AddDays(7).AddSeconds(-1)));
        Assert.False(token.IsActive(Start.AddDays(7)));
    }

    [Fact]
    public void Revoke_DeactivatesAndIsIdempotent()
    {
        var token = Issue();

        token.Revoke(Start.AddMinutes(1));
        token.Revoke(Start.AddMinutes(2));

        Assert.False(token.IsActive(Start.AddMinutes(3)));
        Assert.Equal(Start.AddMinutes(1), token.RevokedAt);
    }
}
