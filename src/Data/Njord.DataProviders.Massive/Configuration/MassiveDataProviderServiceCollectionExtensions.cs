using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Mapping;
using Njord.DataProviders.Massive.Provider;
using Njord.DataProviders.Massive.Transport;

namespace Njord.DataProviders.Massive.Configuration;

/// <summary>Registers Massive historical market-data services.</summary>
public static class MassiveDataProviderServiceCollectionExtensions
{
    #region Dependency injection

    /// <summary>Adds the Massive provider using the DataProviders:Massive configuration section.</summary>
    public static IServiceCollection AddMassiveDataProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IValidateOptions<MassiveDataProviderOptions>, MassiveDataProviderOptionsValidator>();
        services.AddOptions<MassiveDataProviderOptions>()
            .Bind(configuration.GetSection(MassiveDataProviderOptions.SectionName))
            .ValidateOnStart();

        services.AddHttpClient(MassiveAggregatesApiClient.HttpClientName, (serviceProvider, client) =>
        {
            client.Timeout = serviceProvider
                .GetRequiredService<IOptions<MassiveDataProviderOptions>>()
                .Value
                .RequestTimeout;
        });

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<MassiveRequestRateLimiter>();
        services.AddSingleton<MassiveCircuitBreaker>();
        services.AddTransient<IMassiveAggregatesApiClient>(serviceProvider =>
            new MassiveAggregatesApiClient(
                serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<IOptions<MassiveDataProviderOptions>>(),
                serviceProvider.GetRequiredService<MassiveRequestRateLimiter>(),
                serviceProvider.GetRequiredService<MassiveCircuitBreaker>(),
                serviceProvider.GetRequiredService<TimeProvider>()));
        services.AddSingleton<IMassiveOhlcMapper, MassiveOhlcMapper>();
        services.AddTransient<IMassiveHistoricalMarketDataProvider, MassiveHistoricalMarketDataProvider>();

        return services;
    }

    #endregion
}
