using System.Net;
using System.Text;

namespace Njord.Data.Tests.Shared;

internal static class HttpTestResponses
{
    public static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}

internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;
    private readonly Action<HttpClient>? _configureClient;

    public StubHttpClientFactory(
        HttpMessageHandler handler,
        Action<HttpClient>? configureClient = null)
    {
        _handler = handler;
        _configureClient = configureClient;
    }

    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient(_handler, disposeHandler: false);
        _configureClient?.Invoke(client);
        return client;
    }
}

internal class QueuedHttpMessageHandler<TSnapshot> : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses;
    private readonly Func<HttpRequestMessage, TSnapshot> _snapshotFactory;

    public QueuedHttpMessageHandler(
        IEnumerable<HttpResponseMessage> responses,
        Func<HttpRequestMessage, TSnapshot> snapshotFactory)
    {
        _responses = new Queue<HttpResponseMessage>(responses);
        _snapshotFactory = snapshotFactory;
    }

    public List<TSnapshot> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(_snapshotFactory(request));

        if (_responses.Count == 0)
            throw new InvalidOperationException("No HTTP response was queued for the request.");

        return Task.FromResult(_responses.Dequeue());
    }
}
