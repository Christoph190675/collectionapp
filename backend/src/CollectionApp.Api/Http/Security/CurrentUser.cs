using System.Security.Claims;
using CollectionApp.Application.Security;

namespace CollectionApp.Api.Http.Security
{
    /// <summary>
    /// ICurrentUser implementation based on the claims of the current HttpContext
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

        public int? UserId => Principal?.GetUserId();

        public string Subject => Principal?.GetSubject() ?? "";

        public bool HasClaim(string claim) => Principal?.HasClaim(claim) ?? false;
    }
}
