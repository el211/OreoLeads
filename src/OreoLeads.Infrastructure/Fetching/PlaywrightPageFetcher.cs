using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Infrastructure.Fetching;

/// <summary>
/// Récupération de page avec rendu JavaScript via Playwright/Chromium.
/// Singleton : le navigateur est lancé une fois, chaque fetch ouvre un contexte
/// isolé. Si Chromium est indisponible (non installé), se marque indisponible
/// pour que le CompositePageFetcher bascule sur HTTP.
/// </summary>
public sealed class PlaywrightPageFetcher : IPageFetcher, IAsyncDisposable
{
    private readonly PlaywrightSettings _settings;
    private readonly ILogger<PlaywrightPageFetcher> _logger;
    private readonly SemaphoreSlim _pageThrottle;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _unavailable;

    public PlaywrightPageFetcher(IOptions<PlaywrightSettings> settings, ILogger<PlaywrightPageFetcher> logger)
    {
        _settings = settings.Value;
        _logger   = logger;
        _pageThrottle = new SemaphoreSlim(Math.Max(1, _settings.MaxConcurrentPages));
    }

    public bool IsAvailable => _settings.Enabled && !_unavailable;

    public async Task<PageFetchResult> FetchAsync(string url, CancellationToken ct = default)
    {
        var browser = await EnsureBrowserAsync(ct);
        if (browser is null)
            return new PageFetchResult(null, url, 0, 0, 0, false, false, "Playwright indisponible");

        await _pageThrottle.WaitAsync(ct);
        var sw = Stopwatch.StartNew();
        IBrowserContext? context = null;
        try
        {
            context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "fr-FR",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                            "(KHTML, like Gecko) Chrome/120.0 Safari/537.36",
                IgnoreHTTPSErrors = true,
            });

            // On coupe images/polices/médias pour accélérer le rendu
            await context.RouteAsync("**/*", async route =>
            {
                var type = route.Request.ResourceType;
                if (type is "image" or "font" or "media")
                    await route.AbortAsync();
                else
                    await route.ContinueAsync();
            });

            var page = await context.NewPageAsync();
            var timeout = _settings.NavigationTimeoutSeconds * 1000;

            IResponse? response;
            try
            {
                response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = timeout,
                });
            }
            catch (TimeoutException)
            {
                // NetworkIdle jamais atteint (widgets qui pollent) → on se contente du DOM chargé
                response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.Load,
                    Timeout = timeout,
                });
            }

            if (_settings.PostLoadDelayMs > 0)
                await page.WaitForTimeoutAsync(_settings.PostLoadDelayMs);

            var html = await page.ContentAsync();
            var finalUrl = page.Url;
            sw.Stop();

            return new PageFetchResult(
                Html: html,
                FinalUrl: finalUrl,
                StatusCode: response?.Status ?? 200,
                ResponseTimeMs: (int)sw.ElapsedMilliseconds,
                RedirectCount: string.Equals(url, finalUrl, StringComparison.OrdinalIgnoreCase) ? 0 : 1,
                CertificateValid: url.StartsWith("https://", StringComparison.OrdinalIgnoreCase),
                UsedBrowser: true,
                Error: null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Playwright fetch échoué pour {Url}", url);
            return new PageFetchResult(null, url, 0, (int)sw.ElapsedMilliseconds, 0, false, true, ex.Message);
        }
        finally
        {
            if (context is not null) await context.CloseAsync();
            _pageThrottle.Release();
        }
    }

    private async Task<IBrowser?> EnsureBrowserAsync(CancellationToken ct)
    {
        if (_browser is not null) return _browser;
        if (_unavailable || !_settings.Enabled) return null;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_browser is not null) return _browser;
            if (_unavailable) return null;

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-dev-shm-usage"],
            });
            _logger.LogInformation("Chromium (Playwright) lancé.");
            return _browser;
        }
        catch (Exception ex)
        {
            _unavailable = true;
            _logger.LogWarning(ex,
                "Chromium indisponible — bascule sur le fetch HTTP. " +
                "Installez le navigateur (playwright install chromium) pour activer le rendu JavaScript.");
            return null;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}
