using System.Net;

namespace CollectionApp.Domain.Exceptions
{
    public class InternalException: AbstractException
    {
        public InternalException(string message) : base(HttpStatusCode.InternalServerError, message)
        {
        }
    }
}