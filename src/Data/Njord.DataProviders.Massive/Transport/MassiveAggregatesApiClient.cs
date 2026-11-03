using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Njord.Core.Serialization;
using Njord.Core.Retry;
using Njord.DataProviders.Massive.Configuration;
using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Errors;
using Njord.DataProviders.Massive.Serialization;

namespace Njord.DataProviders.Massive.Transport;

internal sealed class MassiveAggregatesApiClient :
    IMassiveAggregatesApiClient,
    IMassiveCorporateActionsApiClient
{
    #region Client configuration

    public const string HttpClientName = "Njord.Massive";

    private const int MaximumAggregateLimit = 50_000;

    private readonly HttpClient _httpClient;
    private readonly MassiveDataProviderOptions _options;
    private readonly RetryPolicy _retryPolicy;
    private readonly MassiveCircuitBreaker _circuitBreaker;
    private readonly TimeProvider _timeProvider;

    public MassiveAggregatesApiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<MassiveDataProviderOptions> options,
        RetryPolicy retryPolicy,
        MassiveCircuitBreaker circuitBreaker,
        TimeProvider timeProvider)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientName);
        _options = options.Value;
        _retryPolicy = retryPolicy;
        _circuitBreaker = circuitBreaker;
        _timeProvider = timeProvider;
    }

    #endregion

    #region Historical aggregates

    public async Task<IReadOnlyList<MassiveAggregateResponseDto>> GetHistoricalAggregatesAsync(
        MassiveResolvedOhlcRequest request,
        CancellationToken cancellationToken)
    {
        request.Validate();

        var pages = new List<MassiveAggregateResponseDto>();
        var nextPage = BuildInitialUri(request);

        while (nextPage is not null)
        {
            if (pages.Count >= _options.MaximumPagesPerQuery)
                throw new MassiveDataProviderException(
                    $"Massive pagination exceeded the configured maximum of {_options.MaximumPagesPerQuery} pages.");

            var page = await SendPageAsync<MassiveAggregateResponseDto>(
                nextPage,
                "aggregate",
                cancellationToken).ConfigureAwait(false);
            if (page.Results is null)
                throw new MassiveDataProviderException(
                    "Massive returned a response without a results array.",
                    requestId: page.RequestId);
            if (page.Ticker is not null && !string.Equals(page.Ticker, request.Ticker, StringComparison.Ordinal))
                throw new MassiveDataProviderException(
                    "Massive returned aggregate data for a ticker different from the requested ticker.",
                    requestId: page.RequestId);

            pages.Add(page);
            nextPage = GetNextPageUri(page.NextUrl);
        }

        return pages.AsReadOnly();
    }

    public Task<IReadOnlyList<MassiveStockSplitDto>> GetHistoricalStockSplitsAsync(
        string ticker,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        GetHistoricalResultsAsync<MassiveStockSplitDto, MassiveStockSplitResponseDto>(
            BuildCorporateActionUri("splits", ticker, "execution_date", from, to),
            "stock split",
            cancellationToken);

    public Task<IReadOnlyList<MassiveStockDividendDto>> GetHistoricalStockDividendsAsync(
        string ticker,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        GetHistoricalResultsAsync<MassiveStockDividendDto, MassiveStockDividendResponseDto>(
            BuildCorporateActionUri("dividends", ticker, "ex_dividend_date", from, to),
            "stock dividend",
            cancellationToken);

    public Task<IReadOnlyList<MassiveStockIpoDto>> GetHistoricalStockIposAsync(
        string ticker,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        GetHistoricalResultsAsync<MassiveStockIpoDto, MassiveStockIpoResponseDto>(
            BuildIpoUri(ticker, from, to),
            "stock IPO",
            cancellationToken);

    public async Task<IReadOnlyList<MassiveTickerEventDto>> GetTickerEventsAsync(
        string ticker,
        CancellationToken cancellationToken)
    {
        var events = new List<MassiveTickerEventDto>();
        var nextPage = BuildTickerEventsUri(ticker);
        var pageCount = 0;

        while (nextPage is not null)
        {
            if (pageCount >= _options.MaximumPagesPerQuery)
                throw new MassiveDataProviderException(
                    $"Massive ticker-event pagination exceeded the configured maximum of {_options.MaximumPagesPerQuery} pages.");

            var response = await SendPageAsync<MassiveTickerEventsResponseDto>(
                nextPage,
                "ticker event",
                cancellationToken).ConfigureAwait(false);
            pageCount++;
            if (response.Results?.Events is null)
                throw new MassiveDataProviderException(
                    "Massive returned a ticker event response without an events array.",
                    requestId: response.RequestId);

            events.AddRange(response.Results.Events);
            nextPage = GetNextPageUri(response.NextUrl);
        }

        return events.AsReadOnly();
    }

    private async Task<IReadOnlyList<TItem>> GetHistoricalResultsAsync<TItem, TResponse>(
        Uri firstPageUri,
        string responseType,
        CancellationToken cancellationToken)
        where TResponse : class, IMassivePagedResponseDto<TItem>
    {
        var results = new List<TItem>();
        var nextPage = firstPageUri;
        var pageCount = 0;

        while (nextPage is not null)
        {
            if (pageCount >= _options.MaximumPagesPerQuery)
                throw new MassiveDataProviderException(
                    $"Massive {responseType} pagination exceeded the configured maximum of {_options.MaximumPagesPerQuery} pages.");

            var page = await SendPageAsync<TResponse>(
                nextPage,
                responseType,
                cancellationToken).ConfigureAwait(false);
            pageCount++;
            if (page.Results is null)
                throw new MassiveDataProviderException(
                    $"Massive returned a {responseType} response without a results array.",
                    requestId: page.RequestId);

            results.AddRange(page.Results);
            nextPage = GetNextPageUri(page.NextUrl);
        }

        return results.AsReadOnly();
    }

    private async Task<TResponse> SendPageAsync<TResponse>(
        Uri requestUri,
        string responseType,
        CancellationToken cancellationToken)
        where TResponse : class, IMassiveApiResponseDto
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(
                token => SendPageAttemptAsync<TResponse>(requestUri, responseType, token),
                exception => IsRetryable(exception, cancellationToken),
                GetRetryAfter,
                cancellationToken).ConfigureAwait(false);
        }
        catch (MassiveTransientHttpException exception)
        {
            throw new MassiveDataProviderException(
                $"Massive returned transient HTTP status {(int)exception.StatusCode}.",
                exception.StatusCode,
                exception.RequestId,
                exception);
        }
        catch (HttpRequestException exception)
        {
            throw new MassiveDataProviderException(
                "The Massive HTTP request failed after the configured retries.",
                innerException: exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MassiveDataProviderException(
                "The Massive HTTP request timed out after the configured retries.",
                innerException: exception);
        }
    }

    private async Task<TResponse> SendPageAttemptAsync<TResponse>(
        Uri requestUri,
        string responseType,
        CancellationToken cancellationToken)
        where TResponse : class, IMassiveApiResponseDto
    {
        var isProbe = _circuitBreaker.ThrowIfOpenAndAcquireProbe();

        try
        {
            using var response = await SendAsync(requestUri, cancellationToken).ConfigureAwait(false);

            var requestId = GetRequestId(response);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _circuitBreaker.RecordSuccess();
                throw new MassiveRateLimitException(GetRetryAfter(response), requestId);
            }

            if (IsTransient(response.StatusCode))
            {
                var exception = new MassiveTransientHttpException(
                    response.StatusCode,
                    requestId,
                    GetRetryAfter(response));
                _circuitBreaker.RecordTransientFailure();
                throw exception;
            }

            if (!response.IsSuccessStatusCode)
            {
                _circuitBreaker.RecordSuccess();
                var errorResponse = await TryReadErrorResponseAsync(response, cancellationToken)
                    .ConfigureAwait(false);
                var errorRequestId = GetRequestId(response) ?? errorResponse?.RequestId;
                var details = errorResponse?.Error ?? errorResponse?.Message;
                var message = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    ? $"Massive denied access (HTTP {(int)response.StatusCode}). Verify the configured API key and account access to this endpoint."
                    : $"Massive returned HTTP status {(int)response.StatusCode}.";
                if (!string.IsNullOrWhiteSpace(details))
                    message = $"{message} Massive detail: {details}";

                throw new MassiveDataProviderException(
                    message,
                    response.StatusCode,
                    errorRequestId);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            TResponse? result;
            try
            {
                result = json.Déserialize<TResponse>();
            }
            catch (JsonException exception)
            {
                _circuitBreaker.RecordSuccess();
                throw new MassiveDataProviderException(
                    $"Massive returned malformed {responseType} JSON.",
                    response.StatusCode,
                    requestId,
                    exception);
            }

            if (result is null)
            {
                _circuitBreaker.RecordSuccess();
                throw new MassiveDataProviderException(
                    $"Massive returned an empty {responseType} response.",
                    response.StatusCode,
                    requestId);
            }

            result.RequestId ??= requestId;
            if (!(result.Status == "OK"
             || result.Status == "DELAYED"))
            {
                _circuitBreaker.RecordSuccess();
                var detail = result.Error ?? result.Message ?? "The response status was not OK.";
                throw new MassiveDataProviderException(detail, response.StatusCode, result.RequestId);
            }

            _circuitBreaker.RecordSuccess();
            return result;
        }
        catch (HttpRequestException)
        {
            _circuitBreaker.RecordTransientFailure();
            throw;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _circuitBreaker.RecordTransientFailure();
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _circuitBreaker.ReleaseProbe(isProbe);
            throw;
        }
    }

    private static bool IsRetryable(Exception exception, CancellationToken cancellationToken) =>
        exception is MassiveRateLimitException ||
        exception is MassiveTransientHttpException ||
        exception is HttpRequestException ||
        (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    private TimeSpan? GetRetryAfter(Exception exception) =>
        exception switch
        {
            MassiveRateLimitException rateLimitException => rateLimitException.RetryAfter,
            MassiveTransientHttpException transientException => transientException.RetryAfter,
            _ => null
        };

    #endregion

    #region Request URI construction

    private Uri BuildInitialUri(MassiveResolvedOhlcRequest request)
    {
        var ticker = Uri.EscapeDataString(request.Ticker);
        var from = request.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = request.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string relativePath;
        var query = new List<KeyValuePair<string, string>>
        {
            new("sort", request.AssetClass == MassiveAssetClass.Future ? "window_start.asc" : "asc"),
            new("limit", MaximumAggregateLimit.ToString(CultureInfo.InvariantCulture))
        };

        if (request.AssetClass == MassiveAssetClass.Future)
        {
            var resolutionUnit = request.Timespan switch
            {
                MassiveTimespan.Second => "sec",
                MassiveTimespan.Minute => "min",
                MassiveTimespan.Hour => "hour",
                MassiveTimespan.Session => "session",
                MassiveTimespan.Week => "week",
                MassiveTimespan.Month => "month",
                MassiveTimespan.Quarter => "quarter",
                MassiveTimespan.Year => "year",
                _ => throw new ArgumentException("The requested timespan is not supported for futures.", nameof(request))
            };

            relativePath = $"futures/v1/aggs/{ticker}";
            query.Add(new("resolution", $"{request.Multiplier}{resolutionUnit}"));
            query.Add(new("window_start.gte", from));
            query.Add(new("window_start.lte", to));
        }
        else
        {
            if (request.Timespan == MassiveTimespan.Session)
                throw new ArgumentException("Session aggregates are only supported for futures.", nameof(request));

            var timespan = request.Timespan.ToString().ToLowerInvariant();
            relativePath = $"v2/aggs/ticker/{ticker}/range/{request.Multiplier}/{timespan}/{from}/{to}";
            if (request.Adjusted.HasValue)
                query.Add(new("adjusted", request.Adjusted.Value ? "true" : "false"));
        }

        return CreateUri(relativePath, query);
    }

    private Uri BuildCorporateActionUri(
        string endpoint,
        string ticker,
        string dateParameter,
        DateOnly from,
        DateOnly to)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
        if (from > to)
            throw new ArgumentException("The start date must not be after the end date.");

        var query =
            new List<KeyValuePair<string, string>>
            {
                new("ticker", ticker),
                new($"{dateParameter}.gte", from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new($"{dateParameter}.lte", to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new("limit", "5000"),
                new("sort", $"{dateParameter}.asc")
            };
        return CreateUri($"stocks/v1/{endpoint}", query);
    }

    private Uri BuildIpoUri(string ticker, DateOnly from, DateOnly to)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
        if (from > to)
            throw new ArgumentException("The start date must not be after the end date.");

        var query =
            new List<KeyValuePair<string, string>>
            {
                new("ticker", ticker),
                new("listing_date.gte", from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new("listing_date.lte", to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new("limit", "1000"),
                new("sort", "listing_date"),
                new("order", "asc")
            };
        return CreateUri("vX/reference/ipos", query);
    }

    private Uri BuildTickerEventsUri(string ticker)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
        var escapedTicker = Uri.EscapeDataString(ticker);
        return new Uri(_options.BaseAddress!, $"vX/reference/tickers/{escapedTicker}/events?types=ticker_change");
    }

    private Uri CreateUri(string relativePath, IEnumerable<KeyValuePair<string, string>> query)
    {
        var uri = new Uri(_options.BaseAddress!, relativePath);
        var builder = new UriBuilder(uri)
        {
            Query = string.Join(
                "&",
                query.Select(parameter =>
                    $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"))
        };
        return builder.Uri;
    }

    private Uri? GetNextPageUri(string? nextUrl)
    {
        if (string.IsNullOrWhiteSpace(nextUrl))
            return null;

        var nextUri = Uri.TryCreate(nextUrl, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : new Uri(_options.BaseAddress!, nextUrl);
        if (!IsSameOrigin(nextUri, _options.BaseAddress!))
            throw new MassiveDataProviderException("Massive returned a pagination URL outside the configured API origin.");

        var parameters = nextUri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(parameter =>
            {
                var name = parameter.Split('=', StringSplitOptions.TrimEntries)[0];
                return !string.Equals(Uri.UnescapeDataString(name), "apiKey", StringComparison.OrdinalIgnoreCase);
            });
        var builder = new UriBuilder(nextUri)
        {
            Query = string.Join("&", parameters)
        };
        return builder.Uri;
    }

    private async Task<HttpResponseMessage> SendAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        return await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool IsSameOrigin(Uri left, Uri right) =>
        left.Scheme.Equals(right.Scheme, StringComparison.OrdinalIgnoreCase) &&
        left.Host.Equals(right.Host, StringComparison.OrdinalIgnoreCase) &&
        left.Port == right.Port &&
        string.IsNullOrEmpty(left.UserInfo);

    #endregion

    #region HTTP response handling

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        (int)statusCode >= 500;

    private static async Task<MassiveErrorResponseDto?> TryReadErrorResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return json.Déserialize<MassiveErrorResponseDto>();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string? GetRequestId(HttpResponseMessage response) =>
        response.Headers.TryGetValues("request_id", out var values)
            ? values.FirstOrDefault()
            : null;

    private TimeSpan? GetRetryAfter(HttpResponseMessage? response)
    {
        var retryAfter = response?.Headers.RetryAfter;
        if (retryAfter?.Delta is not null)
            return retryAfter.Delta;

        if (retryAfter?.Date is not null)
            return retryAfter.Date.Value - _timeProvider.GetUtcNow();

        return null;
    }

    #endregion
}
