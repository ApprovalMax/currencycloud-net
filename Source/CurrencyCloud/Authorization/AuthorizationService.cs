using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http;
using System.Net;

namespace CurrencyCloud.Authorization;
public class AuthorizationService : IAuthorizationService

{
    private readonly HttpClient httpClient;
    private readonly TokenState tokenState;
    private readonly AuthorizationOptions authorizationOptions;

    public AuthorizationService(HttpClient httpClient, AuthorizationOptions authorizationOptions, TokenState tokenState)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.tokenState = tokenState ?? throw new ArgumentNullException(nameof(tokenState));
        this.authorizationOptions = authorizationOptions ?? throw new ArgumentNullException(nameof(authorizationOptions));
    }

    private bool IsTokenInvalid => tokenState.Token == null || authorizationOptions.TokenInactivityTimeoutInMinutes <= DateTime.UtcNow - tokenState.LastTokenRefresh;

    public async Task<string> GetTokenAsync(bool reauthorize)
    {
        if (!(reauthorize || IsTokenInvalid)) return tokenState.Token;

        await tokenState.Semaphore.WaitAsync();

        try
        {
            if (reauthorize || IsTokenInvalid)
            {
                tokenState.Token = await AuthorizeAsync();
                tokenState.LastTokenRefresh = DateTime.UtcNow;
            }
        }
        finally
        {
            tokenState.Semaphore.Release();
        }

        return tokenState.Token;
    }

    private async Task<string> AuthorizeAsync()
    {
        const string requestUri = "/v2/authenticate/api";
        var authParams = new ParamsObject();
        authParams.Add("login_id", authorizationOptions.Credentials.LoginId);
        authParams.Add("api_key", authorizationOptions.Credentials.ApiKey);

        using var httpAuthRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
        httpAuthRequestMessage.Content = authParams.buildFormUrlBodyFromParams();

        var response = await httpClient.SendAsync(httpAuthRequestMessage);

        if (!response.IsSuccessStatusCode)
        {
            throw await ApiExceptionFactory.FromHttpResponse(response);
        }

        var responseString = await response.Content.ReadAsStringAsync();
        var responseObject = JObject.Parse(responseString);
        return responseObject["auth_token"]?.Value<string>()
                    ?? throw await ApiExceptionFactory.FromHttpResponse(response);
    }
}
