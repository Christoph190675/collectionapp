using System.Net;
using System.Net.Http.Json;
using CollectionApp.Application.Auth;
using CollectionApp.Application.Security;
using CollectionApp.Api.Http.Security;
using CollectionApp.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CollectionApp.Tests.Auth;

/// <summary>
/// GET /api/auth/me and the identity carried by the access token (US2-AC7, analysis H1).
/// </summary>
[Collection("MySql")]
public class MeTests(MySqlContainerFixture mySql)
{
    [Fact]
    public async Task US2_AC7_ValidAccessToken_ReturnsCurrentUser()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var (client, session) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(AuthTestData.MeUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<UserResponse>(AuthTestData.Json))!;
        Assert.Equal(session.User.Id, user.Id);
        Assert.Equal(["User"], user.Claims);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));

        var response = await factory.CreateClient().GetAsync(AuthTestData.MeUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task H1_IssuedToken_PopulatesCurrentUserId()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var (_, session) = await factory.CreateAuthenticatedClientAsync();

        // Validate the token exactly like the API does, then read it through ICurrentUser
        var bearerOptions = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var parameters = bearerOptions.TokenValidationParameters.Clone();
        parameters.LifetimeValidator = (_, _, _, _) => true;
        var result = await new JsonWebTokenHandler { MapInboundClaims = false }.ValidateTokenAsync(session.AccessToken, parameters);
        Assert.True(result.IsValid);

        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new(result.ClaimsIdentity) } };
        ICurrentUser currentUser = new CurrentUser(accessor);

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(session.User.Id, currentUser.UserId);
        Assert.True(currentUser.HasClaim("User"));
    }
}
