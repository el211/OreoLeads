using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Analysis;

/// <summary>
/// Calcule un "score d'opportunité commerciale" entre 0 et 100.
/// Plus le score est élevé, plus Oreo Studios a de valeur ajoutée à proposer.
/// </summary>
public static class BusinessScoringService
{
    public static int Calculate(WebsiteAnalysis a)
    {
        var score = 0;

        // Fonctionnalités manquantes (opportunités de vente)
        if (!a.HasContactForm)    score += 20;  // gros gain d'UX
        if (!a.HasQuoteForm)      score += 15;  // perte de business direct
        if (!a.HasBookingSystem)  score += 15;  // selon secteur
        if (a.ResponseTimeMs > 3000) score += 10; // site lent
        if (!a.HasViewport)       score += 10;  // pas mobile-friendly
        if (string.IsNullOrWhiteSpace(a.MetaDescription)) score += 10; // SEO manquant
        if (!a.UsesHttps)         score +=  5;  // sécurité basique
        if (!a.CertificateValid)  score +=  5;  // certificat invalide
        if (!a.HasPrivacyPolicy)  score +=  3;  // conformité RGPD
        if (!a.HasLegalNotice)    score +=  3;  // obligation légale FR

        // Bonus (information disponible pour prospection)
        if (a.HasEmailVisible)    score +=  3;
        if (a.HasPhoneVisible)    score +=  1;

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>Retourne la liste des opportunités identifiées (textes affichables).</summary>
    public static List<string> GetOpportunities(WebsiteAnalysis a)
    {
        var list = new List<string>();

        if (!a.HasContactForm)    list.Add("Aucun formulaire de contact");
        if (!a.HasQuoteForm)      list.Add("Aucun formulaire de devis");
        if (!a.HasBookingSystem)  list.Add("Aucun système de réservation");
        if (a.ResponseTimeMs > 3000) list.Add($"Site lent ({a.ResponseTimeMs} ms)");
        if (!a.HasViewport)       list.Add("Site non responsive (pas de viewport mobile)");
        if (string.IsNullOrWhiteSpace(a.MetaDescription)) list.Add("Aucune meta description (SEO)");
        if (!a.UsesHttps)         list.Add("Pas de HTTPS");
        if (!a.CertificateValid && a.UsesHttps) list.Add("Certificat SSL invalide");
        if (!a.HasPrivacyPolicy)  list.Add("Politique de confidentialité absente");
        if (!a.HasLegalNotice)    list.Add("Mentions légales absentes");
        if (!string.IsNullOrWhiteSpace(a.CmsDetected))
            list.Add($"CMS détecté : {a.CmsDetected}");
        if (a.HasEmailVisible)    list.Add("Email professionnel accessible (contact direct possible)");

        return list;
    }
}
