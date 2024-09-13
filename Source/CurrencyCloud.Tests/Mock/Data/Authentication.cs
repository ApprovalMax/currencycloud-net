using CurrencyCloud.Authorization;
using CurrencyCloud.Environment;
using System.Net;

namespace CurrencyCloud.Tests.Mock.Data
{
    static class Authentication
    {
        public static readonly Credentials Credentials = new Credentials(
            "development@currencycloud.com",
            "deadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef"
        );

        public static readonly ApiServer ApiServer = ApiServer.Mock;

        public static readonly AuthorizationOptions AuthorizationOptions = new(Credentials, 30);
    }
}
