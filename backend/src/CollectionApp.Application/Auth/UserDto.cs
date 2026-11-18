using CollectionApp.Domain.Users;

namespace CollectionApp.Application.Auth
{
    /// <summary>
    /// Public view of a user: never the password nor its hash (FR-005)
    /// </summary>
    public sealed record UserDto(int Id, string LastName, string FirstName, string Email, IReadOnlyList<string> Claims)
    {
        public static UserDto From(User user) => new(
            user.Id,
            user.LastName,
            user.FirstName,
            user.Email,
            user.UserClaims.Select(uc => uc.Claim.Name).Order().ToList());
    }
}
