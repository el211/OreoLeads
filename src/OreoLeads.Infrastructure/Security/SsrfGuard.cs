using System.Net;
using System.Net.Sockets;

namespace OreoLeads.Infrastructure.Security;

/// <summary>
/// Guards against Server-Side Request Forgery (SSRF) attacks.
/// Validates URLs before any outbound HTTP request to ensure they do not target
/// private networks, localhost, cloud metadata endpoints, or other restricted resources.
/// Validation is performed AFTER DNS resolution to defeat DNS rebinding attacks.
/// </summary>
public static class SsrfGuard
{
    // RFC 1918 private ranges + loopback + link-local + cloud metadata
    private static readonly (IPAddress Network, int PrefixLength)[] BlockedRanges =
    [
        (IPAddress.Parse("127.0.0.0"),   8),   // Loopback
        (IPAddress.Parse("10.0.0.0"),    8),   // RFC 1918
        (IPAddress.Parse("172.16.0.0"),  12),  // RFC 1918
        (IPAddress.Parse("192.168.0.0"), 16),  // RFC 1918
        (IPAddress.Parse("169.254.0.0"), 16),  // Link-local (AWS metadata 169.254.169.254)
        (IPAddress.Parse("100.64.0.0"),  10),  // Shared address space (RFC 6598)
        (IPAddress.Parse("::1"),         128), // IPv6 loopback
        (IPAddress.Parse("fc00::"),      7),   // IPv6 unique local
        (IPAddress.Parse("fe80::"),      10),  // IPv6 link-local
    ];

    private static readonly string[] AllowedSchemes = ["http", "https"];

    /// <summary>
    /// Validates a URL for SSRF safety. Throws <see cref="InvalidOperationException"/>
    /// if the URL targets a private/restricted address.
    /// </summary>
    public static async Task ValidateAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"Invalid URL: {url}");

        if (!AllowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"URL scheme '{uri.Scheme}' is not allowed. Only HTTP/HTTPS permitted.");

        // Block numeric IP literals before DNS resolution
        if (IPAddress.TryParse(uri.Host, out var literalIp))
        {
            if (IsBlockedIp(literalIp))
                throw new InvalidOperationException($"URL targets a restricted IP address: {literalIp}");
            return;
        }

        // Resolve DNS and validate all returned addresses
        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.Host, ct);
        }
        catch (SocketException ex)
        {
            throw new InvalidOperationException($"DNS resolution failed for '{uri.Host}': {ex.Message}");
        }

        if (addresses.Length == 0)
            throw new InvalidOperationException($"DNS returned no addresses for '{uri.Host}'.");

        foreach (var ip in addresses)
        {
            if (IsBlockedIp(ip))
                throw new InvalidOperationException(
                    $"URL '{uri.Host}' resolves to restricted address {ip}. SSRF blocked.");
        }
    }

    /// <summary>Validates an already-resolved IP (used for redirect chain validation).</summary>
    public static void ValidateIp(IPAddress ip)
    {
        if (IsBlockedIp(ip))
            throw new InvalidOperationException($"Redirect targets restricted IP {ip}. SSRF blocked.");
    }

    private static bool IsBlockedIp(IPAddress ip)
    {
        // Normalize IPv4-mapped IPv6 addresses
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        foreach (var (network, prefix) in BlockedRanges)
        {
            if (network.AddressFamily != ip.AddressFamily) continue;
            if (IsInRange(ip, network, prefix)) return true;
        }

        return false;
    }

    private static bool IsInRange(IPAddress ip, IPAddress network, int prefixLength)
    {
        var ipBytes = ip.GetAddressBytes();
        var netBytes = network.GetAddressBytes();
        if (ipBytes.Length != netBytes.Length) return false;

        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < fullBytes; i++)
            if (ipBytes[i] != netBytes[i]) return false;

        if (remainingBits > 0)
        {
            var mask = (byte)(0xFF << (8 - remainingBits));
            if ((ipBytes[fullBytes] & mask) != (netBytes[fullBytes] & mask)) return false;
        }

        return true;
    }
}
