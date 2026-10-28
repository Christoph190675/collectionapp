
namespace CollectionApp.Domain.Exceptions
{
    public class ForbiddenException: AbstractException
    {
        public ForbiddenException(string message) : base(message)
        {
        }
    }
}