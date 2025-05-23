using System;
using System.Net.Http;
using System.Threading;
using CurrencyCloud.Authorization;
using CurrencyCloud.Options;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using CurrencyCloudClient = CurrencyCloud.Client;

namespace CurrencyCloud.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCurrencyCloudClient(
        this IServiceCollection serviceCollection,
        CurrencyCloudOptions options)
    {
        EnsureArg.IsNotNull(serviceCollection);
        EnsureArg.IsNotNull(options);
        EnsureArg.IsNotNullOrWhiteSpace(options.LoginId);
        EnsureArg.IsNotNullOrWhiteSpace(options.ApiKey);

        serviceCollection.AddSingleton(
            new AuthorizationOptions(new Credentials(options.LoginId, options.ApiKey), options.TokenLifetime));

        serviceCollection.AddSingleton<IAuthorizationService, AuthorizationService>();

        serviceCollection.AddHttpClient<AuthorizationService>(
                client =>
                {
                    client.BaseAddress = new Uri(options.ApiServer.Url);
                    client.Timeout = options.Timeout;
                })
            .ConfigurePrimaryHttpMessageHandler(
                () => new SocketsHttpHandler
                {
                    PooledConnectionLifetime = options.PooledConnectionLifetime
                })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

        serviceCollection
            .AddScoped<ICurrencyCloudClient, CurrencyCloudClient>();

        serviceCollection
            .AddHttpClient<CurrencyCloudClient>()
            .ConfigureHttpClient(
                (_, client) =>
                {
                    client.BaseAddress = new Uri(options.ApiServer.Url);
                    client.Timeout = options.Timeout;
                })
            .ConfigurePrimaryHttpMessageHandler(
                () => new SocketsHttpHandler
                {
                    PooledConnectionLifetime = options.PooledConnectionLifetime
                })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

        Retry.Enabled = true;
        
        return serviceCollection;
    }
}
