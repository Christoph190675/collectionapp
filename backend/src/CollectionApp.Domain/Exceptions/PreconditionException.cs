using System.Net;

namespace CollectionApp.Domain.Exceptions
{
    public class PreconditionException: AbstractException
    {
        public PreconditionException(string message) : base(HttpStatusCode.PreconditionFailed, message)
        {
        }
    }
}