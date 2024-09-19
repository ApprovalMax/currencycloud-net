namespace CurrencyCloud.Authorization
{
    public class AuthorizationOptions
    {
        public AuthorizationOptions(Credentials credentials, TimeSpan tokenInactivityTimeout)
        {
            Credentials = credentials;
            TokenInactivityTimeout = tokenInactivityTimeout;
        }

        public Credentials Credentials { get; }
        public TimeSpan TokenInactivityTimeout { get; }
    }
}
