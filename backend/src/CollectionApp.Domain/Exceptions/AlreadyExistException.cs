using System.Net;

namespace CollectionApp.Domain.Exceptions {
    public class AlreadyExistException : AbstractException
    {
        public AlreadyExistException(string message) : base(HttpStatusCode.Conflict, message)
        {
        }
    }
}