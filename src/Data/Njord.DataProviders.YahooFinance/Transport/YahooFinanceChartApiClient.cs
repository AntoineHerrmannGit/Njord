using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Njord.Core.Retry;
using Njord.Core.Serialization;
using Njord.DataProviders.YahooFinance.Configuration;
using Njord.DataProviders.YahooFinance.Contracts;
using Njord.DataProviders.YahooFinance.Errors;
using Njord.DataProviders.YahooFinance.Serialization;

namespace Njord.DataProviders.YahooFinance.Transport;

internal sealed class YahooFinanceChartApiClient : IYahooFinanceChartApiClient
{
    #region Client configuration

    public const string HttpClientName = "Njord.YahooFinance";

    private readonly HttpClient _httpClient;
    private readonly YahooFinanceDataProviderOptions _options;
    private readonly RetryPolicy _retryPolicy;
    private readonly TimeProvider _timeProvider;

    public YahooFinanceChartApiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<YahooFinanceDataProviderOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _httpClient = httpClientFactory.CreateClient(HttpClientName);
        _options = options.Value;
        _timeProvider = timeProvider;
        _retryPolicy = new RetryPolicy(Options.Create(_options.Retry), _timeProvider);
    }

    #endregion

    #region Chart retrieval

    public async Task<YahooFinanceChartResultDto> GetHistoricalChartAsync(
        YahooFinanceResolvedOhlcRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestUri = BuildChartUri(request);

        try
        {
            var response = await _retryPolicy.ExecuteAsync(
                token => SendAttemptAsync(requestUri, token),
                exception => IsRetryable(exception, cancellationToken),
                GetRetryAfter,
                cancellationToken).ConfigureAwait(false);

            var chart = response.Chart;
            if (chart is null)
                throw new YahooFinanceDataProviderException("Yahoo Finance returned a response without a chart object.");

            if (chart.Error is not null)
                throw new YahooFinanceDataProviderException(
                    $"Yahoo Finance reported '{chart.Error.Code ?? "unknown"}': {chart.Error.Description ?? "no description"}.",
                    errorCode: chart.Error.Code);

            if (chart.Result is not { Count: 1 } || chart.Result[0] is null)
                throw new YahooFinanceDataProviderException("Yahoo Finance returned no chart result for the requested ticker and range.");

            return chart.Result[0];
        }
        catch (YahooFinanceTransientHttpException exception)
        {
            throw new YahooFinanceDataProviderException(
                $"Yahoo Finance returned transient HTTP status {(int)exception.StatusCode} after the configured retries.",
                exception.StatusCode,
                innerException: exception);
        }
        catch (HttpRequestException exception)
        {
            throw new YahooFinanceDataProviderException(
                "The Yahoo Finance HTTP request failed after the configured retries.",
                innerException: exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new YahooFinanceDataProviderException(
                "The Yahoo Finance HTTP request timed out after the configured retries.",
                innerException: exception);
        }
    }

    private async Task<YahooFinanceChartResponseDto> SendAttemptAsync(
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient
            .GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
            (int)response.StatusCode >= 500)
            throw new YahooFinanceTransientHttpException(response.StatusCode, GetRetryAfter(response));

        if (!response.IsSuccessStatusCode)
            throw new YahooFinanceDataProviderException(
                $"Yahoo Finance returned HTTP status {(int)response.StatusCode}.",
                response.StatusCode);

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return json.Déserialize<YahooFinanceChartResponseDto>()
                ?? throw new YahooFinanceDataProviderException("Yahoo Finance returned an empty JSON response.");
        }
        catch (JsonException exception)
        {
            throw new YahooFinanceDataProviderException(
                "Yahoo Finance returned malformed chart JSON.",
                innerException: exception);
        }
    }

    private static Uri BuildChartUri(YahooFinanceResolvedOhlcRequest request)
    {
        var period1 = ToUnixSeconds(request.From);
        var period2 = ToUnixSeconds(request.To.AddDays(1));
        var escapedTicker = Uri.EscapeDataString(request.Ticker);
        var relativeUri = string.Create(
            CultureInfo.InvariantCulture,
            $"v8/finance/chart/{escapedTicker}?period1={period1}&period2={period2}&interval={request.Interval}");
        return new Uri(relativeUri, UriKind.Relative);
    }

    private static long ToUnixSeconds(DateOnly date) =>
        new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();

    private static bool IsRetryable(Exception exception, CancellationToken cancellationToken) =>
        exception is YahooFinanceTransientHttpException ||
        exception is HttpRequestException ||
        (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    private TimeSpan? GetRetryAfter(Exception exception)
    {
        if (exception is not YahooFinanceTransientHttpException transientException)
            return null;

        return transientException.RetryAfter;
    }

    private TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is not null)
            return retryAfter.Delta;

        if (retryAfter?.Date is not null)
        {
            var delay = retryAfter.Date.Value - _timeProvider.GetUtcNow();
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return null;
    }

    #endregion

    #region Retry diagnostics

    private sealed class YahooFinanceTransientHttpException : Exception
    {
        public YahooFinanceTransientHttpException(HttpStatusCode statusCode, TimeSpan? retryAfter)
            : base($"Yahoo Finance returned transient HTTP status {(int)statusCode}.")
        {
            StatusCode = statusCode;
            RetryAfter = retryAfter;
        }

        public HttpStatusCode StatusCode { get; }

        public TimeSpan? RetryAfter { get; }
    }

    #endregion
}
