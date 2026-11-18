using System.Net;
using System.Net.Http.Json;
using CollectionApp.Domain.Users;
using CollectionApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CollectionApp.Tests.Auth;

/// <summary>
/// POST /api/auth/register (User Story 1).
/// </summary>
[Collection("MySql")]
public class RegisterTests(MySqlContainerFixture mySql)
{
    [Fact]
    public async Task US1_AC1_ValidData_Returns201WithNormalizedEmail()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);

        var response = await factory.CreateClient().PostAsJsonAsync(
            AuthTestData.RegisterUrl, AuthTestData.RegisterRequest(" Test@Example.fr ", lastName: " Pinto ", firstName: " Christoph "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);
        var user = (await response.Content.ReadFromJsonAsync<UserResponse>(AuthTestData.Json))!;
        Assert.True(user.Id > 0);
        Assert.Equal("test@example.fr", user.Email);
        Assert.Equal("Pinto", user.LastName);
        Assert.Equal("Christoph", user.FirstName);
    }

    [Fact]
    public async Task US1_AC2_SameEmailDifferentCase_Returns409()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);
        var client = factory.CreateClient();
        await AuthTestData.RegisterAsync(client, "a@b.fr");

        var response = await client.PostAsJsonAsync(AuthTestData.RegisterUrl, AuthTestData.RegisterRequest("A@B.fr"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await AuthTestData.ReadErrorAsync(response);
        Assert.Equal("Cette adresse e-mail est déjà utilisée", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task US1_AC3_InvalidFields_Returns400WithFieldErrors()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);

        var response = await factory.CreateClient().PostAsJsonAsync(
            AuthTestData.RegisterUrl, AuthTestData.RegisterRequest("abc", "abcdefgh"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await AuthTestData.ReadErrorAsync(response);
        var fields = error.GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("field").GetString()).Distinct();
        Assert.Equal(["email", "password"], fields.Order());
    }

    [Fact]
    public async Task US1_AC4_ConfirmationMismatch_Returns400()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);

        var response = await factory.CreateClient().PostAsJsonAsync(
            AuthTestData.RegisterUrl, AuthTestData.RegisterRequest(AuthTestData.NewEmail(), confirmation: "Collec2027"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await AuthTestData.ReadErrorAsync(response);
        Assert.Equal("passwordConfirmation", error.GetProperty("fields")[0].GetProperty("field").GetString());
    }

    [Fact]
    public async Task US1_AC5_NewUser_HasOnlyUserClaim()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);

        var user = await AuthTestData.RegisterAsync(factory.CreateClient(), AuthTestData.NewEmail());

        Assert.Equal([ClaimValue.DefaultClaimName], user.Claims);
        await using var db = MySqlContainerFixture.CreateDbContext(connectionString);
        var claimIds = await db.UserClaims.Where(uc => uc.UserId == user.Id).Select(uc => uc.ClaimId).ToListAsync();
        Assert.Equal([ClaimValue.DefaultClaimId], claimIds);
    }

    [Fact]
    public async Task FR005_PasswordStoredHashed()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);

        var user = await AuthTestData.RegisterAsync(factory.CreateClient(), AuthTestData.NewEmail());

        await using var db = MySqlContainerFixture.CreateDbContext(connectionString);
        var hash = await db.Users.Where(u => u.Id == user.Id).Select(u => u.PasswordHash).SingleAsync();
        Assert.NotEqual(AuthTestData.ValidPassword, hash);
        Assert.DoesNotContain(AuthTestData.ValidPassword, hash);
        // Identity V3 format marker (0x01) in base64
        Assert.StartsWith("AQAAAA", hash);
    }

    [Fact]
    public async Task EdgeCase_ConcurrentRegistrations_OnlyOneSucceeds()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);
        var email = AuthTestData.NewEmail();

        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            factory.CreateClient().PostAsJsonAsync(AuthTestData.RegisterUrl, AuthTestData.RegisterRequest(email))));

        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], responses.Select(r => r.StatusCode).Order());
    }
}
