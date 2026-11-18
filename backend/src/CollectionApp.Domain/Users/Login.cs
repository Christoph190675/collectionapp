namespace CollectionApp.Domain.Users
{
    /// <summary>
    /// Login state of a user (one per user): date of the last attempt and consecutive failures.
    /// The account is locked for 15 minutes after 5 consecutive failures (FR-009 to FR-011).
    /// </summary>
    public class Login
    {
        public const int MaxFailures = 5;
        public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime DateLogin { get; set; }
        public int NbrTries { get; set; }

        public User User { get; set; } = null!;

        /// <summary>
        /// End of the lock, null if the account has not reached the failure limit
        /// </summary>
        public DateTime? LockedUntil => NbrTries >= MaxFailures ? DateLogin + LockDuration : null;

        /// <summary>
        /// A locked attempt does not update DateLogin, so the lock never gets extended
        /// </summary>
        public bool IsLocked(DateTime now) => LockedUntil.HasValue && now < LockedUntil.Value;

        /// <summary>
        /// Once the lock is over, the failure counter restarts from zero
        /// </summary>
        public void ResetIfLockExpired(DateTime now)
        {
            if (LockedUntil.HasValue && !IsLocked(now))
            {
                NbrTries = 0;
            }
        }

        public void RegisterFailure(DateTime now)
        {
            NbrTries += 1;
            DateLogin = now;
        }

        public void RegisterSuccess(DateTime now)
        {
            NbrTries = 0;
            DateLogin = now;
        }
    }
}
