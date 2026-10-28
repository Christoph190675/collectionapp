namespace CollectionApp.Api.Http.Middlewares 
{
    public static class SecureCookieMiddleware
    {

        public static IApplicationBuilder UseSecureCookieMiddleware(this IApplicationBuilder builder)
        {
            builder.UseCookiePolicy(
                new CookiePolicyOptions
                {
                    Secure = CookieSecurePolicy.Always
                });
            return builder;
        }
    }
}
