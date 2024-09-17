using CurrencyCloud.Authorization;
using CurrencyCloud.Environment;
using CurrencyCloud.Tests.Mock.Data;
using CurrencyCloud.Tests.Mock.Http;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyCloud.Tests
{
    public static class TestHelper
    {
        public static Client GetClient(AuthorizationOptions authorizationOptions)
        {
            string userAgent = "CurrencyCloudSDK/2.0 .NET/6.5.0";

            var httpClientFactory = CreateHttpClientFactory(userAgent, Authentication.ApiServer.Url);

            var authorizationServiceHttpClient = httpClientFactory.CreateClient(nameof(AuthorizationService));

            var clientHttpClient = httpClientFactory.CreateClient(nameof(Client));

            var authorizationService =
                new AuthorizationService(httpClientFactory, authorizationOptions);

            return new Client(clientHttpClient, authorizationService);
        }

        private static IHttpClientFactory CreateHttpClientFactory(string userAgent, string baseUrl)
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };

            var authorizationServiceHttpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl)
            };
            authorizationServiceHttpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

            var clientHttpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl)
            };
            clientHttpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

            return new TestHttpClientFactory(new Dictionary<string, HttpClient>
            {
                { nameof(AuthorizationService), authorizationServiceHttpClient },
                { nameof(Client), clientHttpClient }
            });
        }
    }

    public class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly Dictionary<string, HttpClient> clients;

        public TestHttpClientFactory(Dictionary<string, HttpClient> clients)
        {
            this.clients = clients ?? throw new ArgumentNullException(nameof(clients));
        }

        public HttpClient CreateClient(string name)
        {
            if (clients.ContainsKey(name))
            {
                return clients[name];
            }

            throw new InvalidOperationException($"HttpClient for {name} not configured.");
        }
    }
}
