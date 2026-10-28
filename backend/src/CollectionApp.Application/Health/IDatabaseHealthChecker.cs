namespace CollectionApp.Application.Health
{
    /// <summary>
    /// Checks the database health.
    /// </summary>
    public interface IDatabaseHealthChecker
    {
        /// <summary>
        /// Check the database.
        /// </summary>
        /// <param name="cancellationToken">A CancellationToken</param>
        /// <returns>true if and only if the connection succeeds AND no migration is pending</returns>
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
    }
}
