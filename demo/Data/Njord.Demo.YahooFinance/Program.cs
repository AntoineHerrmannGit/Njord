using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Njord.Core.FileManagement;
using Njord.Data.Referential.Configuration;
using Njord.Data.Referential.Contracts;
using Njord.Data.Referential.Errors;
using Njord.DataProviders.YahooFinance.Configuration;
using Njord.DataProviders.YahooFinance.Contracts;
using Njord.DataProviders.YahooFinance.Errors;

namespace Njord.Demo.YahooFinance;

internal static class Program
{
    private static readonly DemoInstrument[] Instruments =
    [
        new("Stock", "AAPL", "Apple Inc."),
        new("Index", "SPX", "S&P 500 index"),
        new("Option", "AAPL=2025-01-17C250000", "AAPL call, expiry 2025-01-17, strike 250"),
        new("Future", "ES-CONTINUOUS", "E-mini S&P 500 continuous future"),
        new("Crypto", "BTC-USD", "Bitcoin / U.S. dollar"),
        new("Forex", "EURUSD", "Euro / U.S. dollar")
    ];

    private static async Task<int> Main(string[] args)
    {
        if (!TryReadRange(args, out var from, out var to))
        {
            Console.Error.WriteLine(
                "Usage : Njord.Demo.YahooFinance [date-debut yyyy-MM-dd] [date-fin yyyy-MM-dd]");
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
            var yahooConfiguration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("yahoo-finance.settings.json", optional: false, reloadOnChange: false)
                .Build();
            var solutionRoot = Path.GetDirectoryName(FileManager.FindFile("Njord.slnx"))
                ?? throw new InvalidOperationException("The Njord solution root could not be determined.");
            var referentialSettingsPath = FileManager.FindFile("referential.settings.local.json");
            var referentialConfiguration = new ConfigurationBuilder()
                .AddJsonFile(referentialSettingsPath, optional: false, reloadOnChange: false)
                .Build();
            var configuredDataFilePath = referentialConfiguration["Referential:DataFilePath"]
                ?? new ReferentialOptions().DataFilePath;
            var absoluteDataFilePath = Path.GetFullPath(configuredDataFilePath, solutionRoot);
            var resolvedReferentialConfiguration = new ConfigurationBuilder()
                .AddConfiguration(referentialConfiguration)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{ReferentialOptions.SectionName}:DataFilePath"] = absoluteDataFilePath
                })
                .Build();

            var services = new ServiceCollection();
            services.AddNjordReferential(resolvedReferentialConfiguration);
            services.AddYahooFinanceDataProvider(yahooConfiguration);

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var referential = scope.ServiceProvider.GetRequiredService<IInstrumentReferential>();
            var provider = scope.ServiceProvider.GetRequiredService<IYahooFinanceHistoricalMarketDataProvider>();

            Console.WriteLine("Njord — historique OHLC Yahoo Finance");
            Console.WriteLine($"Période inclusive : {from:yyyy-MM-dd} à {to:yyyy-MM-dd} (barres journalières)");
            Console.WriteLine("Une requête vectorielle Njord couvre les six classes ci-dessous.");
            Console.WriteLine("Le référentiel distingue les tickers canoniques des symboles Yahoo résolus :");

            foreach (var instrument in Instruments)
            {
                try
                {
                    var mappings = await referential.ResolveAsync(
                        instrument.Ticker,
                        "YahooFinance",
                        from,
                        to,
                        cancellationTokenSource.Token);

                    foreach (var mapping in mappings)
                        Console.WriteLine(
                            $"  {instrument.AssetClass,-7} Njord={mapping.Ticker} | Yahoo={mapping.ProviderTicker} | {instrument.Description}");
                }
                catch (TickerReferenceResolutionException exception)
                {
                    Console.WriteLine(
                        $"  {instrument.AssetClass,-7} Njord={instrument.Ticker} | mapping non résolu : {exception.Message}");
                }
            }

