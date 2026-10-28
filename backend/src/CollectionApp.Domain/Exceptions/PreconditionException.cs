
namespace CollectionApp.Domain.Exceptions
{
    public class PreconditionException: AbstractException
    {
        public PreconditionException(string message) : base(message)
        {
        }
    }
}