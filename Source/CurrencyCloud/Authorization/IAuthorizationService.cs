namespace CurrencyCloud.Authorization;

public interface IAuthorizationService
{
    Task<string> GetTokenAsync(bool reauthorize);
}
