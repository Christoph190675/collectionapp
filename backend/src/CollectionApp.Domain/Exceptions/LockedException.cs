using System.Net;

namespace CollectionApp.Domain.Exceptions
{
    public class LockedException: AbstractException
    {
        public LockedException(string message) : base(HttpStatusCode.Locked, message)
        {
        }
    }
}