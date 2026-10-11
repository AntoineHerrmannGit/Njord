using System.Text.Json;
using Njord.DataProviders.YahooFinance.Serialization;
using Njord.DataProviders.YahooFinance.Transport;
using Njord.MarketData;
using static Njord.DataProviders.YahooFinance.Serialization.YahooFinanceJsonReader;

namespace Njord.DataProviders.YahooFinance.Options;

internal sealed class YahooFinanceOptionChainDataSource(YahooFinanceHttpClient httpClient)
{
    public async Task<MarketOptionChain> GetOptionChainAsync(
        OptionChainRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = await httpClient.GetOptionChainResponseAsync(
            request.Underlying.Symbol,
            request.Expiration,
            cancellationToken).ConfigureAwait(false);
        return MapOptionChain(response, request);
    }

    private static MarketOptionChain MapOptionChain(JsonElement response, OptionChainRequest request)
    {
        var results = GetRequiredProperty(response, "result");
        if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() != 1)
        {
            throw InvalidResponse("Yahoo Finance did not return exactly one option-chain result.");
        }

        var result = results[0];
        var underlyingSymbol = ReadOptionalString(result, "underlyingSymbol")
            ?? throw InvalidResponse("Yahoo Finance option-chain response did not include its underlying symbol.");
        if (!string.Equals(
                underlyingSymbol,
                request.Underlying.Symbol,
                StringComparison.OrdinalIgnoreCase))
        {
            throw InvalidResponse("Yahoo Finance returned an option chain for a different underlying instrument.");
        }

        var quoteMetadata = GetRequiredProperty(result, "quote");
        var currency = ReadOptionalString(quoteMetadata, "currency")
            ?? throw InvalidResponse("Yahoo Finance option-chain response did not include an underlying currency.");
        var exchange = ReadOptionalString(quoteMetadata, "fullExchangeName")
            ?? ReadOptionalString(quoteMetadata, "exchangeName");
        var underlying = CreateInstrument(request.Underlying, exchange);
        var availableExpirations = ReadExpirations(GetRequiredProperty(result, "expirationDates"));
        if (availableExpirations.Count == 0)
        {
            throw InvalidResponse("Yahoo Finance returned no available option expirations.");
        }

        var options = GetRequiredProperty(result, "options");
        if (options.ValueKind != JsonValueKind.Array || options.GetArrayLength() != 1)
        {
            throw InvalidResponse("Yahoo Finance did not return exactly one selected option expiration.");
        }

        var optionSet = options[0];
        var selectedExpiration = ReadOptionalInt64(optionSet, "expirationDate") is { } expirationTimestamp
            ? DateOnly.FromDateTime(ParseTimestamp(expirationTimestamp).UtcDateTime)
            : availableExpirations[0];
        if (!availableExpirations.Contains(selectedExpiration) ||
            request.Expiration is { } requestedExpiration && requestedExpiration != selectedExpiration)
        {
            throw InvalidResponse("Yahoo Finance returned an option chain for an unexpected expiration date.");
        }

        var calls = ReadOptionContracts(
            GetRequiredProperty(optionSet, "calls"),
            OptionType.Call,
            underlying,
            selectedExpiration,
            currency);
        var puts = ReadOptionContracts(
            GetRequiredProperty(optionSet, "puts"),
            OptionType.Put,
            underlying,
            selectedExpiration,
            currency);

        return new MarketOptionChain(
            underlying,
            selectedExpiration,
            availableExpirations,
            calls.Concat(puts),
            "YahooFinance");
    }

    private static MarketInstrument CreateInstrument(MarketInstrument instrument, string? providerExchange) =>
        new(instrument.Symbol.Trim().ToUpperInvariant(), instrument.AssetClass, instrument.Exchange ?? providerExchange);

    private static IReadOnlyList<DateOnly> ReadExpirations(JsonElement values)
    {
        if (values.ValueKind != JsonValueKind.Array)
        {
            throw InvalidResponse("Yahoo Finance returned invalid option expiration dates.");
        }

        var expirations = new List<DateOnly>(values.GetArrayLength());
        foreach (var value in values.EnumerateArray())
        {
            if (!value.TryGetInt64(out var timestamp))
            {
                throw InvalidResponse("Yahoo Finance returned an invalid option expiration timestamp.");
            }

            expirations.Add(DateOnly.FromDateTime(ParseTimestamp(timestamp).UtcDateTime));
        }

        return expirations.Distinct().Order().ToArray();
    }

    private static IReadOnlyList<MarketOptionContract> ReadOptionContracts(
        JsonElement values,
        OptionType type,
        MarketInstrument underlying,
        DateOnly selectedExpiration,
        string currency)
    {
        if (values.ValueKind != JsonValueKind.Array)
        {
            throw InvalidResponse($"Yahoo Finance returned an invalid {type} contracts array.");
        }

        var contracts = new List<MarketOptionContract>(values.GetArrayLength());
        foreach (var value in values.EnumerateArray())
        {
            var symbol = ReadOptionalString(value, "contractSymbol");
            var strike = ReadOptionalDecimal(value, "strike");
            var expiration = ReadOptionalInt64(value, "expiration");
            if (string.IsNullOrWhiteSpace(symbol) || strike is null || expiration is null)
            {
                throw InvalidResponse($"Yahoo Finance returned an incomplete {type} contract.");
            }

            if (DateOnly.FromDateTime(ParseTimestamp(expiration.Value).UtcDateTime) != selectedExpiration)
            {
                throw InvalidResponse($"Yahoo Finance returned a {type} contract with a mismatched expiration.");
            }

            DateTimeOffset? lastTradeTimestamp = ReadOptionalInt64(value, "lastTradeDate") is { } lastTrade
                ? ParseTimestamp(lastTrade)
                : null;

            try
            {
                contracts.Add(new MarketOptionContract(
                    new MarketInstrument(symbol, MarketAssetClass.Option),
                    underlying,
                    type,
                    selectedExpiration,
                    strike.Value,
                    ReadOptionalString(value, "currency") ?? currency,
                    "YahooFinance",
                    ReadOptionalDecimal(value, "lastPrice"),
                    ReadOptionalDecimal(value, "bid"),
                    ReadOptionalDecimal(value, "ask"),
                    lastTradeTimestamp,
                    ReadOptionalInt64(value, "volume"),
                    ReadOptionalInt64(value, "openInterest"),
                    ReadOptionalDecimal(value, "impliedVolatility"),
                    ReadOptionalBoolean(value, "inTheMoney")));
            }
            catch (ArgumentException exception)
            {
                throw InvalidResponse($"Yahoo Finance returned an invalid {type} contract '{symbol}'.", exception);
            }
        }

        return contracts;
    }
}
