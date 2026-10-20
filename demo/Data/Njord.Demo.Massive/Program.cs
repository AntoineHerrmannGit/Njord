using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Njord.Core.FileManagement;
using Njord.Data.Referential.Configuration;
using Njord.Data.Referential.Contracts;
using Njord.Data.Referential.Errors;
using Njord.DataProviders.Massive.Configuration;
using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Errors;

namespace Njord.Demo.Massive;

internal static class Program
{
    #region Application

    private static async Task<int> Main(string[] args)
    {
        if (!TryReadRequest(args, out var tickers, out var from, out var to))
        {
            Console.Error.WriteLine("Usage : Njord.Demo.Massive [ticker1,ticker2,...] [date-debut yyyy-MM-dd] [date-fin yyyy-MM-dd]");
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
            var massiveConfiguration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("massive.settings.json", optional: false, reloadOnChange: false)
                .Build();
            var referentialConfigurationPath = FindReferentialConfigurationPath();
            var referentialConfiguration = new ConfigurationBuilder()
                .AddJsonFile(referentialConfigurationPath, optional: false, reloadOnChange: false)
                .Build();
            var services = new ServiceCollection();
            services.AddNjordReferential(referentialConfiguration);
            services.AddMassiveDataProvider(massiveConfiguration);

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var provider = scope.ServiceProvider.GetRequiredService<IMassiveHistoricalMarketDataProvider>();
            var request = new MassiveHistoricalOhlcRequest
            {
                Tickers = tickers,
                From = from,
                To = to,
                Multiplier = 1,
                Timespan = MassiveTimespan.Day
            };

            Console.WriteLine(
                $"Récupération Massive de {string.Join(", ", tickers)}, du {from:yyyy-MM-dd} au {to:yyyy-MM-dd}...");
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
            Console.Error.WriteLine($"Configuration invalide : {exception.Message}");
            return 1;
        }
        catch (FileNotFoundException exception)
        {
            Console.Error.WriteLine(
                $"Fichier de configuration manquant : {exception.FileName ?? exception.Message}");
            return 1;
        }
        catch (JsonException exception)
        {
            Console.Error.WriteLine($"Fichier de configuration JSON invalide : {exception.Message}");
            return 1;
        }
        catch (InvalidDataException exception)
        {
            Console.Error.WriteLine($"Base JSON du référentiel invalide : {exception.Message}");
            return 1;
        }
        catch (TickerReferenceResolutionException exception)
        {
            Console.Error.WriteLine($"Référentiel incomplet ou ambigu : {exception.Message}");
            return 1;
        }
        catch (TickerReferenceConflictException exception)
        {
            Console.Error.WriteLine($"Référentiel invalide : {exception.Message}");
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

    private static string FindReferentialConfigurationPath()
        => FileManager.FindFile("referential.settings.local.json");

    private static bool TryReadRequest(
        IReadOnlyList<string> args,
        out IReadOnlyList<string> tickers,
        out DateOnly from,
        out DateOnly to)
    {
        tickers = args.Count > 0
            ? args[0].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : ["AAPL"];
        to = DateOnly.FromDateTime(DateTime.UtcNow);
        from = to.AddDays(-10);

        if (args.Count > 3 || tickers.Count == 0)
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
