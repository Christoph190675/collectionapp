using System.Net;

namespace CollectionApp.Domain.Exceptions
{
    public class NotAcceptableException: AbstractException
    {
        public NotAcceptableException(string message) : base(HttpStatusCode.NotAcceptable, message)
        {
        }
    }
}