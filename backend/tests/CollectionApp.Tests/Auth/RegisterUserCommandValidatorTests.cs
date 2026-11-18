using CollectionApp.Application.Auth;

namespace CollectionApp.Tests.Auth;

/// <summary>
/// Registration rules: FR-003, FR-004, US1-AC3, US1-AC4 (data-model.md, users).
/// </summary>
public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    private static RegisterUserCommand Valid() =>
        new("Pinto", "Christoph", "christoph@example.fr", AuthTestData.ValidPassword, AuthTestData.ValidPassword);

    private IEnumerable<string> InvalidFields(RegisterUserCommand command) =>
        _validator.Validate(command).Errors.Select(e => e.PropertyName).Distinct();

    [Fact]
    public void ValidCommand_HasNoError()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void US1_AC3_EmptyNames_AreRejected(string name)
    {
        Assert.Equal(["FirstName", "LastName"], InvalidFields(Valid() with { LastName = name, FirstName = name }).Order());
    }

    [Fact]
    public void US1_AC3_NamesLongerThan100_AreRejected()
    {
        var tooLong = new string('a', 101);

        Assert.Equal(["FirstName", "LastName"], InvalidFields(Valid() with { LastName = tooLong, FirstName = tooLong }).Order());
    }

    [Fact]
    public void Names_100CharactersAfterTrim_AreAccepted()
    {
        var name = " " + new string('a', 100) + " ";

        Assert.True(_validator.Validate(Valid() with { LastName = name, FirstName = name }).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("christoph@")]
    public void US1_AC3_InvalidEmail_IsRejected(string email)
    {
        Assert.Equal(["Email"], InvalidFields(Valid() with { Email = email }));
    }

    [Fact]
    public void US1_AC3_EmailLongerThan254_IsRejected()
    {
        var email = new string('a', 245) + "@example.fr";

        Assert.Equal(["Email"], InvalidFields(Valid() with { Email = email }));
    }

    [Theory]
    [InlineData("abcdefgh")]
    [InlineData("12345678")]
    [InlineData("abc1234")]
    public void US1_AC3_NonCompliantPassword_IsRejected(string password)
    {
        Assert.Contains("Password", InvalidFields(Valid() with { Password = password, PasswordConfirmation = password }));
    }

    [Fact]
    public void PasswordLongerThan128_IsRejected()
    {
        var password = "a1" + new string('b', 127);

        Assert.Contains("Password", InvalidFields(Valid() with { Password = password, PasswordConfirmation = password }));
    }

    [Fact]
    public void US1_AC4_ConfirmationMismatch_IsRejected()
    {
        Assert.Equal(["PasswordConfirmation"], InvalidFields(Valid() with { PasswordConfirmation = "Collec2027" }));
    }
}
