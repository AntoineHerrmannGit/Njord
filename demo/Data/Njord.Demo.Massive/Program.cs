using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Njord.Core.FileManagement;
using Njord.DataProviders.Massive.Configuration;
using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Errors;

namespace Njord.Demo.Massive;

internal static class Program
{
    #region Application

    private static async Task<int> Main(string[] args)
    {
        var configurationPath = FindConfigurationPath();

        if (!TryReadRequest(args, out var ticker, out var from, out var to))
        {
            Console.Error.WriteLine("Usage : Njord.Demo.Massive [ticker] [date-debut yyyy-MM-dd] [date-fin yyyy-MM-dd]");
            return 2;
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(configurationPath, optional: false, reloadOnChange: false)
                .Build();
            var services = new ServiceCollection();
            services.AddMassiveDataProvider(configuration);

            using var serviceProvider = services.BuildServiceProvider();
            var provider = serviceProvider.GetRequiredService<IMassiveHistoricalMarketDataProvider>();
            var request = new MassiveHistoricalOhlcRequest
            {
                Ticker = ticker,
                AssetClass = MassiveAssetClass.Stock,
                From = from,
                To = to,
                Multiplier = 1,
                Timespan = MassiveTimespan.Day,
                Adjusted = true
            };

            Console.WriteLine($"Récupération Massive de {ticker}, du {from:yyyy-MM-dd} au {to:yyyy-MM-dd}...");
            var bars = await provider.GetHistoricalOhlcAsync(request, cancellationTokenSource.Token);

            if (bars.Count == 0)
                Console.WriteLine("Aucune barre disponible pour cette période et ce ticker.");
            else
            {
                foreach (var bar in bars)
                    Console.WriteLine(
                        $"{bar.Timestamp:O} O={bar.Open} H={bar.High} L={bar.Low} C={bar.Close} V={bar.Volume?.ToString() ?? "n/d"}");

                Console.WriteLine($"{bars.Count} barre(s) reçue(s).");
            }

            return 0;
        }
        catch (OptionsValidationException exception)
        {
            Console.Error.WriteLine($"Configuration Massive invalide : {exception.Message}");
            return 1;
        }
        catch (MassiveDataProviderException exception)
        {
            Console.Error.WriteLine($"La requête Massive a échoué : {exception.Message}");
            if (!string.IsNullOrWhiteSpace(exception.RequestId))
                Console.Error.WriteLine($"Request ID : {exception.RequestId}");

            return 1;
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            Console.Error.WriteLine("Requête annulée.");
            return 130;
        }
    }

    #endregion

    #region Input and configuration

    private static string FindConfigurationPath()
        => FileManager.FindFile("massive.settings.json");

    private static bool TryReadRequest(
        IReadOnlyList<string> args,
        out string ticker,
        out DateOnly from,
        out DateOnly to)
    {
        ticker = args.Count > 0 ? args[0] : "AAPL";
        to = DateOnly.FromDateTime(DateTime.UtcNow);
        from = to.AddDays(-10);

        if (args.Count > 3)
            return false;

        if (args.Count > 1 &&
            !DateOnly.TryParseExact(args[1], "yyyy-MM-dd", out from))
            return false;

        if (args.Count > 2 &&
            !DateOnly.TryParseExact(args[2], "yyyy-MM-dd", out to))
            return false;

        return from <= to;
    }

    #endregion
}
