using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CollectionApp.Tests.Auth;

public sealed record UserResponse(int Id, string LastName, string FirstName, string Email, string[] Claims);

public sealed record AuthSession(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserResponse User);

/// <summary>
/// Shared data and calls of the /api/auth tests
/// </summary>
public static class AuthTestData
{
    public const string ValidPassword = "Collec2026";

    public const string RegisterUrl = "/api/auth/register";
    public const string LoginUrl = "/api/auth/login";
    public const string RefreshUrl = "/api/auth/refresh";
    public const string LogoutUrl = "/api/auth/logout";
    public const string MeUrl = "/api/auth/me";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string NewEmail() => $"user-{Guid.NewGuid():N}@example.fr";

    public static object RegisterRequest(string email, string password = ValidPassword, string? confirmation = null,
        string lastName = "Pinto", string firstName = "Christoph") => new
    {
        lastName,
        firstName,
        email,
        password,
        passwordConfirmation = confirmation ?? password,
    };

    public static async Task<UserResponse> RegisterAsync(HttpClient client, string email, string password = ValidPassword)
    {
        var response = await client.PostAsJsonAsync(RegisterUrl, RegisterRequest(email, password));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserResponse>(Json))!;
    }

    public static Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync(LoginUrl, new { email, password });

    public static async Task<AuthSession> LoginAsync(HttpClient client, string email, string password = ValidPassword)
    {
        var response = await PostLoginAsync(client, email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthSession>(Json))!;
    }

    /// <summary>
    /// Error body of the API (HttpExceptionBodyError): { id, code, message, status, fields? }
    /// </summary>
    public static async Task<JsonElement> ReadErrorAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
