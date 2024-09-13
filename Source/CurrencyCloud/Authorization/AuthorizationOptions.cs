namespace CurrencyCloud.Authorization
{

    public class AuthorizationOptions
    {
        public AuthorizationOptions(Credentials credentials, int tokenInactivityTimeoutInMinutes)
        {
            Credentials = credentials;
            TokenInactivityTimeoutInMinutes = TimeSpan.FromMinutes(tokenInactivityTimeoutInMinutes);
        }

        public Credentials Credentials { get; }
        public TimeSpan TokenInactivityTimeoutInMinutes { get; }
    }
}