            var results = await provider.GetHistoricalOhlcAsync(
                new YahooFinanceHistoricalOhlcRequest
                {
                    Tickers = Instruments.Select(instrument => instrument.Ticker).ToArray(),
                    From = from,
                    To = to,
                    Multiplier = 1,
                    Timespan = YahooFinanceTimespan.Day
                },
                cancellationTokenSource.Token);

            Console.WriteLine();
            Console.WriteLine("Résultats (la disponibilité dépend du symbole, de l'historique Yahoo et du réseau) :");
            foreach (var result in results)
            {
                var instrument = Instruments.Single(item =>
                    string.Equals(item.Ticker, result.Ticker, StringComparison.OrdinalIgnoreCase));

                Console.WriteLine();
                Console.WriteLine($"[{instrument.AssetClass}] {result.Ticker} — {result.Bars.Count} barre(s)");

                if (!result.HasData)
                    Console.WriteLine("  Aucune barre exploitable renvoyée.");

                foreach (var bar in result.Bars)
                    Console.WriteLine(
                        $"  {bar.Timestamp:O} O={bar.Open} H={bar.High} L={bar.Low} C={bar.Close} " +
                        $"V={bar.Volume?.ToString() ?? "n/d"} Devise={bar.Currency ?? "n/d"}");

                foreach (var diagnostic in result.Diagnostics)
                {
                    var details = new List<string>();
                    if (diagnostic.StatusCode.HasValue)
                        details.Add($"HTTP {(int)diagnostic.StatusCode.Value} {diagnostic.StatusCode.Value}");
                    if (diagnostic.AffectedBarCount.HasValue)
                        details.Add($"barres omises={diagnostic.AffectedBarCount.Value}");
                    if (diagnostic.FirstAffectedTimestamp.HasValue)
                        details.Add($"première={diagnostic.FirstAffectedTimestamp.Value:O}");
                    var suffix = details.Count == 0 ? string.Empty : $" ({string.Join(", ", details)})";
                    Console.WriteLine($"  Diagnostic [{diagnostic.Code}]{suffix} : {diagnostic.Message}");
                }
            }

            Console.WriteLine();
            Console.WriteLine(
                "Limites : Yahoo peut ne pas fournir d'historique pour certaines options ou certains symboles; " +
                "les diagnostics sont par ticker. Aucun enrichissement ni pricing n'est effectué ici.");
            Console.WriteLine(
                "Ces données de démonstration ne constituent ni une validation financière, ni une validation de qualité ou de complétude.");
            return 0;
        }
        catch (OptionsValidationException exception)
        {
            Console.Error.WriteLine($"Configuration Yahoo invalide : {exception.Message}");
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
            Console.Error.WriteLine($"Fichier JSON invalide : {exception.Message}");
            return 1;
        }
        catch (InvalidDataException exception)
        {
            Console.Error.WriteLine($"Base JSON du référentiel invalide : {exception.Message}");
            return 1;
        }
        catch (TickerReferenceConflictException exception)
        {
            Console.Error.WriteLine($"Référentiel invalide : {exception.Message}");
            return 1;
        }
        catch (YahooFinanceDataProviderException exception)
        {
            Console.Error.WriteLine($"La requête Yahoo Finance a échoué : {exception.Message}");
            if (exception.StatusCode.HasValue)
                Console.Error.WriteLine(
                    $"HTTP {(int)exception.StatusCode.Value} {exception.StatusCode.Value}");
            return 1;
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            Console.Error.WriteLine("Requête annulée.");
            return 130;
        }
    }

    private static bool TryReadRange(IReadOnlyList<string> args, out DateOnly from, out DateOnly to)
    {
        from = new DateOnly(2025, 1, 2);
        to = new DateOnly(2025, 1, 10);

        if (args.Count > 2)
            return false;

        if (args.Count > 0 && !DateOnly.TryParseExact(args[0], "yyyy-MM-dd", out from))
            return false;

        if (args.Count > 1 && !DateOnly.TryParseExact(args[1], "yyyy-MM-dd", out to))
            return false;

        return from <= to && to < DateOnly.MaxValue;
    }

    private sealed record DemoInstrument(string AssetClass, string Ticker, string Description);
}
