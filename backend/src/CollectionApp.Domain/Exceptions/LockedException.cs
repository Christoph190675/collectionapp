namespace CollectionApp.Domain.Exceptions
{
    public class LockedException: AbstractException
    {
        /// <summary>
        /// Remaining time before the resource is unlocked, if known (sent as the Retry-After header)
        /// </summary>
        public TimeSpan? RetryAfter { get; }

        public LockedException(string message, TimeSpan? retryAfter = null) : base(message)
        {
            RetryAfter = retryAfter;
        }
    }
}
