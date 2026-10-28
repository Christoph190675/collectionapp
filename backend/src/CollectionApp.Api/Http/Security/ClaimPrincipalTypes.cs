using System.Security.Claims;

namespace CollectionApp.Api.Http.Security {

     public class ClaimPrincipalTypes {
        public const string NameIdentifier = ClaimTypes.NameIdentifier;
        public const string Endpoint = "ept";
        public const string GrantType = "gt";
        public const string Scope = "scp";
        public const string ResponseType = "rst";
        public const string Subject = "sub";
        public const string Name = "name";
        public const string Role = "roles";
        public const string Claim = "claims";
        public const string Email = "email";
        public const string Firstname = "firstname";
        public const string Lastname = "lastname";
        public const string TenantId = "tenantId";
        public const string TenantCode = "tenantCode";
        public const string HierarchyId = "hierarchyId";
        public const string CompanyId = "companyId";
        public const string UserId = "userId";
        public const string NameId = "nameid";
        public const string SessionId = "sessionId";
        public const string ApplicationId = "applicationId";
        public const string Owns = "owns";
        public const string ImpersonatorId = "impersonatorId";
        public const string RefreshKey = "refreshKey";
        public const string From = "from";
        public const string To = "to";
     }
    
}