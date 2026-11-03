using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Njord.Core.FileManagement;
using Njord.Core.Serialization;
using Njord.Data.MarketData.Contracts;
using Njord.Data.Referential.Configuration;
using Njord.Data.Referential.Contracts;
using Njord.Data.Referential.Errors;
using Njord.Data.Referential.Models;
using Njord.DataProviders.Massive.Configuration;
using Njord.DataProviders.Massive.Errors;
using Njord.Models.MarketData;

namespace Njord.Demo.Massive;

internal static class Program
{
    #region Demo instruments

    private static readonly DateOnly From = new(2025, 1, 2);
    private static readonly DateOnly To = new(2025, 1, 10);

    private static readonly DemoInstrument[] Instruments =
    [
        new()
        {
            AssetClass = "Stock",
            Ticker = "AAPL",
            Description = "Apple Inc."
        },
        new()
        {
            AssetClass = "Index",
            Ticker = "NDX",
            Description = "Nasdaq-100 index"
        },
        new()
        {
            AssetClass = "Option",
            Ticker = "AAPL-2025-01-17-C-100",
            Description = "AAPL call, expiry 2025-01-17, strike 100"
        },
        new()
        {
            AssetClass = "Future",
            Ticker = "ES=2025-09-19",
            Description = "E-mini S&P 500 September 2025 future"
        },
        new()
        {
            AssetClass = "Crypto",
            Ticker = "BTC/USD",
            Description = "Bitcoin / U.S. dollar"
        },
        new()
        {
            AssetClass = "Forex",
            Ticker = "EUR/USD",
            Description = "Euro / U.S. dollar"
        }
    ];

    #endregion

    #region Application

