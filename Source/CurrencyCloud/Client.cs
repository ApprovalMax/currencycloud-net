using CurrencyCloud.Authorization;
using CurrencyCloud.Entity;
using CurrencyCloud.Entity.List;
using CurrencyCloud.Entity.Onboarding;
using CurrencyCloud.Entity.Pagination;
using CurrencyCloud.Environment;
using CurrencyCloud.Exception;
using CurrencyCloud.Extension;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CurrencyCloud.Converters;

[assembly: InternalsVisibleTo("Currencycloud.Tests")]

namespace CurrencyCloud
{
    /// <summary>
    /// Represents API client
    /// </summary>
    public class Client : ICurrencyCloudClient
    {
        private HttpClient httpClient;
        private readonly IAuthorizationService authorizationService;
        private string onBehalfOf;

        private static readonly JsonSerializerSettings JsonSerializerOptions = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new PascalContractResolver(),
            Converters = { new UtcDateTimeOffsetConverter(), new DateOnlyConverter() }
        };

        public Client(IHttpClientFactory httpClientFactory, IAuthorizationService authorizationService)
        {
            this.httpClient = httpClientFactory.CreateClient(nameof(Client));
            httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.UserAgent);
            this.authorizationService =
                authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }

        internal string Token
        {
            get { return httpClient.DefaultRequestHeaders.GetValues("X-Auth-Token").FirstOrDefault(); }
            set
            {
                httpClient.DefaultRequestHeaders.Remove("X-Auth-Token");
                httpClient.DefaultRequestHeaders.Add("X-Auth-Token", value);
            }
        }

        #region Retry

        public static TimeSpan backoffWait(int attempt, int min, int max, int jitter)
        {
            var wait = TimeSpan.FromMilliseconds(min * Math.Pow(2, attempt))
                       + TimeSpan.FromMilliseconds(new Random().Next(jitter));

            Debug.WriteLine("UTC: {0} - Backoff & Retry - Waiting {1}ms before retrying. {2} retries left",
                DateTime.UtcNow, wait.TotalMilliseconds, Retry.NumRetries - attempt);

            if (wait.TotalMilliseconds <= min)
                return TimeSpan.FromMilliseconds(min);

            return wait.TotalMilliseconds > max ? TimeSpan.FromMilliseconds(max) : wait;
        }

        public static HttpRequestMessage CloneHttpRequestMessage(HttpRequestMessage req)
        {
            HttpRequestMessage clone = new HttpRequestMessage(req.Method, req.RequestUri);

            var ms = new MemoryStream();
            if (req.Content != null)
            {
                req.Content.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                if (req.Content.Headers != null)
                    foreach (var h in req.Content.Headers)
                        clone.Content.Headers.Add(h.Key, h.Value);
            }

            clone.Version = req.Version;

            foreach (KeyValuePair<string, object> prop in req.Properties)
                clone.Properties.Add(prop);

            foreach (KeyValuePair<string, IEnumerable<string>> header in req.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }

        public static AsyncPolicy<HttpResponseMessage> retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(res => Retry.OnError.Contains(res.StatusCode))
            .WaitAndRetryAsync(
                Retry.NumRetries,
                attempt => backoffWait(attempt, Retry.MinWait, Retry.MaxWait, Retry.Jitter),
                (err, delay) => Debug.WriteLine("UTC: {0} - Retry Policy - Delaying for {1}ms.",
                    DateTime.UtcNow, delay.TotalMilliseconds));

        #endregion

        #region Request

        private async Task<TResult> OnboardingRequestAsync<TResult>(string path, HttpMethod method)
        {
            // HACK: CurrencyCloud api requires not null content for POST requests
            var message = method == HttpMethod.Get || method == HttpMethod.Delete
                ? new HttpRequestMessage(method, path)
                : new HttpRequestMessage(method, path)
                {
                    Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json")
                };

            return await RequestAsync<TResult>(message);
        }

        private async Task<TResult> OnboardingRequestAsync<TResult, TRequest>(string path, HttpMethod method,
            TRequest request)
        {
            var message = method == HttpMethod.Get
                ? new HttpRequestMessage(method, path)
                : new HttpRequestMessage(method, path)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(request, JsonSerializerOptions),
                        System.Text.Encoding.UTF8, "application/json")
                };

            return await RequestAsync<TResult>(message);
        }

        private async Task<TResult> RequestAsync<TResult>(string path, HttpMethod method, ParamsObject obj = null)
        {
            var paramsObj = new ParamsObject();
            if (obj != null)
            {
                paramsObj += obj;
            }

            paramsObj.AddNotNull("OnBehalfOf", onBehalfOf);

            var requestUri = path;
            if (paramsObj.Count > 0)
            {
                requestUri += "?" + paramsObj.ToQueryString();
            }

            var httpRequestMessage = method == HttpMethod.Get
                ? new HttpRequestMessage(method, requestUri)
                : new HttpRequestMessage(method, path)
                {
                    Content = paramsObj.buildFormUrlBodyFromParams()
                };

            return await RequestAsync<TResult>(httpRequestMessage);
        }

        private async Task<string> AuthorizeAsync(bool reauthorize = false)
        {
            var token = await authorizationService.GetTokenAsync(reauthorize);
            httpClient.DefaultRequestHeaders.Remove("X-Auth-Token");
            httpClient.DefaultRequestHeaders.Add("X-Auth-Token", token);

            return token;
        }

        private async Task<TResult> RequestAsync<TResult>(HttpRequestMessage requestMessage)
        {
            if (httpClient == null)
            {
                throw new InvalidOperationException("Client is not initialized.");
            }

            if (!IsInitialized)
            {
                await AuthorizeAsync();
            }

            Func<Task<TResult>> requestAsyncDelegate = async () =>
            {
                var httpRequestMessage = CloneHttpRequestMessage(requestMessage);

                Debug.WriteLine(
                    "UTC: {0} - HTTP {1} Request - {2} Content - {3}",
                    DateTime.UtcNow,
                    httpRequestMessage.Method,
                    httpRequestMessage.RequestUri,
                    httpRequestMessage.Content?.ReadAsStringAsync().Result);

                if (Retry.Enabled)
                    Debug.WriteLine(
                        "UTC: {0} - Retrying request - Retries: {1}, MinWait: {2}, MaxWait: {3}, Jitter: {4}]",
                        DateTime.UtcNow, Retry.NumRetries, Retry.MinWait, Retry.MaxWait, Retry.Jitter);

                HttpResponseMessage res = Retry.Enabled
                    ? await retryPolicy.ExecuteAsync(
                        ct => httpClient.SendAsync(CloneHttpRequestMessage(httpRequestMessage)), CancellationToken.None)
                    : await httpClient.SendAsync(httpRequestMessage);

                if (res.IsSuccessStatusCode)
                {
                    string resString = await res.Content.ReadAsStringAsync();
                    Debug.WriteLine("UTC: {0} - HTTP Response: {1}", DateTime.UtcNow, resString);

                    return JsonConvert.DeserializeObject<TResult>(resString, JsonSerializerOptions);
                }

                throw await ApiExceptionFactory.FromHttpResponse(res);
            };

            for (int attempts = 0; ; attempts++)
            {
                try
                {
                    if (attempts == 1)
                    {
                        await AuthorizeAsync();
                    }
                    else if (attempts > 1)
                    {
                        await AuthorizeAsync(reauthorize: true);
                    }


                    return await requestAsyncDelegate();
                }
                catch (AuthenticationException)
                {
                    if (attempts == 3)
                    {
                        throw;
                    }
                }
            }
        }

        #endregion

        #region OnBehalfOf

        /// <summary>
        /// Executes operations on behalf of another contact.
        /// </summary>
        /// <param name="id">Id of the contact.</param>
        /// <param name="function">Asynchronous function, which is executed on behalf of the given contact.</param>
        /// <returns>Asynchronous task.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the previous call of the method has not yet completed.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is not a valid UUID.</exception>
        public async Task OnBehalfOf(string id, Func<Task> function)
        {
            if (onBehalfOf != null)
            {
                throw new InvalidOperationException("OnBehalfOf has already been called and not yet completed.");
            }

            string UuidPattern = @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";
            if (!Regex.IsMatch(id, UuidPattern))
            {
                throw new ArgumentException("Id is not a valid UUID", "id");
            }

            onBehalfOf = id;

            try
            {
                await function();
            }
            finally
            {
                onBehalfOf = null;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Gets a value that indicates whether the client is initialized.
        /// </summary>
        public bool IsInitialized => httpClient.DefaultRequestHeaders.Contains("X-Auth-Token");

        /// <summary>
        /// Initializes the client and generates authentication token for the API user.
        /// </summary>
        /// <param name="apiServer">API server to make requests against.</param>
        /// <returns>Asynchronous task, which returns the authentication token.</returns>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        [Obsolete("This method is deprecated. Please use Authorize Method.")]
        public async Task<string> InitializeAsync(ApiServer apiServer)
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.UserAgent);

            httpClient.BaseAddress = new Uri(apiServer.Url);

            return await AuthorizeAsync();
        }

        /// <summary>
        /// Closes current session and resets the client.
        /// </summary>
        /// <returns>Asynchronous task.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        [Obsolete("This method is deprecated. Please use Authorize Method.")]
        public async Task CloseAsync()
        {
            if (httpClient == null)
            {
                throw new InvalidOperationException("Client is not initialized.");
            }

            HttpResponseMessage res =
                await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/v2/authenticate/close_session"));
            if (res.IsSuccessStatusCode)
            {
                httpClient.Dispose();
                httpClient = null;
            }
            else
            {
                throw await ApiExceptionFactory.FromHttpResponse(res);
            }
        }

        #endregion

        #region Accounts

        /// <summary>
        /// Creates a new account.
        /// </summary>
        /// <param name="account">Account object</param>
        /// <returns>Asynchronous task, which returns newly created account.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Account> CreateAccountAsync(Account account)
        {
            var paramsObj = ParamsObject.CreateFromStaticObject(account);

            return await RequestAsync<Account>("/v2/accounts/create", HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Gets details of an account.
        /// </summary>
        /// <param name="id">Id of the requested account.</param>
        /// <returns>Asynchronous task, which returns the requested account.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Account> GetAccountAsync(string id)
        {
            return await RequestAsync<Account>("/v2/accounts/" + id, HttpMethod.Get, null);
        }

        /// <summary>
        /// Updates an existing account.
        /// </summary>
        /// <param name="account">Account object to be updated</param>
        /// <returns>Asynchronous task, which returns the updated account.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Account> UpdateAccountAsync(Account account)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(account);
            string id = account.Id;

            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Account Id cannot be null");

            return await RequestAsync<Account>("/v2/accounts/" + id, HttpMethod.Post, optional);
        }

        /// <summary>
        /// Finds accounts matching the search criteria for the active user.
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns the list of the found accounts, as well as pagination information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedAccounts> FindAccountsAsync(AccountFindParameters parameters = null)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedAccounts>("/v2/accounts/find", HttpMethod.Post, optional);
        }

        /// <summary>
        /// Gets details of the active account.
        /// </summary>
        /// <returns>Asynchronous task, which returns the active account.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Account> GetCurrentAccountAsync()
        {
            return await RequestAsync<Account>("/v2/accounts/current", HttpMethod.Get);
        }

        /// <summary>
        /// Gets payment charges settings for given account.
        /// </summary>
        /// <returns>Asynchronous task, which returns the payment charges settings.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaymentChargesSettingsList> GetPaymentChargesSettingsAsync(string id)
        {
            return await RequestAsync<PaymentChargesSettingsList>("/v2/accounts/" + id + "/payment_charges_settings",
                HttpMethod.Get);
        }

        /// <summary>
        /// Manage given Account's Payment Charge Settings (enable, disable, set default).
        /// </summary>
        /// <param name="paymentChargesSettings">Account's Payment Charge Settings object to be updated</param>
        /// <returns>Asynchronous task, which returns the updated account.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaymentChargesSettings> ManageAccountPaymentChargesSettingsAsync(
            PaymentChargesSettings paymentChargesSettings)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(paymentChargesSettings);
            string accountId = paymentChargesSettings.AccountId;
            string chargeSettingsId = paymentChargesSettings.ChargeSettingsId;

            if (string.IsNullOrEmpty(accountId))
                throw new ArgumentException("Account Id cannot be null");

            if (string.IsNullOrEmpty(chargeSettingsId))
                throw new ArgumentException("Charge Settings Id cannot be null");

            return await RequestAsync<PaymentChargesSettings>(
                "/v2/accounts/" + accountId + "/payment_charges_settings/" + chargeSettingsId, HttpMethod.Post,
                optional);
        }

        #endregion

        #region Balances

        /// <summary>
        /// Gets the balance for a currency.
        /// </summary>
        /// <param name="currency">Currency to get the balance for.</param>
        /// <returns>Asynchronous task, which returns the requested balance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Balance> GetBalanceAsync(string currency)
        {
            return await RequestAsync<Balance>("/v2/balances/" + currency, HttpMethod.Get, null);
        }

        /// <summary>
        /// Finds balances matching the search criteria.
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns the list of the found balances, as well as pagination information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedBalances> FindBalancesAsync(BalanceFindParameters parameters = null)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedBalances>("/v2/balances/find", HttpMethod.Get, optional);
        }

        /// <summary>
        /// Top up the margin balance for a currency.
        /// </summary>
        /// <param name="currency">Currency to top up the balance with.</param>
        /// <param name="amount">Amount to top up the balance with.</param>
        /// <returns>Asynchronous task, which tops up the given margin balance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<MarginBalanceTopUp> TopUpMarginBalanceAsync(string currency, decimal amount)
        {
            if (string.IsNullOrEmpty(currency))
                throw new ArgumentException("Currency can not be null");
            var paramsObj = new ParamsObject();
            paramsObj.AddNotNull("Currency", currency);
            paramsObj.AddNotNull("Amount", amount);
            return await RequestAsync<MarginBalanceTopUp>("/v2/balances/top_up_margin", HttpMethod.Post, paramsObj);
        }

        #endregion

        #region Beneficiaries

        /// <summary>
        /// Validates beneficiary details without creating one.
        /// </summary>
        /// <param name="beneficiary">Beneficiary data to be validated</param>
        /// <returns>Asynchronous task, which returns the validated beneficiary.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Beneficiary> ValidateBeneficiaryAsync(Beneficiary beneficiary)
        {
            var paramsObj = ParamsObject.CreateFromStaticObject(beneficiary);

            return await RequestAsync<Beneficiary>("/v2/beneficiaries/validate", HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Verifies beneficiary details without creating one.
        /// </summary>
        /// <param name="request">Beneficiary verification request data</param>
        /// <returns>Asynchronous task, which returns the verified beneficiary.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<BeneficiaryVerification> VerifyBeneficiaryAsync(BeneficiaryVerificationParameters request)
        {
            var paramsObj = ParamsObject.CreateFromStaticObject(request);

            return await RequestAsync<BeneficiaryVerification>("/v2/beneficiaries/account_verification",
                HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Creates a new beneficiary.
        /// </summary>
        /// <param name="beneficiary">Beneficiary object to be created</param>
        /// <returns>Asynchronous task, which returns newly created beneficiary.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Beneficiary> CreateBeneficiaryAsync(Beneficiary beneficiary)
        {
            var paramsObj = ParamsObject.CreateFromStaticObject(beneficiary);

            return await RequestAsync<Beneficiary>("/v2/beneficiaries/create", HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Gets details of a beneficiary.
        /// </summary>
        /// <param name="id">Id of the requested beneficiary.</param>
        /// <returns>Asynchronous task, which returns the requested beneficiary.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Beneficiary> GetBeneficiaryAsync(string id)
        {
            return await RequestAsync<Beneficiary>("/v2/beneficiaries/" + id, HttpMethod.Get, null);
        }

        /// <summary>
        /// Updates an existing beneficiary.
        /// </summary>
        /// <param name="beneficiary">Beneficiary object to be updated</param>
        /// <returns>Asynchronous task, which returns the updated beneficiary.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        /// <exception cref="ArgumentException">Thrown if Beneficiary.Id is NULL</exception>
        public async Task<Beneficiary> UpdateBeneficiaryAsync(Beneficiary beneficiary)
        {
            string id = beneficiary.Id;
            if (id == null)
                throw new ArgumentException("Beneficiary Id cannot be null");

            var optional = ParamsObject.CreateFromStaticObject(beneficiary);

            return await RequestAsync<Beneficiary>("/v2/beneficiaries/" + id, HttpMethod.Post, optional);
        }

        /// <summary>
        /// Finds beneficiaries matching the search criteria for the active user.
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns the list of the found beneficiaries, as well as pagination information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedBeneficiaries> FindBeneficiariesAsync(BeneficiaryFindParameters parameters = null)
        {
            var optional = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedBeneficiaries>("/v2/beneficiaries/find", HttpMethod.Post, optional);
        }

        /// <summary>
        /// Deletes an existing beneficiary.
        /// </summary>
        /// <param name="id">Id of the deleted beneficiary.</param>
        /// <returns>Asynchronous task, which returns the deleted beneficiary.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Beneficiary> DeleteBeneficiaryAsync(string id)
        {
            var optional = new ParamsObject();

            return await RequestAsync<Beneficiary>("/v2/beneficiaries/" + id + "/delete", HttpMethod.Post, optional);
        }

        #endregion

        #region Contacts

        /// <summary>
        /// Creates a new contact.
        /// </summary>
        /// <param name="contact">Contact object to be created</param>
        /// <returns>Asynchronous task, which returns newly created contact.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Contact> CreateContactAsync(Contact contact)
        {
            var paramsObj = ParamsObject.CreateFromStaticObject(contact);

            return await RequestAsync<Contact>("/v2/contacts/create", HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Gets details of a contact.
        /// </summary>
        /// <param name="id">Id of the requested contact.</param>
        /// <returns>Asynchronous task, which returns the requested contact.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Contact> GetContactAsync(string id)
        {
            return await RequestAsync<Contact>("/v2/contacts/" + id, HttpMethod.Get, null);
        }

        /// <summary>
        /// Updates an existing contact.
        /// </summary>
        /// <param name="contact">Contact object to be updated</param>
        /// <returns>Asynchronous task, which returns the updated contact.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        /// <exception cref="ArgumentException">Thrown if Contact.Id is NULL</exception>
        public async Task<Contact> UpdateContactAsync(Contact contact)
        {
            string id = contact.Id;
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Contact Id can not be null");

            ParamsObject optional = ParamsObject.CreateFromStaticObject(contact);
            //remove account_id. Not required by server while update.
            optional.Remove("AccountId");

            return await RequestAsync<Contact>("/v2/contacts/" + id, HttpMethod.Post, optional);
        }

        /// <summary>
        /// Finds contacts matching the search criteria for the active user.
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns the list of the found contacts, as well as pagination information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedContacts> FindContactsAsync(ContactFindParameters parameters = null)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedContacts>("/v2/contacts/find", HttpMethod.Post, optional);
        }

        /// <summary>
        /// Gets details of the active contact.
        /// </summary>
        /// <returns>Asynchronous task, which returns the active contact.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Contact> GetCurrentContactAsync()
        {
            return await RequestAsync<Contact>("/v2/contacts/current", HttpMethod.Get);
        }

        #endregion

        #region Conversions

        /// <summary>
        /// Creates a new conversion.
        /// </summary>
        /// <param name="conversion">Data object for new conversion</param>
        /// <returns>Asynchronous task, which returns newly created conversion.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Conversion> CreateConversionAsync(Conversion conversion)
        {
            var paramsObj = ParamsObject.CreateFromStaticObject(conversion);

            return await RequestAsync<Conversion>("/v2/conversions/create", HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Gets details of a conversion.
        /// </summary>
        /// <param name="id">Id of the requested conversion.</param>
        /// <returns>Asynchronous task, which returns the requested conversion.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Conversion> GetConversionAsync(string id)
        {
            return await RequestAsync<Conversion>("/v2/conversions/" + id, HttpMethod.Get, null);
        }

        /// <summary>
        /// Finds conversions matching the search criteria for the active user.
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns the list of the found conversions, as well as pagination information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedConversions> FindConversionsAsync(ConversionFindParameters parameters = null)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedConversions>("/v2/conversions/find", HttpMethod.Get, optional);
        }

        /// <summary>
        /// Quotes cost of cancelling conversion identified by the provided unique id.
        /// </summary>
        /// <param name="conversionCancellationQuote">Object holding the Id of the conversion that is being quoted</param>
        /// <returns>Asynchronous task, which returns the details of the cancelled conversion</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<ConversionCancellation> QuoteCancelConversionAsync(
            ConversionCancellation conversionCancellationQuote)
        {
            string id = conversionCancellationQuote.ConversionId;

            return await RequestAsync<ConversionCancellation>("/v2/conversions/" + id + "/cancellation_quote",
                HttpMethod.Get, null);
        }

        /// <summary>
        /// Cancels the conversion identified by the provided unique id.
        /// </summary>
        /// <param name="conversionCancellation">Object holding the Id and Notes (optional) of the conversion that is being cancelled</param>
        /// <returns>Asynchronous task, which returns the details of the cancelled conversion</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<ConversionCancellation> CancelConversionsAsync(ConversionCancellation conversionCancellation)
        {
            ParamsObject paramsObj = ParamsObject.CreateFromStaticObject(conversionCancellation);
            string id = conversionCancellation.ConversionId;

            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Conversion Id cannot be null");

            return await RequestAsync<ConversionCancellation>("/v2/conversions/" + id + "/cancel", HttpMethod.Post,
                paramsObj);
        }

        /// <summary>
        /// Returns an object containing the quote for changing the date of the specified conversion.
        /// </summary>
        /// <param name="conversionDateChange">Object holding the Id and New Settlement Date of the conversion that is being changed</param>
        /// <returns>Asynchronous task, which returns the details of the conversion date change</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<ConversionDateChange> QuoteDateChangeConversionAsync(
            string conversionId,
            DateOnly newSettlementDate)
        {
            if (string.IsNullOrEmpty(conversionId))
                throw new ArgumentException("Conversion Id cannot be null");
            
            var paramsObj = new ParamsObject();
            paramsObj.Add("NewSettlementDate", newSettlementDate);

            return await RequestAsync<ConversionDateChange>("/v2/conversions/" + conversionId + "/date_change_quote",
                HttpMethod.Get, paramsObj);
        }

        /// <summary>
        /// Changes the date ofthe conversion identified by the provided unique id.
        /// </summary>
        /// <param name="conversionDateChange">Object holding the Id and New Settlement Date of the conversion that is being changed</param>
        /// <returns>Asynchronous task, which returns the details of the conversion date change</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<ConversionDateChange> DateChangeConversionAsync(ConversionDateChange conversionDateChange)
        {
            ParamsObject paramsObj = ParamsObject.CreateFromStaticObject(conversionDateChange);
            string id = conversionDateChange.ConversionId;
            DateTimeOffset? newSettlementDate = conversionDateChange.NewSettlementDate;

            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Conversion Id cannot be null");
            if (!newSettlementDate.HasValue)
                throw new ArgumentException("New Settlement Date cannot be null");

            return await RequestAsync<ConversionDateChange>("/v2/conversions/" + id + "/date_change", HttpMethod.Post,
                paramsObj);
        }

        /// <summary>
        /// Previews a conversion split.
        /// </summary>
        /// <param name="conversionSplit">Object holding the Id and Amount of the conversion to preview a split</param>
        /// <returns>Asynchronous task, which returns the details of the split conversion</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<ConversionSplit> PreviewSplitConversionAsync(Conversion conversionSplit)
        {
            string id = conversionSplit.Id;
            decimal? amount = conversionSplit.Amount;

            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Conversion Id cannot be null");
            if (!amount.HasValue)
                throw new ArgumentException("Amount cannot be null");

            ParamsObject paramsObj = ParamsObject.CreateFromStaticObject(conversionSplit);

            return await RequestAsync<ConversionSplit>("/v2/conversions/" + id + "/split_preview", HttpMethod.Get,
                paramsObj);
        }

        /// <summary>
        /// Splits a conversion.
        /// </summary>
        /// <param name="conversionSplit">Object holding the Id and Amount of the conversion to split</param>
        /// <returns>Asynchronous task, which returns the details of the split conversion</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<ConversionSplit> SplitConversionAsync(Conversion conversionSplit)
        {
            string id = conversionSplit.Id;
            decimal? amount = conversionSplit.Amount;

            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Conversion Id cannot be null");
            if (!amount.HasValue)
                throw new ArgumentException("Amount cannot be null");

            ParamsObject paramsObj = ParamsObject.CreateFromStaticObject(conversionSplit);

            return await RequestAsync<ConversionSplit>("/v2/conversions/" + id + "/split", HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Conversion split history.
        /// </summary>
        /// <param name="conversionSplit">Object holding the Id of the conversion to query</param>
        /// <returns>Asynchronous task, which returns the split history for a given conversion</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<ConversionSplitHistory> SplitHistoryConversionAsync(Conversion conversionSplit)
        {
            string id = conversionSplit.Id;

            return await RequestAsync<ConversionSplitHistory>("/v2/conversions/" + id + "/split_history",
                HttpMethod.Get, null);
        }

        /// <summary>
        /// Returns an object that contains information related to actions on conversions that have generated profit or loss
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns the list of the found conversions, as well as pagination information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedConversionProfitAndLosses> FindConversionProfitAndLossesAsync(
            ConversionProfitAndLossFindParameters parameters = null)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedConversionProfitAndLosses>("/v2/conversions/profit_and_loss",
                HttpMethod.Get, optional);
        }

        #endregion

        #region Funding

        /// <summary>
        /// Returns an object that contains information related to Funding Accounts
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns the list of the funding accounts, as well as pagination information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedFundingAccounts> FindFundingAccountsAsync(
            FundingAccountFindParameters parameters = null)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedFundingAccounts>("/v2/funding_accounts/find", HttpMethod.Get, optional);
        }

        /// <summary>
        /// Return an object that contains information related to Funding
        /// </summary>
        /// <param name="id">Funding Id</param>
        /// <returns>Asynchronous task, which returns funding entity</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Funding> FindFundingAsync(Guid id)
        {
            return await RequestAsync<Funding>("/v2/funding_transactions/" + id, HttpMethod.Get, null);
        }

        /// <summary>
        /// Triggers a production-like flow for processing funds, topping up CM balance or rejecting the
        /// transaction without topping up CM balance
        /// </summary>
        /// <param name="addFunds">Add funds parameters</param>
        /// <returns>Asynchronous task, which returns the add demo funds.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<AddedFunds> AddDemoFundsAsync(AddFundsParameters addFunds = null)
        {
            ParamsObject paramsObj = ParamsObject.CreateFromStaticObject(addFunds);

            return await RequestAsync<AddedFunds>("/v2/demo/funding/create", HttpMethod.Post, paramsObj);
        }

        #endregion

        #region Onboarding

        /// <summary>
        /// Gets a list of countries.
        /// </summary>
        /// <returns>Asynchronous task, which returns a list of countries.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<CountriesList> GetCountriesAsync() //CountriesList
        {
            var result = await RequestAsync<DataModel<CountriesList>>("/onboarding/v1/countries", HttpMethod.Get);
            return result.Data;
        }

        /// <summary>
        /// Gets document types for a specific country.
        /// </summary>
        /// <param name="countryCode">Country code</param>
        /// <returns>Asynchronous task, which returns a list of document types.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<DocumentsList> GetDocumentTypesAsync(string countryCode) //DocumentTypesList
        {
            var result =
                await RequestAsync<DataModel<DocumentsList>>($"/onboarding/v1/countries/{countryCode}/document_types",
                    HttpMethod.Get);
            return result.Data;
        }

        /// <summary>
        /// Gets a list of supported currencies.
        /// </summary>
        /// <returns>Asynchronous task, which returns a list of supported currencies.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Entity.Onboarding.CurrenciesList> GetSupportedCurrenciesAsync() //SupportedCurrenciesList
        {
            var result =
                await RequestAsync<DataModel<Entity.Onboarding.CurrenciesList>>("/onboarding/v1/currencies/supported",
                    HttpMethod.Get);
            return result.Data;
        }

        /// <summary>
        /// Gets details of a form.
        /// </summary>
        /// <param name="formId">Form ID</param>
        /// <returns>Asynchronous task, which returns the requested form.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<FormWithAssociations> GetFormAsync(Guid formId) //Form
        {
            var result =
                await OnboardingRequestAsync<DataModel<FormWithAssociations>>($"/onboarding/v1/forms/{formId}",
                    HttpMethod.Get);
            return result.Data;
        }

        /// <summary>
        /// Deletes a form.
        /// </summary>
        /// <param name="formId">Form ID</param>
        /// <returns>Asynchronous task, which returns the date of deletion and form id.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<FormDeletionResult> DeleteFormAsync(Guid formId) //Form
        {
            var result =
                await OnboardingRequestAsync<DataModel<FormDeletionResult>>($"/onboarding/v1/forms/{formId}",
                    HttpMethod.Delete);
            return result.Data;
        }

        /// <summary>
        /// Creates a new form.
        /// </summary>
        /// <param name="form">Form object</param>
        /// <returns>Asynchronous task, which returns the newly created form.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<FormWithIds> CreateFormAsync(Form form) //Form
        {
            var result =
                await OnboardingRequestAsync<DataModel<FormWithIds>, Form>("/onboarding/v1/forms", HttpMethod.Post,
                    form);
            return result.Data;
        }

        /// <summary>
        /// Submits a form.
        /// </summary>
        /// <param name="formId">Form ID</param>
        /// <returns>Asynchronous task, which returns the form submission result.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<FormWithIds> SubmitFormAsync(Guid formId) //FormSubmission
        {
            var result =
                await OnboardingRequestAsync<DataModel<FormWithIds>>($"/onboarding/v1/forms/{formId}/submit",
                    HttpMethod.Post);
            return result.Data;
        }

        /// <summary>
        /// Adds a person to a form.
        /// </summary>
        /// <param name="formId">Form ID</param>
        /// <param name="person">Person object</param>
        /// <returns>Asynchronous task, which returns the added person.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PersonWithIds> AddPersonToFormAsync(Guid formId, Person person) //Person
        {
            var result =
                await OnboardingRequestAsync<DataModel<PersonWithIds>, Person>($"/onboarding/v1/forms/{formId}/people",
                    HttpMethod.Post, person);
            return result.Data;
        }

        /// <summary>
        /// Adds a document to a form.
        /// </summary>
        /// <param name="formId">Form ID</param>
        /// <param name="document">Document object</param>
        /// <returns>Asynchronous task, which returns the added document.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<DocumentWithIds> AddDocumentToFormAsync(Guid formId, Document document) //Document
        {
            var result =
                await OnboardingRequestAsync<DataModel<DocumentWithIds>, Document>(
                    $"/onboarding/v1/forms/{formId}/documents", HttpMethod.Post, document);
            return result.Data;
        }

        /// <summary>
        /// Adds a document image to a document.
        /// </summary>
        /// <param name="formId">Form ID</param>
        /// <param name="documentId">Document ID</param>
        /// <param name="documentImage">Document image object</param>
        /// <returns>Asynchronous task, which returns the added document image.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Entity.Onboarding.DocumentImageWithIds> AddDocumentImageAsync(Guid formId, Guid documentId,
            DocumentImage documentImage) //DocumentImage
        {
            var result = await OnboardingRequestAsync<DataModel<Entity.Onboarding.DocumentImageWithIds>, DocumentImage>(
                $"/onboarding/v1/forms/{formId}/documents/{documentId}/document_images",
                HttpMethod.Post,
                documentImage);
            return result.Data;
        }

        /// <summary>
        /// Adds a account usage information to a form.
        /// </summary>
        /// <param name="formId">Form ID</param>
        /// <param name="accountUsage">Account usage object</param>
        /// <returns>Asynchronous task, which returns the added account usage.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<AccountUsageWithIds> AddAccountUsageAsync(Guid formId, AccountUsage accountUsage)
        {
            var result = await OnboardingRequestAsync<DataModel<AccountUsageWithIds>, AccountUsage>(
                $"/onboarding/v1/forms/{formId}/account_usage",
                HttpMethod.Put,
                accountUsage);
            return result.Data;
        }

        /// <summary>
        /// Adds business information to a form.
        /// </summary>
        /// <param name="formId">Form ID</param>
        /// <param name="businessInformation">Business information object</param>
        /// <returns>Asynchronous task, which returns the added business information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Entity.Onboarding.BusinessInformationWithIds> AddBusinessInformationAsync(
            Guid formId,
            BusinessInformation businessInformation)
        {
            var result =
                await OnboardingRequestAsync<DataModel<Entity.Onboarding.BusinessInformationWithIds>,
                    BusinessInformation>(
                    $"/onboarding/v1/forms/{formId}/business_information",
                    HttpMethod.Put,
                    businessInformation);
            return result.Data;
        }

        #endregion

        #region Ibans

        /// <summary>
        /// Find IBANs assigned to the logged in account.
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns structure containing the details of the IBAN assigned to the logged in account.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedIbans> FindIbansAsync(IbanFindParameters parameters = null)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedIbans>("/v2/ibans/find", HttpMethod.Get, optional);
        }

        #endregion

        #region Payers

        /// <summary>
        /// Gets details of a payer.
        /// </summary>
        /// <param name="id">Id of the requested payer.</param>
        /// <returns>Asynchronous task, which returns the requested payer.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Payer> GetPayerAsync(string id)
        {
            return await RequestAsync<Payer>("/v2/payers/" + id, HttpMethod.Get, null);
        }

        #endregion

        #region Payments

        /// <summary>
        /// Creates a new payment.
        /// </summary>
        /// <param name="payment">Payment object to be created</param>
        /// <param name="payer">Optional payer info</param>
        /// <returns>Asynchronous task, which returns newly created payment.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        /// <remarks>Payment.PayerDetailsSource not passed to server while creation</remarks>
        public async Task<Payment> CreatePaymentAsync(Payment payment, Payer payer = null)
        {
            ParamsObject paramsObj = ParamsObject.CreateFromStaticObject(payment);
            if (payer != null)
            {
                paramsObj.AddNotNull("PayerEntityType", payer.LegalEntityType);
                paramsObj.AddNotNull("PayerCompanyName", payer.CompanyName);
                paramsObj.AddNotNull("PayerFirstName", payer.FirstName);
                paramsObj.AddNotNull("PayerLastName", payer.LastName);
                paramsObj.AddNotNull("PayerCity", payer.City);
                paramsObj.AddNotNull("PayerAddress", payer.Address);
                paramsObj.AddNotNull("PayerPostcode", payer.Postcode);
                paramsObj.AddNotNull("PayerStateOrProvince", payer.StateOrProvince);
                paramsObj.AddNotNull("PayerCountry", payer.Country);
                paramsObj.AddNotNull("PayerDateOfBirth", payer.DateOfBirth);
                paramsObj.AddNotNull("PayerIdentificationType", payer.IdentificationType);
                paramsObj.AddNotNull("PayerIdentificationValue", payer.IdentificationValue);
            }

            return await RequestAsync<Payment>("/v2/payments/create", HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Gets details of a payment.
        /// </summary>
        /// <param name="id">Id of the requested payment.</param>
        /// <returns>Asynchronous task, which returns the requested payment.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Payment> GetPaymentAsync(string id)
        {
            return await RequestAsync<Payment>("/v2/payments/" + id, HttpMethod.Get, null);
        }

        /// <summary>
        /// Updates an existing payment.
        /// </summary>
        /// <param name="payment">Payment object to be updated</param>
        /// <param name="payer">Optional payer data to be updated for payment</param>
        /// <returns>Asynchronous task, which returns the updated payment.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        /// <exception cref="ArgumentException">Thrown when Payment.Id is NULL</exception>
        public async Task<Payment> UpdatePaymentAsync(Payment payment, Payer payer = null)
        {
            string id = payment.Id;
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Payment Id cannot be null");

            ParamsObject optional = ParamsObject.CreateFromStaticObject(payment);
            optional.AddNotNull("PayerDetailsSource", payment.PayerDetailsSource);
            if (payer != null)
            {
                optional.AddNotNull("PayerEntityType", payer.LegalEntityType);
                optional.AddNotNull("PayerCompanyName", payer.CompanyName);
                optional.AddNotNull("PayerFirstName", payer.FirstName);
                optional.AddNotNull("PayerLastName", payer.LastName);
                optional.AddNotNull("PayerCity", payer.City);
                optional.AddNotNull("PayerAddress", payer.Address);
                optional.AddNotNull("PayerPostcode", payer.Postcode);
                optional.AddNotNull("PayerStateOrProvince", payer.StateOrProvince);
                optional.AddNotNull("PayerCountry", payer.Country);
                optional.AddNotNull("PayerDateOfBirth", payer.DateOfBirth);
                optional.AddNotNull("PayerIdentificationType", payer.IdentificationType);
                optional.AddNotNull("PayerIdentificationValue", payer.IdentificationValue);
            }

            return await RequestAsync<Payment>("/v2/payments/" + id, HttpMethod.Post, optional);
        }

        /// <summary>
        /// Finds payments matching the search criteria for the active user.
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns  the list of the found payments, as well as pagination information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedPayments> FindPaymentsAsync(FindParameters parameters = null)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedPayments>("/v2/payments/find", HttpMethod.Get, optional);
        }

        /// <summary>
        /// Deletes an existing payment.
        /// </summary>
        /// <param name="id">Id of the deleted payment.</param>
        /// <returns>Asynchronous task, which returns the deleted payment.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Payment> DeletePaymentAsync(string id)
        {
            return await RequestAsync<Payment>("/v2/payments/" + id + "/delete", HttpMethod.Post, null);
        }

        /// <summary>
        /// Returns a hash containing the details of MT103 information for a SWIFT payments.
        /// </summary>
        /// <param name="id">Id payment.</param>
        /// <returns>Asynchronous task, which returns the MT103 information for a SWIFT payment.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaymentSubmission> GetPaymentSubmissionAsync(string id)
        {
            return await RequestAsync<PaymentSubmission>("/v2/payments/" + id + "/submission", HttpMethod.Get, null);
        }

        /// <summary>
        /// Returns an array of PaymentAuthorisation Objects
        /// </summary>
        /// <param name="paymentIds">Array of Payment Ids to authorise</param>
        /// <returns>Asynchronous task, which returns an array of PaymentAuthorisation Objects</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaymentAuthorisationsList> PaymentAuthorisationAsync(string[] paymentIds)
        {
            if (paymentIds.Length < 1)
                throw new ArgumentException("Payment IDs can not be null");

            var paramsObj = new ParamsObject();
            paramsObj.AddNotNull("PaymentIds", paymentIds);

            return await RequestAsync<PaymentAuthorisationsList>("/v2/payments/authorise", HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Returns an object containing the confirmation details of a payment.
        /// </summary>
        /// <param name="id">Id of payment.</param>
        /// <returns>Asynchronous task, which returns the confirmation details of a payment.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaymentConfirmation> GetPaymentConfirmationAsync(string id)
        {
            return await RequestAsync<PaymentConfirmation>("/v2/payments/" + id + "/confirmation", HttpMethod.Get,
                null);
        }

        /// <summary>
        /// Returns an object containing the expected payment delivery date.
        /// </summary>
        /// <param name="paymentDeliveryDate">paymentDeliveryDate to query.</param>
        /// <returns>Asynchronous task, which returns the confirmation details of a payment.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaymentDeliveryDates> GetPaymentDeliveryDatesAsync(PaymentDeliveryDates paymentDeliveryDates)
        {
            var paramsObj = ParamsObject.CreateFromStaticObject(paymentDeliveryDates);

            if (!paymentDeliveryDates.PaymentDate.HasValue)
                throw new ArgumentException("Payment Date cannot be null");
            if (string.IsNullOrEmpty(paymentDeliveryDates.PaymentType))
                throw new ArgumentException("Payment Type cannot be null");
            if (string.IsNullOrEmpty(paymentDeliveryDates.Currency))
                throw new ArgumentException("Currency Type cannot be null");
            if (string.IsNullOrEmpty(paymentDeliveryDates.BankCountry))
                throw new ArgumentException("Bank Country cannot be null");

            return await RequestAsync<PaymentDeliveryDates>("/v2/payments/payment_delivery_date", HttpMethod.Get,
                paramsObj);
        }

        /// <summary>
        /// Assigns a payment fee table to a specific sub-account.
        /// </summary>
        /// <param name="paymentFeeId">ID of the payment fee</param>
        /// <param name="accountId">ID of the account to which the payment fee will be assigned</param>
        /// <returns>Asynchronous task, which returns the payment fee assignment.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaymentFeeAssignment> AssignPaymentFeeAsync(string paymentFeeId, string accountId)
        {
            if (string.IsNullOrEmpty(paymentFeeId))
                throw new ArgumentException("Payment Fee ID cannot be null");
            if (string.IsNullOrEmpty(accountId))
                throw new ArgumentException("Account ID cannot be null");

            var paramsObj = new ParamsObject();
            paramsObj.AddNotNull("payment_fee_id", paymentFeeId);
            paramsObj.AddNotNull("account_id", accountId);

            return await RequestAsync<PaymentFeeAssignment>("/v2/payments/assign_payment_fee", HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Gets the calculated quote for the fee that will be applied against a payment
        /// </summary>
        /// <param name="quotePaymentFee">Quote Payment Fee Details</param>
        /// <returns>Asynchronous task, which returns the Quote Payment Fee.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<QuotePaymentFee> GetQuotePaymentFee(QuotePaymentFee quotePaymentFee)
        {
            var paramsObj = ParamsObject.CreateFromStaticObject(quotePaymentFee);

            return await RequestAsync<QuotePaymentFee>("/v2/payments/quote_payment_fee", HttpMethod.Get, paramsObj);
        }

        /// <summary>
        /// Returns an object containing the tracking information of a payment.
        /// </summary>
        /// <param name="id">Id of payment.</param>
        /// <returns>Asynchronous task, which returns the tracking info of a payment.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaymentTrackingInfo> GetPaymentTrackingInfoAsync(string id)
        {
            return await RequestAsync<PaymentTrackingInfo>("/v2/payments/" + id + "/tracking_info", HttpMethod.Get,
                null);
        }

        #endregion

        #region Rates

        /// <summary>
        /// Gets a full quote for the requested currency based on the spread table of the active contact.
        /// </summary>
        /// <param name="detailedRates">Rate parameters object</param>
        /// <returns>Asynchronous task, which returns the requested rate.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Rate> GetRateAsync(DetailedRates detailedRates)
        {
            ParamsObject paramsObj = ParamsObject.CreateFromStaticObject(detailedRates);

            return await RequestAsync<Rate>("/v2/rates/detailed", HttpMethod.Get, paramsObj);
        }

        /// <summary>
        /// Gets core rate information for multiple currency pairs.
        /// </summary>
        /// <param name="currencyPair">Currency pair</param>
        /// <param name="ignoreInvalidPairs">Optional: Ignore invalid pairs</param>
        /// <returns>Asynchronous task, which returns the list of the found rates.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<RatesList> FindRatesAsync(string currencyPair, bool? ignoreInvalidPairs = null)
        {
            ParamsObject paramsObj = new ParamsObject();
            paramsObj.Add("CurrencyPair", currencyPair);
            paramsObj.AddNotNull("IgnoreInvalidPairs", ignoreInvalidPairs);


            return await RequestAsync<RatesList>("/v2/rates/find", HttpMethod.Get, paramsObj);
        }

        #endregion

        #region Reference

        /// <summary>
        /// Gets required beneficiary details and their basic validation formats.
        /// </summary>
        /// <param name="currency">Currency</param>
        /// <param name="bankAccountCountry">Optional: Bank account country</param>
        /// <param name="beneficiaryCountry">Optional: Beneficiary country</param>
        /// <returns>Asynchronous task, which returns the list of the required beneficiary details.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<BeneficiaryDetailsList> GetBeneficiaryRequiredDetailsAsync(string currency = null,
            string bankAccountCountry = null, string beneficiaryCountry = null)
        {
            ParamsObject optional = null;
            if (!string.IsNullOrEmpty(currency)
                || !string.IsNullOrEmpty(bankAccountCountry)
                || !string.IsNullOrEmpty(beneficiaryCountry)
               )
            {
                optional = new ParamsObject();
                optional.AddNotNull("Currency", currency);
                optional.AddNotNull("BankAccountCountry", bankAccountCountry);
                optional.AddNotNull("BeneficiaryCountry", beneficiaryCountry);
            }

            return await RequestAsync<BeneficiaryDetailsList>("/v2/reference/beneficiary_required_details",
                HttpMethod.Get, optional);
        }

        /// <summary>
        /// Gets dates for which the given currency pair can not be traded.
        /// </summary>
        /// <param name="conversionPair">Currency conversion pair.</param>
        /// <param name="startDate">Optional: start date</param>
        /// <returns>Asynchronous task, which returns the list of the conversion dates.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<ConversionDatesList> GetConversionDatesAsync(string conversionPair, DateOnly? startDate = null)
        {
            var paramsObj = new ParamsObject();
            paramsObj.Add("ConversionPair", conversionPair);
            paramsObj.AddNotNull("StartDate", startDate);

            return await RequestAsync<ConversionDatesList>("/v2/reference/conversion_dates", HttpMethod.Get, paramsObj);
        }

        /// <summary>
        /// Gets a list of all the currencies that are tradeable.
        /// </summary>
        /// <returns>Asynchronous task, which returns the list of the currencies.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Entity.List.CurrenciesList> GetAvailableCurrenciesAsync()
        {
            return await RequestAsync<Entity.List.CurrenciesList>("/v2/reference/currencies", HttpMethod.Get);
        }

        /// <summary>
        /// Gets a list of purpose codes for a given currency.
        /// </summary>
        /// <param name="currency">Currency to get the purpose codes for</param>
        /// <param name="entityType">Optional: entity (individual or company)</param>
        /// <param name="bankAccountCountry">Optional: bank account country</param>
        /// <returns>Asynchronous task, which returns the list purpose codes.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaymentPurposeCodeList> GetPaymentPurposeCodes(string currency, string bankAccountCountry,
            string entityType = null)
        {
            var paramsObj = new ParamsObject();
            paramsObj.Add("Currency", currency);
            paramsObj.Add("BankAccountCountry", bankAccountCountry);
            paramsObj.AddNotNull("EntityType", entityType);

            return await RequestAsync<PaymentPurposeCodeList>("/v2/reference/payment_purpose_codes", HttpMethod.Get,
                paramsObj);
        }

        /// <summary>
        /// Gets dates for which the given currency can not be paid.
        /// </summary>
        /// <param name="currency">Currency name.</param>
        /// <param name="startDate">Start date</param>
        /// <returns>Asynchronous task, which returns the list of the payment dates.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaymentDatesList> GetPaymentDatesAsync(string currency, DateOnly? startDate = null)
        {
            var paramsObj = new ParamsObject();
            paramsObj.Add("Currency", currency);
            paramsObj.AddNotNull("StartDate", startDate);


            return await RequestAsync<PaymentDatesList>("/v2/reference/payment_dates", HttpMethod.Get, paramsObj);
        }

        /// <summary>
        /// Gets settlement account information, detailing where funds need to be sent to.
        /// </summary>
        /// <param name="currency">Currency</param>
        /// <returns>Asynchronous task, which returns the list of the found rates.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<SettlementAccountsList> GetSettlementAccountsAsync(string currency = null)
        {
            ParamsObject optional = null;
            if (!string.IsNullOrEmpty(currency))
            {
                optional = new ParamsObject();
                optional.Add("Currency", currency);
            }

            return await RequestAsync<SettlementAccountsList>("/v2/reference/settlement_accounts", HttpMethod.Get,
                optional);
        }

        /// <summary>
        /// Gets required payer details and their basic validation formats.
        /// </summary>
        /// <param name="payerCountry">ISO 3166-1 country code</param>
        /// <param name="payerEntityType">Optional: Payer Entity Type (could be company or individual)</param>
        /// <param name="paymentType">Optional: Payment Type (could be priority or regular)</param>
        /// <returns>Asynchronous task, which returns required payer details and their basic validation formats.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PayerDetailsList> GetPayerRequiredDetailsAsync(string payerCountry,
            string payerEntityType = null, string paymentType = null)
        {
            ParamsObject optional = null;
            if (!string.IsNullOrEmpty(payerEntityType)
                || !string.IsNullOrEmpty(paymentType)
               )
            {
                optional = new ParamsObject();
                optional.AddNotNull("PayerEntityType", payerEntityType);
                optional.AddNotNull("PaymentType", paymentType);
            }

            var paramsObj = new ParamsObject();
            paramsObj.Add("PayerCountry", payerCountry);
            paramsObj.AddNotNull("PayerEntityType", payerEntityType);
            paramsObj.AddNotNull("PaymentType", paymentType);

            return await RequestAsync<PayerDetailsList>("/v2/reference/payer_required_details", HttpMethod.Get,
                paramsObj);
        }

        /// <summary>
        /// Gets Details of the bank associated to specified account.
        /// </summary>
        /// <param name="identifierType">IdentifierType</param>
        /// <param name="identifierValue">IdentifierValue</param>
        /// <returns>Asynchronous task, which returns the Bank Details.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<BankDetails> GetBankDetailsAsync(string identifierType, string identifierValue)
        {
            var paramsObj = new ParamsObject();
            paramsObj.AddNotNull("IdentifierType", identifierType);
            paramsObj.AddNotNull("IdentifierValue", identifierValue);

            return await RequestAsync<BankDetails>("/v2/reference/bank_details/find", HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Gets Payment Fee Rules.
        /// </summary>
        /// <param name="accountId">AccountId</param>
        /// <param name="paymentType">PaymentType</param>
        /// <param name="chargeType">ChargeType</param>
        /// <returns>Asynchronous task, which returns the Bank Details.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaymentFeeRulesList> GetPaymentFeeRulesAsync(string accountId = null,
            string paymentType = null, string chargeType = null)
        {
            var paramsObj = new ParamsObject();
            paramsObj.AddNotNull("AccountId", accountId);
            paramsObj.AddNotNull("PaymentType", paymentType);
            paramsObj.AddNotNull("ChargeType", chargeType);

            return await RequestAsync<PaymentFeeRulesList>("/v2/reference/payment_fee_rules", HttpMethod.Get,
                paramsObj);
        }

        #endregion

        #region Reports

        /// <summary>
        /// Finds report requests matching the given search criteria.
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns  the list of the report requests, as well as pagination information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedReportRequests> FindReportRequestsAsync(
            ReportRequestFindParameters parameters = null)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedReportRequests>("/v2/reports/report_requests/find", HttpMethod.Get,
                optional);
        }

        /// <summary>
        /// Gets details of the specified report request.
        /// </summary>
        /// <param name="id">Id of the requested report.</param>
        /// <returns>Asynchronous task, which returns the requested transaction.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<ReportRequest> GetReportRequestAsync(string id)
        {
            return await RequestAsync<ReportRequest>("/v2/reports/report_requests/" + id, HttpMethod.Get, null);
        }

        /// <summary>
        /// Creates a new Conversion Report.
        /// </summary>
        /// <param name="parameters">Parameters for new Report</param>
        /// <returns>Asynchronous task, which returns newly created conversion.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<ReportRequest> CreateConversionReportAsync(ReportParameters parameters = null)
        {
            var paramsObj = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<ReportRequest>("/v2/reports/conversions/create", HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Creates a new Payment Report.
        /// </summary>
        /// <param name="parameters">Parameters for new Report</param>
        /// <returns>Asynchronous task, which returns newly created conversion.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<ReportRequest> CreatePaymentReportAsync(ReportParameters parameters = null)
        {
            var paramsObj = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<ReportRequest>("/v2/reports/payments/create", HttpMethod.Post, paramsObj);
        }

        #endregion


        #region Transactions

        /// <summary>
        /// Gets details of a transaction.
        /// </summary>
        /// <param name="id">Id of the requested transaction.</param>
        /// <returns>Asynchronous task, which returns the requested transaction.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Transaction> GetTransactionAsync(string id)
        {
            return await RequestAsync<Transaction>("/v2/transactions/" + id, HttpMethod.Get, null);
        }

        /// <summary>
        /// Finds transactions matching the search criteria for the active user.
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns the list of the found transactions, as well as pagination information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedTransactions> FindTransactionsAsync(TransactionFindParameters parameters = null)
        {
            ParamsObject optional = parameters == null ? null : ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedTransactions>("/v2/transactions/find", HttpMethod.Get, optional);
        }

        /// <summary>
        /// Get Sender Details.
        /// </summary>
        /// <param name="id">Id of the requested transaction.</param>
        /// <returns>Asynchronous task, which returns details of the sender of funds.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<SenderDetails> GetSenderDetailsAsync(string id)
        {
            return await RequestAsync<SenderDetails>("/v2/transactions/sender/" + id, HttpMethod.Get, null);
        }

        #endregion

        #region Transfers

        /// <summary>
        /// Creates a new Transfer.
        /// </summary>
        /// <param name="transfer">Data object for new Transfer</param>
        /// <returns>Asynchronous task, which returns newly created conversion.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Transfer> CreateTransferAsync(Transfer transfer)
        {
            var paramsObj = ParamsObject.CreateFromStaticObject(transfer);

            return await RequestAsync<Transfer>("/v2/transfers/create", HttpMethod.Post, paramsObj);
        }

        /// <summary>
        /// Gets details of a transfer.
        /// </summary>
        /// <param name="id">Id of the requested conversion.</param>
        /// <returns>Asynchronous task, which returns the requested conversion.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Transfer> GetTransferAsync(string id)
        {
            return await RequestAsync<Transfer>("/v2/transfers/" + id, HttpMethod.Get, null);
        }

        /// <summary>
        /// Find all Transfers matching the given search criteria
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns structure containing the details of the IBAN assigned to the logged in account.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedTransfers> FindTransfersAsync(TransferFindParameters parameters = null)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedTransfers>("/v2/transfers/find", HttpMethod.Get, optional);
        }

        /// <summary>
        /// Cancels a transfer.
        /// </summary>
        /// <param name="id">Id of the transfer to be cancelled.</param>
        /// <returns>Asynchronous task, which returns the cancelled conversion.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<Transfer> CancelTransferAsync(string id)
        {
            return await RequestAsync<Transfer>("/v2/transfers/" + id + "/cancel", HttpMethod.Post, null);
        }

        #endregion

        #region VirtualAccounts

        /// <summary>
        /// Find Virtual Accounts assigned to the logged in account.
        /// </summary>
        /// <param name="parameters">Find parameters</param>
        /// <returns>Asynchronous task, which returns the details of the Virtual Accounts assigned to the logged in account.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedVirtualAccounts> FindVirtualAccountsAsync(
            VirtualAccountFindParameters parameters = null)
        {
            ParamsObject optional = ParamsObject.CreateFromStaticObject(parameters);

            return await RequestAsync<PaginatedVirtualAccounts>("/v2/virtual_accounts/find", HttpMethod.Get, optional);
        }

        #endregion

        #region WithdrawalAccounts

        /// <summary>
        /// Finds Withdrawal Accounts matching the accountId. If the account Id is omitted the withdrawal accounts
        /// for the house account and all sub-accounts are returned
        /// </summary>
        /// <param name="accountId">AccountId</param>
        /// <returns>Asynchronous task, which returns the list of the found Withdrawal Accounts, as well as pagination information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<PaginatedWithdrawalAccounts> FindWithdrawalAccountsAsync(String accountId = null)
        {
            var paramsObj = new ParamsObject();
            paramsObj.AddNotNull("AccountId", accountId);

            return await RequestAsync<PaginatedWithdrawalAccounts>("/v2/withdrawal_accounts/find", HttpMethod.Get,
                paramsObj);
        }

        /// <summary>
        /// Pull funds from a withdrawal account
        /// </summary>
        /// <param name="withdrawalAccountId">Id of withdrawal account to pull funds from.</param>
        /// <param name="amount">The amount of funds to pull</param>
        /// <param name="reference">The reference seen on the statement for pulled funds</param>
        /// <returns>Asynchronous task, which pulls funds from a withdrawal account.</returns>
        /// <exception cref="InvalidOperationException">Thrown when client is not initialized.</exception>
        /// <exception cref="ApiException">Thrown when API call fails.</exception>
        public async Task<WithdrawalAccountFunds> WithdrawalAccountsPullFundsAsync(string withdrawalAccountId,
            decimal amount,
            string reference)
        {
            if (string.IsNullOrEmpty(withdrawalAccountId))
                throw new ArgumentException("WithdrawalAccountId can not be null");
            if (string.IsNullOrEmpty(reference))
                throw new ArgumentException("Reference can not be null");
            var paramsObj = new ParamsObject();
            paramsObj.AddNotNull("Reference", reference);
            paramsObj.AddNotNull("Amount", amount);
            return await RequestAsync<WithdrawalAccountFunds>(
                "/v2/withdrawal_accounts/" + withdrawalAccountId + "/pull_funds",
                HttpMethod.Post, paramsObj);
        }

        #endregion
    }


    internal static class ApiExceptionFactory
    {
        private static Request CreateRequest(HttpRequestMessage requestMessage)
        {
            var query = requestMessage.RequestUri.Query;
            var queryParams = HttpUtility.ParseQueryString(query);

            var parameters = queryParams.Cast<string>().ToDictionary(key => key, value => queryParams[value]);
            var verb = requestMessage.Method.Method;
            var url = requestMessage.RequestUri.OriginalString;

            if (!string.IsNullOrEmpty(query))
            {
                url = url.Replace(query, string.Empty);
            }

            return new Request(parameters, verb, url);
        }

        private static Response CreateResponse(HttpStatusCode statusCode, HttpResponseHeaders responseHeaders)
        {
            IEnumerable<string> values;

            string requestId = string.Empty;
            if (responseHeaders.TryGetValues("X-Request-Id", out values))
            {
                requestId = values.First();
            }

            DateTime date = DateTime.MinValue;
            if (responseHeaders.TryGetValues("Date", out values))
            {
                DateTime.TryParse(values.First(), out date);
            }

            return new Response((int)statusCode, date, requestId);
        }

        private static async Task<List<Error>> CreateErrors(HttpContent content)
        {
            var errorString = await content.ReadAsStringAsync();
            var errorObject = JObject.Parse(errorString);

            var errors = from JProperty error in errorObject["error_messages"]
                         select new Error(
                             error.Name,
                             error.Value is JArray
                                 ? (from errorMessage in error.Value
                                    select new Error.ErrorMessage(
                               GetTokenValue(errorMessage, "code", "error_code"),
                               GetTokenValue(errorMessage, "message", "reason"),
                               GetParamsDictionary(errorMessage["params"])
                           )).ToList()
                                 : new List<Error.ErrorMessage>
                                 {
                            new Error.ErrorMessage(
                                GetTokenValue(error.Value, "code", "error_code"),
                                GetTokenValue(error.Value, "message", "reason"),
                                GetParamsDictionary(error.Value["params"])
                            )
                                 }
                         );

            return errors.ToList();

            static string GetTokenValue(JToken token, string primaryKey, string fallbackKey)
            {
                return token[primaryKey]?.Value<string>() ?? token[fallbackKey]?.Value<string>();
            }

            static Dictionary<string, string> GetParamsDictionary(JToken paramsToken)
            {
                if (paramsToken == null)
                {
                    return new Dictionary<string, string>();
                }

                return (from JProperty param in paramsToken
                        select new KeyValuePair<string, string>(param.Name, param.Value.ToString()))
                    .ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public static async Task<ApiException> FromHttpResponse(HttpResponseMessage res)
        {
            var request = CreateRequest(res.RequestMessage);
            var response = CreateResponse(res.StatusCode, res.Headers);
            var errors = await CreateErrors(res.Content);

            switch (response.StatusCode)
            {
                case 400:
                    return new BadRequestException(request, response, errors);
                case 401:
                    return new AuthenticationException(request, response, errors);
                case 403:
                    return new ForbiddenException(request, response, errors);
                case 404:
                    return new NotFoundException(request, response, errors);
                case 422:
                    return new ValidationException(request, response, errors);
                case 429:
                    return new TooManyRequestsException(request, response, errors);
                case 500:
                    return new InternalApplicationException(request, response, errors);
                default:
                    return new UndefinedException(request, response, errors);
            }
        }
    }

    internal class PascalContractResolver : DefaultContractResolver
    {
        protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
        {
            JsonDictionaryContract contract = base.CreateDictionaryContract(objectType);

            if (objectType.GenericTypeArguments[0] == typeof(string))
            {
                contract.Converter = new PascalDictionaryConverter();
            }

            return contract;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            var jsonPropertyAttribute =
                member.GetCustomAttributes(typeof(JsonPropertyAttribute), false).LastOrDefault();
            var attributePropertyName = ((JsonPropertyAttribute)jsonPropertyAttribute)?.PropertyName;

            property.PropertyName = string.IsNullOrWhiteSpace(attributePropertyName)
                ? property.PropertyName.ToSnakeCase()
                : attributePropertyName;

            return property;
        }
    }

    internal class PascalDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            JObject obj = JObject.Load(reader);
            JsonReader objReader = obj.CreateReader();

            objReader.Culture = reader.Culture;
            objReader.DateFormatString = reader.DateFormatString;
            objReader.DateParseHandling = reader.DateParseHandling;
            objReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
            objReader.FloatParseHandling = reader.FloatParseHandling;
            objReader.SupportMultipleContent = reader.SupportMultipleContent;

            dynamic src = Activator.CreateInstance(objectType);
            dynamic res = Activator.CreateInstance(objectType);

            serializer.Populate(objReader, src);

            foreach (var prop in src)
            {
                res.Add((prop.Key as string).ToPascalCase(), prop.Value);
            }

            return res;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
