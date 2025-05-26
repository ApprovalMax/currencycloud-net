using System;
using CurrencyCloud.Environment;

namespace CurrencyCloud.Options;

public sealed record CurrencyCloudOptions
{
    public ApiServer ApiServer => ApiServerFromString(this.ApiServerIdentifier);
    public required string LoginId { get; init; }
    public required string ApiKey { get; init; }
    public required TimeSpan TokenLifetime { get; init; }
    public required TimeSpan Timeout { get; init; }
    public required TimeSpan PooledConnectionLifetime { get; init; }

    private string ApiServerIdentifier { get; init; } = null!;

    private static ApiServer ApiServerFromString(string apiServer) => apiServer switch
    {
        "Demo" => ApiServer.Demo,
        "Production" => ApiServer.Production,
        "Mock" => ApiServer.Mock,
        _ => throw new ArgumentException("Invalid ApiServer specified")
    };
}
