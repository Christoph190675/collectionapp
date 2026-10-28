using System.Net;

namespace CollectionApp.Domain.Exceptions
{
    public class UnsupportedMediaTypeException: AbstractException
    {
        public UnsupportedMediaTypeException(string message) : base(HttpStatusCode.UnsupportedMediaType, message)
        {
        }
    }
}