namespace CollectionApp.Application.Health
{
    /// <summary>
    /// Health check result, serialized as { "api", "database", "checkedAt" }.
    /// </summary>
    /// <param name="Api">Always "ok" when the API answers</param>
    /// <param name="Database">"ok" or "ko"</param>
    /// <param name="CheckedAt">UTC instant of the check (DateTime of kind Utc, serialized with the "Z" suffix)</param>
    public sealed record HealthStatusDto(string Api, string Database, DateTime CheckedAt)
    {
        public const string Ok = "ok";
        public const string Ko = "ko";
    }
}
