
namespace CollectionApp.Domain.Exceptions
{
    public class UnauthorizedException: AbstractException
    {
        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}