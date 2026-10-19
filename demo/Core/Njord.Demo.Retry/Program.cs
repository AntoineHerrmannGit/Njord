using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Njord.Core.Retry;

namespace Njord.Demo.Retry;

internal static class Program
{
    #region Application

    private static async Task Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();
        var services = new ServiceCollection();
        services.AddOptions<RetryPolicyOptions>()
            .Bind(configuration.GetSection("Retry"));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<RetryPolicy>();

        using var serviceProvider = services.BuildServiceProvider();
        var policy = serviceProvider.GetRequiredService<RetryPolicy>();
        var options = serviceProvider.GetRequiredService<IOptions<RetryPolicyOptions>>().Value;
        var attempt = 0;

        Console.WriteLine($"Schéma configuré : {options.Pattern}");
        Console.WriteLine($"Nombre maximal de retries : {options.MaxRetryAttempts}");

        var result = await policy.ExecuteAsync(
            _ =>
            {
                attempt++;
                Console.WriteLine($"Tentative {attempt}");

                if (attempt < 3)
                    return Task.FromException<string>(new HttpRequestException("Erreur réseau simulée."));

                return Task.FromResult("Réponse reçue après retry.");
            },
            exception => exception is HttpRequestException,
            _ => null);

        Console.WriteLine(result);
    }

    #endregion
}
