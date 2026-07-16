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
            // Champ email (type="email" ou name="email")
            if (Regex.IsMatch(content, @"type\s*=\s*[""']email[""']", RegexOptions.IgnoreCase))
                return true;
            // Champ message / sujet / contact explicite
            if (Regex.IsMatch(content,
                @"name\s*=\s*[""'](message|sujet|subject|contact|commentaire|texte)[""']",
                RegexOptions.IgnoreCase))
                return true;
            // Textarea de contact
            if (Regex.IsMatch(content,
                @"placeholder\s*=\s*[""'][^""']*(message|votre\s+message|commentaire)[^""']*[""']",
                RegexOptions.IgnoreCase))
                return true;
        }
        return false;
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
