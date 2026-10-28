using System.Net;

namespace CollectionApp.Domain.Exceptions {
    public class BadInputException : AbstractException
    {
        public BadInputException(string message) : base(HttpStatusCode.BadRequest, message)
        {
        }
    }
}