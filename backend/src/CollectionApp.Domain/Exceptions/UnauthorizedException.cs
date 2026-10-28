using System.Net;

namespace CollectionApp.Domain.Exceptions
{
    public class UnauthorizedException: AbstractException
    {
        public UnauthorizedException(string message) : base(HttpStatusCode.Unauthorized, message)
        {
        }
    }
}