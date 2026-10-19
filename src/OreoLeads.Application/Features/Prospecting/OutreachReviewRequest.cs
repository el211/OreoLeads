using System.ComponentModel.DataAnnotations;

namespace OreoLeads.Application.Features.Prospecting;

public class OutreachReviewRequest
{
    [Required, RegularExpression("^[A-Z]{2}$")] public string CountryCode { get; set; } = "FR";
    [Required, RegularExpression("^(Consent|ProfessionalLegitimateInterest)$")] public string Basis { get; set; } = "Consent";
    [Required, StringLength(2000, MinimumLength = 20)] public string BasisEvidence { get; set; } = "";
    [Required, StringLength(500, MinimumLength = 10)] public string SenderIdentification { get; set; } = "";
    [Required, Url, StringLength(1000)] public string PrivacyNoticeUrl { get; set; } = "";
    public bool ProfessionRelevant { get; set; }
    public bool PrivacyInformationProvided { get; set; }
    [Range(1, 90)] public int ValidDays { get; set; } = 30;
}
