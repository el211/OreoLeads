using System.Text.RegularExpressions;

namespace OreoLeads.Infrastructure.Analysis;

/// <summary>
/// Détecte les technologies web à partir du HTML brut et des en-têtes HTTP.
/// Ne prétend jamais détecter une techno sans signature vérifiable dans le HTML.
/// </summary>
public static class TechnologyDetector
{
    // Chaque entrée : (nom affiché, pattern regex sur le HTML)
    private static readonly (string Name, string Pattern)[] Signatures =
    {
        // CMS
        ("WordPress",   @"wp-content/|wp-includes/|/wp-json/|wordpress"),
        ("WooCommerce", @"woocommerce|wc-cart|wc-add-to-cart"),
        ("Shopify",     @"cdn\.shopify\.com|shopify\.com/s/|Shopify\.theme"),
        ("PrestaShop",  @"prestashop|/modules/blockwishlist|presta-"),
        ("Drupal",      @"drupal\.js|Drupal\.settings|/sites/default/files"),
        ("Joomla",      @"/components/com_|Joomla!|joomla\.js"),
        ("Wix",         @"wix\.com|wixstatic\.com|wix-code"),
        ("Squarespace", @"squarespace\.com|static1\.squarespace"),

        // Frameworks / langages
        ("ASP.NET",     @"__VIEWSTATE|__EventValidation|\.aspx|asp\.net"),
        ("React",       @"data-reactroot|__REACT_DEVTOOLS|react\.production\.min\.js|/static/js/main\.[a-f0-9]+\.chunk"),
        ("Vue.js",      @"vue\.js|vue\.min\.js|__vue_app__|data-v-[a-f0-9]+"),
        ("Angular",     @"ng-version|angular\.js|ng-app=|angular\.min\.js"),

        // CSS / JS libs
        ("Bootstrap",   @"bootstrap\.min\.css|bootstrap\.min\.js|bootstrap@"),
        ("jQuery",      @"jquery\.min\.js|jquery-\d|jquery\.js"),
        ("Tailwind",    @"tailwindcss|tailwind\.min\.css|tw-[a-z]"),
    };

    /// <summary>Retourne la liste de toutes les technologies détectées.</summary>
    public static List<string> Detect(string html)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(html)) return result;

        foreach (var (name, pattern) in Signatures)
        {
            if (Regex.IsMatch(html, pattern, RegexOptions.IgnoreCase))
                result.Add(name);
        }

        return result;
    }

    /// <summary>Retourne uniquement le CMS détecté (null si aucun ou ambigu).</summary>
    public static string? DetectCms(string html)
    {
        if (string.IsNullOrEmpty(html)) return null;

        // Ordre de priorité : CMS les plus spécifiques d'abord
        (string Name, string Pattern)[] cmsList =
        {
            ("WooCommerce", @"woocommerce|wc-cart"),
            ("WordPress",   @"wp-content/|wp-includes/|/wp-json/"),
            ("Shopify",     @"cdn\.shopify\.com|shopify\.com/s/"),
            ("PrestaShop",  @"prestashop|/modules/blockwishlist"),
            ("Drupal",      @"drupal\.js|Drupal\.settings"),
            ("Joomla",      @"/components/com_|Joomla!"),
            ("Wix",         @"wixstatic\.com|wix-code"),
            ("Squarespace", @"squarespace\.com"),
        };

        foreach (var (name, pattern) in cmsList)
            if (Regex.IsMatch(html, pattern, RegexOptions.IgnoreCase))
                return name;

        return null;
    }
}
