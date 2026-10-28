using System.Net;

namespace CollectionApp.Domain.Exceptions
{
    public class NotFoundException: AbstractException
    {
        public NotFoundException(string message) : base(HttpStatusCode.NotFound, message)
        {
        }
    }
}


