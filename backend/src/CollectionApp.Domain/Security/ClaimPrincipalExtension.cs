using System.Security.Claims;

namespace CollectionApp.Domain.Security
{
    public static class ClaimPrincipalExtension
    {

        /// <summary>
        /// Get the connected user unique uidentifier
        /// </summary>
        /// <param name="accessor">An ClaimsPrincipal</param>
        /// <returns>The user unique identifier</returns>
        public static int? GetUserId(this ClaimsPrincipal claimsPrincipal)
        {
            var strId = claimsPrincipal?.FindFirst(ClaimPrincipalTypes.UserId)?.Value ?? claimsPrincipal?.FindFirst(ClaimPrincipalTypes.NameIdentifier)?.Value;
            int number;
            bool success = int.TryParse(strId, out number);
            if (success)
                return number;
            else
                return null;
        }

        /// <summary>
        /// check if the ClaimPrinciapl has the specified claim or not
        /// </summary>
        /// <param name="claimsPrincipal">A ClaimsPrincipal</param>
        /// <param name="claim">A claim</param>
        /// <returns>a boolean</returns>
        public static bool HasClaim(this ClaimsPrincipal claimsPrincipal, string claim)
        {
            return claimsPrincipal.HasClaim(ClaimPrincipalTypes.Claim, claim);
        }

        /// <summary>
        /// check if the ClaimPrinciapl has the specified claim or not
        /// </summary>
        /// <param name="claimsPrincipal">A ClaimsPrincipal</param>
        /// <param name="type">A claim type</param>
        /// <param name="claim">A claim</param>
        /// <returns>a boolean</returns>
        public static bool HasClaim(this ClaimsPrincipal claimsPrincipal, string type, string claim)
        {
            return claimsPrincipal.HasClaim(x => x.Type == type && x.Value == claim);
        }

        /// <summary>
        /// Get the subject
        /// </summary>
        /// <param name="claimsPrincipal">A ClaimsPrincipal</param>
        /// <returns>The subject</returns>
        public static string GetSubject(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal?.FindFirst(ClaimPrincipalTypes.Subject)?.Value ?? "";
        }

        /// <summary>
        /// Gets the claim value corresponding to the given type.
        /// </summary>
        /// <param name="principal">The principal.</param>
        /// <param name="type">The type associated with the claim.</param>
        /// <returns>The claim value.</returns>
        public static string GetClaim(this ClaimsPrincipal principal, string type)
        {
            return principal?.FindFirst(type)?.Value ?? "";
        }


        
    }
}