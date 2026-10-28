using CollectionApp.Application.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CollectionApp.Infrastructure.Persistence
{
    /// <summary>
    /// Checks that the database is reachable and that every known migration has been applied.
    /// The failure cause is only logged, never returned (FR-005b).
    /// </summary>
    public class DatabaseHealthChecker : IDatabaseHealthChecker
    {
        public static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(2);

        private readonly CollectionAppDbContext _dbContext;
        private readonly ILogger<DatabaseHealthChecker> _logger;

        public DatabaseHealthChecker(CollectionAppDbContext dbContext, ILogger<DatabaseHealthChecker> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TIMEOUT);

            try
            {
                // WaitAsync bounds the check even if the driver ignores the cancellation while opening the socket
                return await CheckAsync(timeoutSource.Token).WaitAsync(TIMEOUT, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database health check failed");
                return false;
            }
        }

        private async Task<bool> CheckAsync(CancellationToken cancellationToken)
        {
            if (!await _dbContext.Database.CanConnectAsync(cancellationToken))
            {
                _logger.LogWarning("Database health check failed: unable to connect to the database");
                return false;
            }

            var pendingMigrations = (await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pendingMigrations.Count > 0)
            {
                _logger.LogWarning("Database health check failed: pending migrations {PendingMigrations}",
                    string.Join(", ", pendingMigrations));
                return false;
            }

            return true;
        }
    }
}
