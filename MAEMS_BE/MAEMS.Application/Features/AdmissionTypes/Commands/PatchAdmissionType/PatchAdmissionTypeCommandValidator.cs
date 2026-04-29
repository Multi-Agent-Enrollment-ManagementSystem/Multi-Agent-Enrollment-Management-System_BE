//using FluentValidation;

//namespace MAEMS.Application.Features.AdmissionTypes.Commands.PatchAdmissionType;

//public class PatchAdmissionTypeCommandValidator : AbstractValidator<PatchAdmissionTypeCommand>
//{
//    public PatchAdmissionTypeCommandValidator()
//    {
//        RuleFor(x => x.AdmissionTypeId)
//            .GreaterThan(0).WithMessage("Admission type ID must be greater than 0");

//        RuleFor(x => x.AdmissionTypeName)
//            .Length(3, 200).When(x => !string.IsNullOrEmpty(x.AdmissionTypeName))
//            .WithMessage("Name must be between 3 and 200 characters");

//        RuleFor(x => x.Type)
//            .NotEmpty().WithMessage("Admission type is required")
//            .Length(3, 200).WithMessage("Type must be between 3 and 200 characters");

//        RuleFor(x => x.RequiredDocumentList)
//            .Must(BeValidJson).When(x => !string.IsNullOrEmpty(x.RequiredDocumentList))
//            .WithMessage("Required document list must be valid JSON format");
//    }

//    private bool BeValidJson(string? json)
//    {
//        if (string.IsNullOrWhiteSpace(json)) return true;

//        try
//        {
//            System.Text.Json.JsonDocument.Parse(json);
//            return true;
//        }
//        catch
//        {
//            return false;
//        }
//    }
//}
