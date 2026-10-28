
namespace CollectionApp.Domain.Exceptions {
    public class BadInputException : AbstractException
    {
        public BadInputException(string message) : base(message)
        {
        }
    }
}