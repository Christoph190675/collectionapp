using System.Net;

namespace CollectionApp.Domain.Exceptions
{
    public class TooLargeException: AbstractException
    {
        public TooLargeException(string message) : base(HttpStatusCode.RequestEntityTooLarge, message)
        {
        }
    }
}