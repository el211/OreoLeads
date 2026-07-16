using FluentValidation;
using OreoLeads.Application.Features.LeadNotes.DTOs;

namespace OreoLeads.Application.Features.LeadNotes.Validators;

public class CreateLeadNoteValidator : AbstractValidator<CreateLeadNoteDto>
{
    public CreateLeadNoteValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Le titre est obligatoire.")
            .MaximumLength(200);

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Le contenu est obligatoire.")
            .MaximumLength(10000);
    }
}

public class UpdateLeadNoteValidator : AbstractValidator<UpdateLeadNoteDto>
{
    public UpdateLeadNoteValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Le titre est obligatoire.")
            .MaximumLength(200);

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Le contenu est obligatoire.")
            .MaximumLength(10000);
    }
}
