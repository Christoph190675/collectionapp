namespace CollectionApp.Domain.Exceptions {

    /// <summary>
    /// Base class of the domain exceptions.
    /// The HTTP status code is chosen by the API (see ExceptionStatusMapper), not by the domain.
    /// </summary>
    public abstract class AbstractException : Exception
    {
        public AbstractException(string message) : base(message)
        {
        }
    }
}
