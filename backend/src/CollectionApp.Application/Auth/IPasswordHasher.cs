namespace CollectionApp.Application.Auth
{
    public enum PasswordCheck
    {
        Failed,
        Success,
        SuccessRehashNeeded,
    }

    public interface IPasswordHasher
    {
        /// <summary>
        /// Hash of a password that matches no real password: verified when the e-mail is unknown,
        /// so that the response time does not reveal whether an account exists (research R7)
        /// </summary>
        string DummyHash { get; }

        string Hash(string password);

        PasswordCheck Verify(string hash, string password);
    }
}
