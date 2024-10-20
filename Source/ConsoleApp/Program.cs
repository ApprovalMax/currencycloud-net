using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CurrencyCloud;
using CurrencyCloud.Authorization;
using CurrencyCloud.Environment;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp;

internal static class Program
{
    private const string LoginId = "INSERT YOUR LOGIN ID HERE";
    private const string ApiKey = "INSERT YOUR API KEY HERE";
    
    public static async Task Main(string[] args)
    {
        var serviceProvider = RegisterServices();
        var client = new Client(serviceProvider.GetRequiredService<IHttpClientFactory>(), serviceProvider.GetRequiredService<IAuthorizationService>());

        // execute client here
    }

    private static ServiceProvider RegisterServices()
    {
        var serviceCollection = new ServiceCollection()
            .AddHttpClient()
            .AddSingleton<IAuthorizationService, AuthorizationService>(); 

        serviceCollection.AddHttpClient<Client>(
                client =>
                {
                    client.BaseAddress = new Uri(ApiServer.Demo.Url);
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
            .ConfigurePrimaryHttpMessageHandler(
                () => new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromSeconds(300)
                })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);


        serviceCollection.AddHttpClient<AuthorizationService>(
                client =>
                {
                    client.BaseAddress = new Uri(ApiServer.Demo.Url);
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
            .ConfigurePrimaryHttpMessageHandler(
                () => new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromSeconds(300)
                })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

        serviceCollection.AddSingleton(
            new AuthorizationOptions(
                new Credentials(LoginId,  ApiKey),
                TimeSpan.FromMinutes(25)));

        return serviceCollection.BuildServiceProvider();
    }
}
