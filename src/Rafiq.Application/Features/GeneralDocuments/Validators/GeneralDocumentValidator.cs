using FluentValidation;

namespace Rafiq.Application.Features.GeneralDocuments.Commands.UploadGeneralDocument;

public sealed class UploadGeneralDocumentCommandValidator
    : AbstractValidator<UploadGeneralDocumentCommand>
{
    public UploadGeneralDocumentCommandValidator()
    {
        RuleFor(x => x.Image)
            .NotNull();

        //RuleFor(x => x.Title)
        //    .NotEmpty()
        //    .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(3000);
    }
}