    private static async Task<int> Main()
    {
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
            var solutionRoot = Path.GetDirectoryName(FileManager.FindFile("Njord.slnx"))
                ?? throw new InvalidOperationException("The Njord solution root could not be determined.");
            var referentialConfigurationPath = Path.Combine(
                solutionRoot,
                "demo",
                "Data",
                "Njord.Demo.Massive",
                "massive-referential.settings.example.json");
            if (!File.Exists(referentialConfigurationPath))
                throw new FileNotFoundException(
                    "Create massive-referential.settings.example.json from massive-referential.settings.example.json before running the Massive demo.",
                    referentialConfigurationPath);

            var referentialConfiguration = new ConfigurationBuilder()
                .AddJsonFile(referentialConfigurationPath, optional: false, reloadOnChange: false)
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
            services.AddMassiveDataProvider(massiveConfiguration);

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var referential = scope.ServiceProvider.GetRequiredService<IInstrumentReferential>();
            var provider = scope.ServiceProvider.GetRequiredService<IMarketDataProvider<IMarketData>>();
            await AddMissingDemoMappingsAsync(
                referential,
                absoluteDataFilePath,
                cancellationTokenSource.Token);

            Console.WriteLine("Njord — historique OHLC Massive");
            Console.WriteLine($"Période inclusive : {From:yyyy-MM-dd} à {To:yyyy-MM-dd} (barres journalières)");
            Console.WriteLine($"Base du référentiel : {absoluteDataFilePath}");
            Console.WriteLine("Résolution du référentiel (ticker Njord et symbole Massive) :");

            foreach (var instrument in Instruments)
            {
                try
                {
                    var mappings = await referential.ResolveAsync(
                        instrument.Ticker,
                        "Massive",
                        From,
                        To,
                        cancellationTokenSource.Token);

                    foreach (var mapping in mappings)
                        Console.WriteLine(
                            $"  {instrument.AssetClass,-7} Njord={mapping.Ticker} | Massive={mapping.ProviderTicker} | {instrument.Description}");
                }
                catch (TickerReferenceResolutionException exception)
                {
                    Console.WriteLine(
                        $"  {instrument.AssetClass,-7} Njord={instrument.Ticker} | mapping non résolu : {exception.Message}");
                }
            }

            Console.WriteLine(
                $"Récupération vectorielle de {Instruments.Length} ticker(s)...");
            var results = await provider.Get<Ohlc>(
                new MarketDataHistoricalOhlcRequest
                {
                    Tickers = Instruments.Select(instrument => instrument.Ticker).ToArray(),
                    From = From,
                    To = To,
                    Multiplier = 1,
                    Interval = MarketDataInterval.Day
                },
                cancellationTokenSource.Token);
            var barCount = 0;
            Console.WriteLine();
            Console.WriteLine("Résultats (la disponibilité dépend du symbole, de l'historique et du compte Massive) :");
            foreach (var result in results)
            {
                var instrument = Instruments.Single(item =>
                    string.Equals(item.Ticker, result.Ticker, StringComparison.OrdinalIgnoreCase));
                Console.WriteLine();
                Console.WriteLine($"[{instrument.AssetClass}] {result.Ticker} — {result.Data.Count} barre(s)");

                if (result.Data.Count == 0)
                    Console.WriteLine("  Aucune barre exploitable renvoyée.");

                foreach (var bar in result.Data)
                {
                    Console.WriteLine(
                        $"  {bar.Timestamp:O} O={bar.Open} H={bar.High} L={bar.Low} C={bar.Close} " +
                        $"V={bar.Volume?.ToString() ?? "n/d"}");
                    barCount++;
                }

                foreach (var diagnostic in result.Diagnostics)
                    Console.Error.WriteLine(
                        $"  Diagnostic [{diagnostic.Code}] {result.Ticker} : {diagnostic.Message}");
            }

            Console.WriteLine();
            if (barCount == 0)
            {
                Console.Error.WriteLine("Aucune barre OHLC n'a été récupérée.");
                return 1;
            }

            Console.WriteLine($"{barCount} barre(s) reçue(s) au total.");
            Console.WriteLine(
                "Ces données de démonstration ne constituent ni une validation financière, ni une validation de qualité ou de complétude.");
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

    #region Referential initialization

    private static async Task AddMissingDemoMappingsAsync(
        IInstrumentReferential referential,
        string referentialDataFilePath,
        CancellationToken cancellationToken)
    {
        var mappings = (await referential.GetMappingsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        var instrumentsWithoutMassiveMappings = Instruments
            .Where(instrument => !mappings.Any(mapping =>
                string.Equals(mapping.Ticker, instrument.Ticker, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(mapping.Provider, "Massive", StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        if (instrumentsWithoutMassiveMappings.Length == 0)
            return;

        var seedJsonPath = Path.Combine(AppContext.BaseDirectory, "massive-instruments.example.json");
        var seedJson = await File.ReadAllTextAsync(seedJsonPath, cancellationToken).ConfigureAwait(false);
        var seedMappings = seedJson.Déserialize<List<TickerProviderMapping>>()
            ?? throw new JsonException("Le fichier d'exemple Massive doit contenir un tableau de mappings.");
        var mappingsToAdd = instrumentsWithoutMassiveMappings
            .Select(instrument => seedMappings.SingleOrDefault(mapping =>
                string.Equals(mapping.Ticker, instrument.Ticker, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(mapping.Provider, "Massive", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException(
                    $"Aucun mapping Massive d'exemple n'existe pour le ticker '{instrument.Ticker}'."))
            .ToArray();

        Console.WriteLine(
            $"Ajout de {mappingsToAdd.Length} mapping(s) d'exemple Massive absent(s) dans le référentiel configuré.");
        await referential.AddMappingsAsync(mappingsToAdd, cancellationToken).ConfigureAwait(false);
        Console.WriteLine(
            $"Mappings ajoutés à '{referentialDataFilePath}'. Les mappings déjà présents n'ont pas été modifiés.");
    }

    #endregion

    private sealed record DemoInstrument
    {
        #region Instrument identity

        public required string AssetClass { get; init; }

        public required string Ticker { get; init; }

        public required string Description { get; init; }

        #endregion
    }
}
