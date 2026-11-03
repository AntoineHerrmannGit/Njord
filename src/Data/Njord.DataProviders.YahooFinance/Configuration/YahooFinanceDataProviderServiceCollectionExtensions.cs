using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Njord.Core.Mapping;
using Njord.DataProviders.YahooFinance.Contracts;
using Njord.DataProviders.YahooFinance.Mapping;
using Njord.DataProviders.YahooFinance.Provider;
using Njord.DataProviders.YahooFinance.Transport;

namespace Njord.DataProviders.YahooFinance.Configuration;

/// <summary>Registers Yahoo Finance historical market-data services.</summary>
public static class YahooFinanceDataProviderServiceCollectionExtensions
{
    #region Dependency injection

    /// <summary>Adds Yahoo Finance using the DataProviders:YahooFinance configuration section.</summary>
    public static IServiceCollection AddYahooFinanceDataProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IValidateOptions<YahooFinanceDataProviderOptions>, YahooFinanceDataProviderOptionsValidator>();
        services.AddOptions<YahooFinanceDataProviderOptions>()
            .Bind(configuration.GetSection(YahooFinanceDataProviderOptions.SectionName))
            .ValidateOnStart();

        services.AddHttpClient(YahooFinanceChartApiClient.HttpClientName, (serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<YahooFinanceDataProviderOptions>>()
                .Value;
            client.BaseAddress = options.BaseAddress;
            client.Timeout = options.RequestTimeout;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Njord/1.0");
        });

        services.TryAddSingleton(TimeProvider.System);
        services.AddTransient<IYahooFinanceChartApiClient>(serviceProvider =>
            new YahooFinanceChartApiClient(
                serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<IOptions<YahooFinanceDataProviderOptions>>(),
                serviceProvider.GetRequiredService<TimeProvider>()));
        services.AddSingleton<
            IMapper<YahooFinanceOhlcMappingInput, YahooFinanceOhlcMappingResult>,
            YahooFinanceOhlcMapper>();
        services.AddSingleton<
            IMapper<YahooFinanceHistoricalOhlcRequest, YahooFinanceIntervalMapping>,
            YahooFinanceIntervalMapper>();
        services.AddTransient<IYahooFinanceHistoricalMarketDataProvider, YahooFinanceHistoricalMarketDataProvider>();

        return services;
    }

    #endregion
}
