using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Njord.Core.Mapping;
using Njord.Core.Retry;
using Njord.Data.MarketData;
using Njord.Data.MarketData.Contracts;
using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Mapping;
using Njord.DataProviders.Massive.Provider;
using Njord.DataProviders.Massive.Transport;
using Njord.Models.MarketData;

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
        services.AddSingleton<IValidateOptions<RetryPolicyOptions>, RetryPolicyOptionsValidator>();
        services.AddOptions<RetryPolicyOptions>()
            .Bind(configuration.GetSection($"{MassiveDataProviderOptions.SectionName}:Retry"))
            .ValidateOnStart();

        services.AddHttpClient(MassiveAggregatesApiClient.HttpClientName, (serviceProvider, client) =>
        {
            client.Timeout = serviceProvider
                .GetRequiredService<IOptions<MassiveDataProviderOptions>>()
                .Value
                .RequestTimeout;
        });

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<MassiveCircuitBreaker>();
        services.AddSingleton<RetryPolicy>();
        services.AddTransient<MassiveAggregatesApiClient>();
        services.AddTransient<IMassiveAggregatesApiClient>(serviceProvider =>
            serviceProvider.GetRequiredService<MassiveAggregatesApiClient>());
        services.AddTransient<IMassiveCorporateActionsApiClient>(serviceProvider =>
            serviceProvider.GetRequiredService<MassiveAggregatesApiClient>());
        services.AddSingleton<
            IMapper<MassiveOhlcMappingInput, IReadOnlyList<Ohlc>>,
            MassiveOhlcMapper>();
        services.TryAddTransient<MarketDataProviderSelector>();
        services.AddTransient<MassiveHistoricalMarketDataProvider>();
        services.AddTransient<IMassiveHistoricalMarketDataProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<MassiveHistoricalMarketDataProvider>());
        services.AddTransient<IMarketDataProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<MassiveHistoricalMarketDataProvider>());
        services.AddTransient<IMarketDataProvider<IMarketData>>(serviceProvider =>
            serviceProvider.GetRequiredService<MassiveHistoricalMarketDataProvider>());
        services.AddTransient<ICorporateActionProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<MassiveHistoricalMarketDataProvider>());

        return services;
    }

    #endregion
}
