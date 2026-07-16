using FluentValidation;
using OreoLeads.Application.Features.Leads.DTOs;

namespace OreoLeads.Application.Features.Leads.Validators;

public class CreateLeadValidator : AbstractValidator<CreateLeadDto>
{
    public CreateLeadValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Le nom de l'entreprise est obligatoire.")
            .MaximumLength(200).WithMessage("Le nom de l'entreprise ne peut pas dépasser 200 caractères.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("L'adresse email n'est pas valide.")
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^[\d\s\+\-\(\)\.]{7,20}$").WithMessage("Le numéro de téléphone n'est pas valide.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Website)
            .Must(BeAValidUrl).WithMessage("L'URL du site web n'est pas valide.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x.Score)
            .InclusiveBetween(0, 100).WithMessage("Le score doit être compris entre 0 et 100.");

        RuleFor(x => x.Country)
            .MaximumLength(100);

        RuleFor(x => x.City)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.City));
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
