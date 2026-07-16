using FluentAssertions;
using OreoLeads.Infrastructure.Analysis;

namespace OreoLeads.Tests.Analysis;

public class TechnologyDetectorTests
{
    // ── Detect ────────────────────────────────────────────────────────────────

    [Fact]
    public void Detect_WordPress_WhenWpContentPresent()
    {
        var html = @"<link rel=""stylesheet"" href=""/wp-content/themes/mytheme/style.css"" />";
        var result = TechnologyDetector.Detect(html);
        result.Should().Contain("WordPress");
    }

    [Fact]
    public void Detect_WooCommerce_WhenWooCommerceClass()
    {
        var html = @"<div class=""woocommerce""><button class=""wc-add-to-cart"">Add</button></div>";
        var result = TechnologyDetector.Detect(html);
        result.Should().Contain("WooCommerce");
    }

    [Fact]
    public void Detect_Shopify_WhenShopifyCdn()
    {
        var html = @"<script src=""https://cdn.shopify.com/s/files/1/script.js""></script>";
        var result = TechnologyDetector.Detect(html);
        result.Should().Contain("Shopify");
    }

    [Fact]
    public void Detect_Bootstrap_WhenBootstrapCss()
    {
        var html = @"<link rel=""stylesheet"" href=""/css/bootstrap.min.css"" />";
        var result = TechnologyDetector.Detect(html);
        result.Should().Contain("Bootstrap");
    }

    [Fact]
    public void Detect_React_WhenDataReactRoot()
    {
        var html = @"<div id=""root"" data-reactroot=""""></div>";
        var result = TechnologyDetector.Detect(html);
        result.Should().Contain("React");
    }

    [Fact]
    public void Detect_jQuery_WhenJQueryMinJs()
    {
        var html = @"<script src=""/js/jquery.min.js""></script>";
        var result = TechnologyDetector.Detect(html);
        result.Should().Contain("jQuery");
    }

    [Fact]
    public void Detect_VueJs_WhenVueApp()
    {
        var html = @"<div id=""app"" data-v-123abc></div><script src=""/js/vue.min.js""></script>";
        var result = TechnologyDetector.Detect(html);
        result.Should().Contain("Vue.js");
    }

    [Fact]
    public void Detect_Angular_WhenNgVersion()
    {
        var html = @"<app-root ng-version=""15.0.0""></app-root>";
        var result = TechnologyDetector.Detect(html);
        result.Should().Contain("Angular");
    }

    [Fact]
    public void Detect_Multiple_WhenWordPressAndBootstrap()
    {
        var html = @"<link href=""/wp-content/themes/t.css"" /><link href=""bootstrap.min.css"" />";
        var result = TechnologyDetector.Detect(html);
        result.Should().Contain("WordPress").And.Contain("Bootstrap");
    }

    [Fact]
    public void Detect_Empty_WhenHtmlIsEmpty()
    {
        var result = TechnologyDetector.Detect(string.Empty);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Detect_NoFalsePositive_WhenHtmlHasNoSignatures()
    {
        var html = @"<html><head><title>Simple site</title></head><body><p>Hello world</p></body></html>";
        var result = TechnologyDetector.Detect(html);
        result.Should().BeEmpty();
    }

    // ── DetectCms ─────────────────────────────────────────────────────────────

    [Fact]
    public void DetectCms_WordPress_WhenWpIncludes()
    {
        var html = @"<script src=""/wp-includes/js/jquery.js""></script>";
        TechnologyDetector.DetectCms(html).Should().Be("WordPress");
    }

    [Fact]
    public void DetectCms_WooCommerce_HasPriorityOverWordPress()
    {
        var html = @"<link href=""/wp-content/plugins/woocommerce/style.css"" />
                     <div class=""woocommerce""></div>";
        TechnologyDetector.DetectCms(html).Should().Be("WooCommerce");
    }

    [Fact]
    public void DetectCms_Null_WhenNoKnownCms()
    {
        var html = @"<html><body>Custom site</body></html>";
        TechnologyDetector.DetectCms(html).Should().BeNull();
    }

    [Fact]
    public void DetectCms_Wix_WhenWixStatic()
    {
        var html = @"<link href=""https://static.wixstatic.com/media/img.png"" />";
        TechnologyDetector.DetectCms(html).Should().Be("Wix");
    }
}
