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
            var authorizationServiceHttpClient = new HttpClient();
            string userAgent = "CurrencyCloudSDK/2.0 .NET/6.5.0";
            authorizationServiceHttpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            authorizationServiceHttpClient.BaseAddress = new Uri(Authentication.ApiServer.Url);

            var authorizationService = new AuthorizationService(authorizationServiceHttpClient, authorizationOptions, new TokenState());

            var clientHttpClient = new HttpClient();
            clientHttpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            clientHttpClient.BaseAddress = new Uri(Authentication.ApiServer.Url);

            return new Client(clientHttpClient, authorizationService);
        }
    }
}
