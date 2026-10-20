using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Njord.Data.Referential.Contracts;
using Njord.Data.Referential.Persistence;
using Njord.Data.Referential.Services;

namespace Njord.Data.Referential.Configuration;

/// <summary>Registers the JSON-backed Njord instrument referential.</summary>
public static class ReferentialServiceCollectionExtensions
{
    #region Dependency injection

    /// <summary>Adds referential services using the Referential configuration section.</summary>
    public static IServiceCollection AddNjordReferential(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IValidateOptions<ReferentialOptions>, ReferentialOptionsValidator>();
        services.AddOptions<ReferentialOptions>()
            .Bind(configuration.GetSection(ReferentialOptions.SectionName))
            .ValidateOnStart();
        services.AddScoped<IInstrumentMappingRepository, JsonInstrumentMappingRepository>();
        services.AddScoped<IInstrumentReferential, InstrumentReferential>();

        return services;
    }

    #endregion
}
