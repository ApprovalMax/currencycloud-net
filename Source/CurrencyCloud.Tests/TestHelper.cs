using CurrencyCloud.Authorization;
using CurrencyCloud.Tests.Mock.Data;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Net.Http;
using System;

namespace CurrencyCloud.Tests
{
    public static class TestHelper
    {
        public static Client GetClient(AuthorizationOptions authorizationOptions)
        {
            string userAgent = "CurrencyCloudSDK/2.0 .NET/6.5.0";

            var httpClientFactory = CreateHttpClientFactory(userAgent, Authentication.ApiServer.Url);

            var logger = NullLogger<AuthorizationService>.Instance;

            var authorizationService =
                new AuthorizationService(httpClientFactory, authorizationOptions, logger);

            return new Client(httpClientFactory, authorizationService);
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

            var clientHttpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl)
            };

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
