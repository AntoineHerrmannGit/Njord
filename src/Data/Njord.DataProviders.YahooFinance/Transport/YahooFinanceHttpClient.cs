using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Njord.Core.Serialization;
using Njord.DataProviders.YahooFinance.Serialization;
using Njord.MarketData;

namespace Njord.DataProviders.YahooFinance.Transport;

internal sealed class YahooFinanceHttpClient
{
    private const string ChartBaseAddress = "https://query1.finance.yahoo.com/v8/finance/chart/";
    private const string OptionsBaseAddress = "https://query1.finance.yahoo.com/v7/finance/options/";
    private const string CookieBootstrapAddress = "https://fc.yahoo.com/";
    private const string CrumbAddress = "https://query1.finance.yahoo.com/v1/test/getcrumb";

    private readonly HttpClient _httpClient;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly SemaphoreSlim _optionsRequestGate = new(1, 1);
    private string? _optionsCrumb;

    public YahooFinanceHttpClient(HttpClient httpClient, IJsonSerializer jsonSerializer)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(jsonSerializer);
        _httpClient = httpClient;
        _jsonSerializer = jsonSerializer;
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Njord", "1.0"));
        }
    }

    public Task<JsonElement> GetChartResponseAsync(string symbol, string query, CancellationToken cancellationToken)
    {
        var normalizedSymbol = Uri.EscapeDataString(symbol.Trim().ToUpperInvariant());
        var uri = new Uri($"{ChartBaseAddress}{normalizedSymbol}?{query}", UriKind.Absolute);
        return GetResponseAsync<YahooFinanceChartResponse>(
            uri,
            response => response.Chart,
            cancellationToken);
    }

    public async Task<JsonElement> GetOptionChainResponseAsync(
        string symbol,
        DateOnly? expiration,
        CancellationToken cancellationToken)
    {
        await _optionsRequestGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                if (_optionsCrumb is null || attempt > 0)
                {
                    _optionsCrumb = await GetOptionsCrumbAsync(cancellationToken).ConfigureAwait(false);
                }

                try
                {
                    var uri = BuildOptionChainUri(symbol, expiration, _optionsCrumb);
                    var response = await GetResponseAsync<YahooFinanceOptionChainResponse>(
                        uri,
                        envelope => envelope.OptionChain,
                        cancellationToken).ConfigureAwait(false);

                    if (!YahooFinanceJsonReader.IsAuthorizationError(response))
                    {
                        YahooFinanceJsonReader.ThrowIfApiError(response);
                        return response;
                    }

                    _optionsCrumb = null;
                }
                catch (MarketDataProviderException exception)
                    when (exception.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                {
                    _optionsCrumb = null;
                }
            }

            throw new MarketDataProviderException(
                "YahooFinance",
                "Yahoo Finance rejected the option-chain request after refreshing its session crumb.",
                HttpStatusCode.Unauthorized);
        }
        finally
        {
            _optionsRequestGate.Release();
        }
    }

    private async Task<TResponse> GetEnvelopeAsync<TResponse>(Uri uri, CancellationToken cancellationToken)
        where TResponse : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new MarketDataProviderException(
                "YahooFinance",
                $"Yahoo Finance returned HTTP {(int)response.StatusCode}.",
                response.StatusCode);
        }

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            return await _jsonSerializer.DeserializeAsync<TResponse>(
                stream,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonSerializationException exception)
        {
            throw YahooFinanceJsonReader.InvalidResponse("Yahoo Finance returned malformed JSON.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new MarketDataProviderException(
                "YahooFinance",
                "Reading the Yahoo Finance response failed.",
                innerException: exception);
        }
    }

    private async Task<JsonElement> GetResponseAsync<TResponse>(
        Uri uri,
        Func<TResponse, JsonElement> getPayload,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var envelope = await GetEnvelopeAsync<TResponse>(uri, cancellationToken).ConfigureAwait(false);
        var payload = getPayload(envelope);
        if (payload.ValueKind == JsonValueKind.Undefined)
        {
            throw YahooFinanceJsonReader.InvalidResponse("Yahoo Finance response did not contain the expected payload.");
        }

        return payload.Clone();
    }

    private async Task<string> GetOptionsCrumbAsync(CancellationToken cancellationToken)
    {
        using (var bootstrapRequest = new HttpRequestMessage(HttpMethod.Get, CookieBootstrapAddress))
        using (var bootstrapResponse = await SendRequestAsync(bootstrapRequest, cancellationToken).ConfigureAwait(false))
        {
            if (!bootstrapResponse.IsSuccessStatusCode &&
                bootstrapResponse.StatusCode != HttpStatusCode.NotFound)
            {
                throw new MarketDataProviderException(
                    "YahooFinance",
                    $"Yahoo Finance session endpoint returned HTTP {(int)bootstrapResponse.StatusCode}.",
                    bootstrapResponse.StatusCode);
            }
        }

        using var crumbRequest = new HttpRequestMessage(HttpMethod.Get, CrumbAddress);
        using var crumbResponse = await SendRequestAsync(crumbRequest, cancellationToken).ConfigureAwait(false);
        if (!crumbResponse.IsSuccessStatusCode)
        {
            throw new MarketDataProviderException(
                "YahooFinance",
                $"Yahoo Finance crumb endpoint returned HTTP {(int)crumbResponse.StatusCode}.",
                crumbResponse.StatusCode);
        }

        string crumbText;
        try
        {
            crumbText = await crumbResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            throw new MarketDataProviderException(
                "YahooFinance",
                "Reading the Yahoo Finance session crumb failed.",
                innerException: exception);
        }

        var crumb = crumbText.Trim();
        if (string.IsNullOrWhiteSpace(crumb) || crumb.Contains('<', StringComparison.Ordinal))
        {
            throw YahooFinanceJsonReader.InvalidResponse(
                "Yahoo Finance returned an invalid session crumb for option-chain access.");
        }

        return crumb;
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            throw new MarketDataProviderException(
                "YahooFinance",
                "The Yahoo Finance request failed.",
                innerException: exception);
        }
    }

    private static Uri BuildOptionChainUri(string symbol, DateOnly? expiration, string crumb)
    {
        var normalizedSymbol = Uri.EscapeDataString(symbol.Trim().ToUpperInvariant());
        var query = expiration is { } date
            ? $"?date={new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}"
            : string.Empty;
        var separator = query.Length == 0 ? "?" : "&";
        return new Uri(
            $"{OptionsBaseAddress}{normalizedSymbol}{query}{separator}crumb={Uri.EscapeDataString(crumb)}",
            UriKind.Absolute);
    }
}
