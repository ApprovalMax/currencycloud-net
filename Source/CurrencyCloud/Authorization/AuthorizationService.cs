using Newtonsoft.Json.Linq;
using CurrencyCloud.Environment;

namespace CurrencyCloud.Authorization
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly HttpClient httpClient;
        private string token { get; set; }
        private DateTime lastTokenRefresh { get; set; }
        private SemaphoreSlim semaphore { get; } = new(1, 1);

        private readonly AuthorizationOptions authorizationOptions;

        public AuthorizationService(IHttpClientFactory httpClientFactory, AuthorizationOptions authorizationOptions)
        {
            this.httpClient = httpClientFactory.CreateClient(nameof(AuthorizationService));
            httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.UserAgent);
            this.authorizationOptions =
                authorizationOptions ?? throw new ArgumentNullException(nameof(authorizationOptions));
        }

        private bool IsTokenInvalid => this.token == null ||
                                       authorizationOptions.TokenInactivityTimeout <=
                                       DateTime.UtcNow - this.lastTokenRefresh;

        public async Task<string> GetTokenAsync(bool reauthorize)
        {
            if (!(reauthorize || IsTokenInvalid)) return this.token;

            await this.semaphore.WaitAsync();

            try
            {
                if (reauthorize || IsTokenInvalid)
                {
                    this.token = await AuthorizeAsync();
                    this.lastTokenRefresh = DateTime.UtcNow;
                }
            }
            finally
            {
                this.semaphore.Release();
            }

            return this.token;
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
}
