namespace CollectionApp.Application.Security
{
    /// <summary>
    /// The user executing the current request, independent of the authentication mechanism
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// True if the user is authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// The user unique identifier, null if unknown
        /// </summary>
        int? UserId { get; }

        /// <summary>
        /// The subject, empty if unknown
        /// </summary>
        string Subject { get; }

        /// <summary>
        /// Check if the user has the specified claim or not
        /// </summary>
        /// <param name="claim">A claim</param>
        /// <returns>a boolean</returns>
        bool HasClaim(string claim);
    }
}
