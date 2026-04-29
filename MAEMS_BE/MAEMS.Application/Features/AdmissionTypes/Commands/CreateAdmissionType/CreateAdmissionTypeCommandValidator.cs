//using FluentValidation;

//namespace MAEMS.Application.Features.AdmissionTypes.Commands.CreateAdmissionType;

//public class CreateAdmissionTypeCommandValidator : AbstractValidator<CreateAdmissionTypeCommand>
//{
//    public CreateAdmissionTypeCommandValidator()
//    {
//        RuleFor(x => x.AdmissionTypeName)
//            .NotEmpty().WithMessage("Admission type name is required")
//            .Length(3, 200).WithMessage("Name must be between 3 and 200 characters");

//        RuleFor(x => x.EnrollmentYearId)
//            .GreaterThan(0).When(x => x.EnrollmentYearId.HasValue)
//            .WithMessage("Enrollment year ID must be greater than 0 if provided");

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
