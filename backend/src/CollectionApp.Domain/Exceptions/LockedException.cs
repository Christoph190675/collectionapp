
namespace CollectionApp.Domain.Exceptions
{
    public class LockedException: AbstractException
    {
        public LockedException(string message) : base(message)
        {
        }
    }
}