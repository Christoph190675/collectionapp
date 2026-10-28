using System.Net;

namespace CollectionApp.Domain.Exceptions {

    
    public abstract class AbstractException : Exception
    {
        public HttpStatusCode Code {get; private set;}

        public AbstractException(HttpStatusCode code, string message) : base(message)
        {
            Code = code;
        }
    }
}