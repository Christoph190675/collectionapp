namespace CollectionApp.Application.Auth
{
    public sealed record AuthSessionDto(
        string AccessToken,
        DateTime AccessTokenExpiresAt,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt,
        UserDto User);
}
