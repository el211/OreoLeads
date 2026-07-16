using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Analysis;

/// <summary>
/// Propose des services Oreo Studios adaptés au secteur du prospect et à l'analyse de son site.
/// </summary>
public static class BusinessRecommendationService
{
    public static List<string> GetOreoServices(WebsiteAnalysis a, string? industry)
    {
        var services = new List<string>();
        var ind = (industry ?? string.Empty).ToLowerInvariant();

        // ── Recommandations sectorielles ─────────────────────────────────────

        if (IsIndustry(ind, "restaurant", "brasserie", "café", "café", "hôtel", "bar",
                            "traiteur", "boulangerie", "pâtisserie", "pizzeria", "sushi"))
        {
            if (!a.HasBookingSystem) services.Add("Réservation en ligne");
            services.Add("Refonte du site vitrine");
            services.Add("Gestion d'événements");
            services.Add("Programme de fidélisation client");
        }
        else if (IsIndustry(ind, "garage", "automobile", "auto", "mécanique",
                                 "carrosserie", "pneumatique"))
        {
            if (!a.HasBookingSystem) services.Add("Prise de rendez-vous en ligne");
            if (!a.HasQuoteForm)    services.Add("Module de devis en ligne");
            services.Add("Tableau de bord de suivi client");
            services.Add("Application mobile pour les techniciens");
        }
        else if (IsIndustry(ind, "immobilier", "agence immobilière", "promoteur",
                                 "gestion locative", "biens"))
        {
            services.Add("Module d'estimation en ligne");
            services.Add("CRM immobilier");
            services.Add("Gestion et qualification des leads");
            services.Add("Alertes automatiques de nouveaux biens");
        }
        else if (IsIndustry(ind, "artisan", "plombier", "électricien", "menuisier",
                                 "peintre", "maçon", "couvreur", "carreleur", "btp",
                                 "construction", "rénovation", "travaux"))
        {
            if (!a.HasQuoteForm) services.Add("Module de devis en ligne");
            services.Add("Site vitrine professionnel moderne");
            services.Add("Référencement local (SEO)");
            services.Add("Gestion planning et interventions");
        }
        else if (IsIndustry(ind, "médecin", "dentiste", "kiné", "ostéopathe",
                                 "psychologue", "infirmier", "paramédical", "santé", "cabinet"))
        {
            if (!a.HasBookingSystem) services.Add("Prise de rendez-vous en ligne (Doctolib-like)");
            services.Add("Site RGPD conforme");
            services.Add("Gestion du dossier patient simplifié");
        }
        else if (IsIndustry(ind, "coiffeur", "salon", "esthétique", "beauté",
                                 "nail", "spa", "massage", "bien-être"))
        {
            if (!a.HasBookingSystem) services.Add("Réservation en ligne");
            services.Add("Application de fidélisation");
            services.Add("Gestion des plannings et équipes");
        }
        else if (IsIndustry(ind, "e-commerce", "boutique", "vente en ligne",
                                 "commerce", "magasin"))
        {
            services.Add("Optimisation de la boutique en ligne");
            services.Add("Automatisation marketing");
            services.Add("Tableau de bord des ventes");
        }
        else
        {
            // PME générique
            if (!a.HasContactForm)   services.Add("Formulaire de contact optimisé");
            if (!a.HasQuoteForm)     services.Add("Module de devis en ligne");
            services.Add("Logiciel métier sur mesure");
            services.Add("CRM de prospection");
            services.Add("Automatisation des processus");
        }

        // ── Recommandations transversales basées sur l'analyse ──────────────

        if (!a.HasViewport)       services.Add("Mise en responsive (mobile-first)");
        if (!a.UsesHttps)         services.Add("Migration HTTPS + certificat SSL");
        if (a.ResponseTimeMs > 3000) services.Add("Optimisation des performances");
        if (string.IsNullOrWhiteSpace(a.MetaDescription)) services.Add("Optimisation SEO de base");
        if (!a.HasPrivacyPolicy || !a.HasLegalNotice) services.Add("Mise en conformité RGPD");

        return services.Distinct().Take(8).ToList();
    }

    private static bool IsIndustry(string industry, params string[] keywords)
        => keywords.Any(k => industry.Contains(k, StringComparison.OrdinalIgnoreCase));
}
