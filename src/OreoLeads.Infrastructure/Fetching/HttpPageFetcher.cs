using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Infrastructure.Fetching;

/// <summary>Bounded public HTML fetcher. DNS is validated at connection time, TLS is never bypassed.</summary>
public sealed class HttpPageFetcher(ILogger<HttpPageFetcher> logger) : IPageFetcher
{
    private const int MaxBytes = 2 * 1024 * 1024;
    // Fixed stripes bound memory and serialize each host, even under high domain churn.
    private static readonly SemaphoreSlim[] Gates = Enumerable.Range(0, 256).Select(_ => new SemaphoreSlim(1)).ToArray();

    public async Task<PageFetchResult> FetchAsync(string url, CancellationToken ct = default)
    {
        var watch = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            var uri = new Uri(url, UriKind.Absolute);
            using var handler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false, UseProxy = false, UseCookies = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                ConnectCallback = async (context, token) =>
                {
                    var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, token);
                    if (addresses.Length == 0) throw new InvalidOperationException("No public DNS address.");
                    foreach (var address in addresses) SsrfGuard.ValidateIp(address);
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        await socket.ConnectAsync(new IPEndPoint(addresses[0], context.DnsEndPoint.Port), token);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch { socket.Dispose(); throw; }
                }
            };
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OreoLeads/1.0 (+https://oreostudios.fr)");
            for (var redirects = 0; redirects <= 5; redirects++)
            {
                await SsrfGuard.ValidateAsync(uri.AbsoluteUri, timeout.Token);
                if (!uri.IsDefaultPort || uri.UserInfo.Length > 0) throw new InvalidOperationException("Only public HTTP/HTTPS default ports are permitted.");
                var host = uri.IdnHost;
                if (host == "linkedin.com" || host.EndsWith(".linkedin.com", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("LinkedIn is search-result discovery only.");
                var gate = Gates[(uint)StringComparer.OrdinalIgnoreCase.GetHashCode(host) % Gates.Length];
                await gate.WaitAsync(timeout.Token);
                try
                {
                    using var robots = await client.GetAsync(new Uri(uri, "/robots.txt"), HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                    if (robots.StatusCode != HttpStatusCode.NotFound)
                    {
                        if (!robots.IsSuccessStatusCode) throw new InvalidOperationException("Robots policy unavailable; fetch deferred.");
                        if (robots.Content.Headers.ContentType?.MediaType != "text/plain")
                            throw new InvalidOperationException("Robots policy is not plain text; fetch deferred.");
                        var policy = await ReadBoundedAsync(robots, 256 * 1024, timeout.Token);
                        if (!RobotsPolicy.Allows(policy, uri.PathAndQuery)) throw new InvalidOperationException("Disallowed by robots.txt.");
                    }
                    using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                    if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
                    {
                        if (response.Headers.Location == null) throw new InvalidOperationException("Redirect without location.");
                        uri = new Uri(uri, response.Headers.Location);
                        continue;
                    }
                    var type = response.Content.Headers.ContentType?.MediaType;
                    var html = response.IsSuccessStatusCode && type is "text/html" or "application/xhtml+xml"
                        ? await ReadBoundedAsync(response, MaxBytes, timeout.Token) : null;
                    return new(html, uri.AbsoluteUri, (int)response.StatusCode, (int)watch.ElapsedMilliseconds,
                        redirects, uri.Scheme == "https", false,
                        !response.IsSuccessStatusCode ? $"HTTP {(int)response.StatusCode}" : html == null ? "Not HTML" : null);
                }
                finally { gate.Release(); }
            }
            throw new InvalidOperationException("Redirect limit exceeded.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Public page fetch failed for {Url}", url);
            return new(null, url, 0, (int)watch.ElapsedMilliseconds, 0, false, false, ex.Message);
        }
    }

    private static async Task<string> ReadBoundedAsync(HttpResponseMessage response, int limit, CancellationToken ct)
    {
        if (response.Content.Headers.ContentLength > limit) throw new InvalidOperationException("Response size limit exceeded.");
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var bytes = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(bytes, ct)) > 0)
        {
            if (buffer.Length + read > limit) throw new InvalidOperationException("Response size limit exceeded.");
            buffer.Write(bytes, 0, read);
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
