using System.Net;

namespace CollectionApp.Domain.Exceptions {
    public class BadRequestException : AbstractException
    {
        public BadRequestException(string message) : base(HttpStatusCode.BadRequest, message)
        {
        }
    }
}