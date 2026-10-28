
namespace CollectionApp.Domain.Exceptions {
    public class AlreadyExistException : AbstractException
    {
        public AlreadyExistException(string message) : base(message)
        {
        }
    }
}