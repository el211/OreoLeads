using System.Text.RegularExpressions;

namespace OreoLeads.Infrastructure.Analysis;

/// <summary>
/// Analyse le HTML brut d'une page publique avec des expressions régulières.
/// Méthodes pures — pas de dépendance externe, faciles à tester.
/// </summary>
public static class HtmlAnalyzer
{
    public static string? ExtractTitle(string html)
    {
        var m = Regex.Match(html, @"<title[^>]*>\s*(.*?)\s*</title>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return m.Success ? CleanText(m.Groups[1].Value) : null;
    }

    public static string? ExtractMetaDescription(string html)
    {
        // name="description" content="…"
        var m = Regex.Match(html,
            @"<meta[^>]+name\s*=\s*[""']description[""'][^>]+content\s*=\s*[""']([^""']{0,500})[""']",
            RegexOptions.IgnoreCase);
        if (m.Success) return CleanText(m.Groups[1].Value);

        // content="…" name="description"
        m = Regex.Match(html,
            @"<meta[^>]+content\s*=\s*[""']([^""']{0,500})[""'][^>]+name\s*=\s*[""']description[""']",
            RegexOptions.IgnoreCase);
        return m.Success ? CleanText(m.Groups[1].Value) : null;
    }

    public static bool HasViewport(string html)
        => Regex.IsMatch(html, @"<meta[^>]+name\s*=\s*[""']viewport[""']", RegexOptions.IgnoreCase);

    public static bool HasContactForm(string html)
    {
        // Un formulaire contenant un champ email ou message/sujet/contact
        var forms = Regex.Matches(html, @"<form[^>]*>(.*?)</form>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        foreach (Match form in forms)
        {
            var content = form.Groups[1].Value;

            // Champ message / sujet / contact explicite → formulaire de contact certain
            var hasMessageField =
                Regex.IsMatch(content,
                    @"name\s*=\s*[""'](message|sujet|subject|contact|commentaire|texte)[""']",
                    RegexOptions.IgnoreCase)
                || Regex.IsMatch(content, @"<textarea", RegexOptions.IgnoreCase)
                || Regex.IsMatch(content,
                    @"placeholder\s*=\s*[""'][^""']*(message|votre\s+message|commentaire)[^""']*[""']",
                    RegexOptions.IgnoreCase);
            if (hasMessageField)
                return true;

            // Champ email seul : compte comme contact SAUF si c'est une newsletter
            if (Regex.IsMatch(content, @"type\s*=\s*[""']email[""']", RegexOptions.IgnoreCase)
                && !IsNewsletterMarkup(content))
                return true;
        }

        // Service de formulaire embarqué (iframe/script) : Typeform, Google Forms, etc.
        if (HasEmbeddedFormService(html))
            return true;

        // Marqueurs de plugins de formulaire (WordPress & co) dont le rendu
        // ne suit pas toujours le schéma <form>…</form> détecté ci-dessus
        if (Regex.IsMatch(html,
            @"wpcf7|contact-form-7|wpforms|gform_wrapper|gravityform|elementor-form|ninja-forms|forminator|fluentform|hs-form",
            RegexOptions.IgnoreCase))
            return true;

        // Fallback : champ email + textarea présents sur la page même si le
        // couple <form>…</form> n'a pas pu être apparié (HTML mal formé, JS)
        return Regex.IsMatch(html, @"type\s*=\s*[""']email[""']", RegexOptions.IgnoreCase)
            && Regex.IsMatch(html, @"<textarea", RegexOptions.IgnoreCase);
    }

    public static bool HasEmbeddedFormService(string html)
        => Regex.IsMatch(html,
            @"typeform\.com|docs\.google\.com/forms|forms\.gle|jotform|tally\.so|hsforms\.net|forms\.hubspot|formspree\.io|web3forms|getform\.io|cognitoforms|wufoo|framaforms|forms\.office\.com",
            RegexOptions.IgnoreCase);

    private static readonly Regex NewsletterRegex = new(
        @"newsletter|infolettre|abonnez[-\s]?vous|inscrivez[-\s]?vous\s+à\s+(notre|la)|s['’]abonner|subscribe|restez\s+informé",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool IsNewsletterMarkup(string markup) => NewsletterRegex.IsMatch(markup);

    /// <summary>
    /// Formulaire de newsletter : un champ email dans un contexte d'abonnement,
    /// sans champ message. À distinguer d'un formulaire de contact.
    /// </summary>
    public static bool HasNewsletterForm(string html)
    {
        var forms = Regex.Matches(html, @"<form[^>]*>(.*?)</form>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        foreach (Match form in forms)
        {
            var content = form.Groups[1].Value;
            var hasEmail = Regex.IsMatch(content, @"type\s*=\s*[""']email[""']", RegexOptions.IgnoreCase);
            var hasMessage = Regex.IsMatch(content, @"<textarea", RegexOptions.IgnoreCase)
                || Regex.IsMatch(content, @"name\s*=\s*[""'](message|sujet|subject)[""']", RegexOptions.IgnoreCase);
            if (hasEmail && !hasMessage && IsNewsletterMarkup(content))
                return true;
        }
        // Widgets d'emailing sans balise <form> détectable
        return Regex.IsMatch(html, @"mailchimp|mc4wp|sendinblue|mailerlite|sib-form|klaviyo",
            RegexOptions.IgnoreCase) && IsNewsletterMarkup(html);
    }

    /// <summary>Nom du service de réservation en ligne détecté, ou null.</summary>
    public static string? DetectBookingProvider(string html)
    {
        (string Pattern, string Name)[] providers =
        [
            (@"planity\.com|planity", "Planity"),
            (@"treatwell", "Treatwell"),
            (@"calendly", "Calendly"),
            (@"doctolib", "Doctolib"),
            (@"fresha\.com|fresha", "Fresha"),
            (@"zenchef", "Zenchef"),
            (@"thefork|lafourchette", "TheFork"),
            (@"simplybook", "SimplyBook"),
            (@"guestonline", "GuestOnline"),
            (@"resengo", "Resengo"),
        ];
        foreach (var (pattern, name) in providers)
            if (Regex.IsMatch(html, pattern, RegexOptions.IgnoreCase))
                return name;
        return null;
    }

    public static bool HasWhatsAppLink(string html)
        => Regex.IsMatch(html, @"wa\.me/|api\.whatsapp\.com|whatsapp://|href\s*=\s*[""'][^""']*whatsapp",
            RegexOptions.IgnoreCase);

    public static bool HasMessengerLink(string html)
        => Regex.IsMatch(html, @"m\.me/|messenger\.com/t/|fb-messenger://|facebook\.com/messages",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Extrait les URLs internes de pages contact/devis/rendez-vous à analyser
    /// en plus de la page d'accueil (même hôte uniquement).
    /// </summary>
    public static List<string> FindContactPageUrls(string html, Uri baseUri)
        => FindInternalUrls(html, baseUri,
            @"contact|nous-joindre|rendez-vous|reservation|réservation|devis|booking|appointment");

    /// <summary>
    /// URLs internes des pages mentions légales / à-propos / confidentialité —
    /// pages où figurent souvent e-mail, SIREN et coordonnées.
    /// </summary>
    public static List<string> FindLegalPageUrls(string html, Uri baseUri)
        => FindInternalUrls(html, baseUri,
            @"mentions-legales|mentions_legales|mentions|legal|a-propos|apropos|about|equipe|team|politique-de-confidentialite|confidentialite|privacy|cgv");

    private static List<string> FindInternalUrls(string html, Uri baseUri, string pathPattern)
    {
        var urls = new List<string>();
        var links = Regex.Matches(html, @"<a[^>]+href\s*=\s*[""']([^""'#]+)[""']",
            RegexOptions.IgnoreCase);

        foreach (Match link in links)
        {
            var href = link.Groups[1].Value.Trim();
            if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
             || href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
             || href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!Uri.TryCreate(baseUri, href, out var absolute)) continue;
            if (!string.Equals(absolute.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase)) continue;

            if (Regex.IsMatch(absolute.AbsolutePath, pathPattern, RegexOptions.IgnoreCase))
            {
                var url = absolute.GetLeftPart(UriPartial.Path);
                if (!urls.Contains(url, StringComparer.OrdinalIgnoreCase))
                    urls.Add(url);
            }
        }
        return urls;
    }

    public static bool HasQuoteForm(string html)
        => Regex.IsMatch(html,
            @"devis|demande\s+de\s+devis|quote|estimate|tarif|chiffrage",
            RegexOptions.IgnoreCase);

    public static bool HasBookingSystem(string html)
        => Regex.IsMatch(html,
            @"réservation|réserver|booking|book\s+(a\s+table|now|online)|calendly|doctolib|planity|booker",
            RegexOptions.IgnoreCase);

    public static bool HasChatWidget(string html)
        => Regex.IsMatch(html,
            @"intercom|crisp|drift|tawk|zendesk|livechat|freshchat|hubspot|_hsq\s*=|__lc|smartsupp",
            RegexOptions.IgnoreCase);

    public static bool HasEmailVisible(string html)
    {
        // Email en clair OU lien mailto: (exclut les variables JS type {email})
        return Regex.IsMatch(html, @"mailto:[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.IgnoreCase)
            || Regex.IsMatch(html, @"\b[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}\b(?!\s*[}""'])");
    }

    public static bool HasPhoneVisible(string html)
        // Numéros FR : +33 ou 0[1-9] suivi de 8 chiffres (avec séparateurs)
        => Regex.IsMatch(html, @"(\+33|0[1-9])[\s.\-]?(\d{2}[\s.\-]?){4}", RegexOptions.IgnoreCase)
        || Regex.IsMatch(html, @"tel:[0-9+\s\-.()+]{7,}", RegexOptions.IgnoreCase);

    public static bool HasAddressVisible(string html)
        // Code postal FR (5 chiffres) visible
        => Regex.IsMatch(html, @"\b\d{5}\b", RegexOptions.IgnoreCase);

    public static bool HasPrivacyPolicy(string html)
        => Regex.IsMatch(html,
            @"politique\s+de\s+confidentialit|privacy\s+policy|données\s+personnelles|rgpd|gdpr",
            RegexOptions.IgnoreCase);

    public static bool HasLegalNotice(string html)
        => Regex.IsMatch(html,
            @"mentions?\s+légales?|legal\s+notice|informations?\s+légales?",
            RegexOptions.IgnoreCase);

    private static string CleanText(string s)
        => Regex.Replace(s.Trim(), @"\s+", " ");
}
