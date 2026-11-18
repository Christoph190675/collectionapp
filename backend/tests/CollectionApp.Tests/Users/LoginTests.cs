using CollectionApp.Domain.Users;

namespace CollectionApp.Tests.Users;

/// <summary>
/// Lock rules of the login state (research R8, FR-009 to FR-011).
/// </summary>
public class LoginTests
{
    private static readonly DateTime Start = new(2026, 11, 18, 10, 0, 0, DateTimeKind.Utc);

    private static Login FailTimes(int count)
    {
        var login = new Login();
        for (var i = 0; i < count; i++)
        {
            login.RegisterFailure(Start);
        }
        return login;
    }

    [Fact]
    public void FourFailures_NotLocked()
    {
        var login = FailTimes(4);

        Assert.False(login.IsLocked(Start));
        Assert.Null(login.LockedUntil);
    }

    [Fact]
    public void FifthFailure_LocksFor15Minutes()
    {
        var login = FailTimes(5);

        Assert.True(login.IsLocked(Start));
        Assert.Equal(Start.AddMinutes(15), login.LockedUntil);
    }

    [Fact]
    public void Lock_StillActiveJustBefore15Minutes()
    {
        Assert.True(FailTimes(5).IsLocked(Start.AddMinutes(15).AddSeconds(-1)));
    }

    [Fact]
    public void Lock_OverAt15Minutes()
    {
        Assert.False(FailTimes(5).IsLocked(Start.AddMinutes(15)));
    }

    [Fact]
    public void ResetIfLockExpired_ResetsCounterOnlyAfterTheLock()
    {
        var login = FailTimes(5);

        login.ResetIfLockExpired(Start.AddMinutes(10));
        Assert.Equal(5, login.NbrTries);

        login.ResetIfLockExpired(Start.AddMinutes(15));
        Assert.Equal(0, login.NbrTries);
    }

    [Fact]
    public void ResetIfLockExpired_KeepsFailuresBelowTheLimit()
    {
        var login = FailTimes(3);

        login.ResetIfLockExpired(Start.AddHours(1));

        Assert.Equal(3, login.NbrTries);
    }

    [Fact]
    public void Success_ResetsCounterAndUpdatesDate()
    {
        var login = FailTimes(3);

        login.RegisterSuccess(Start.AddMinutes(1));

        Assert.Equal(0, login.NbrTries);
        Assert.Equal(Start.AddMinutes(1), login.DateLogin);
    }

    [Fact]
    public void Failure_IncrementsCounterAndUpdatesDate()
    {
        var login = new Login();

        login.RegisterFailure(Start.AddMinutes(2));

        Assert.Equal(1, login.NbrTries);
        Assert.Equal(Start.AddMinutes(2), login.DateLogin);
    }

    [Fact]
    public void NormalizeEmail_TrimsAndLowersCase()
    {
        Assert.Equal("a@b.fr", User.NormalizeEmail(" A@B.fr "));
    }
}
