using FluentValidation;
using MkWMS.API.DTOs;

public class CreateDocumentDtoValidator : AbstractValidator<CreateDocumentDto>
{
    public CreateDocumentDtoValidator()
    {
        RuleFor(x => x.Number).MaximumLength(50);
        RuleFor(x => x.DocumentTypeId).GreaterThan(0);
        RuleFor(x => x.WarehouseId).GreaterThan(0);


        RuleFor(x => x.BaseDocumentId)
            .GreaterThan(0)
            .When(x => x.BaseDocumentId.HasValue);

        RuleForEach(x => x.Items).SetValidator(new CreateDocumentItemDtoValidator());
    }
}

public class CreateDocumentItemDtoValidator : AbstractValidator<CreateDocumentItemDto>
{
    public CreateDocumentItemDtoValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}