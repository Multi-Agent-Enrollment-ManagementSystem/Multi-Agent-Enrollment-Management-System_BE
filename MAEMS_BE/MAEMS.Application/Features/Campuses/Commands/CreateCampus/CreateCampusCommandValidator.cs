//using FluentValidation;

//namespace MAEMS.Application.Features.Campuses.Commands.CreateCampus;

//public class CreateCampusCommandValidator : AbstractValidator<CreateCampusCommand>
//{
//    public CreateCampusCommandValidator()
//    {
//        RuleFor(x => x.Name)
//            .NotEmpty().WithMessage("Campus name is required")
//            .Length(3, 200).WithMessage("Name must be between 3 and 200 characters");

//        RuleFor(x => x.Email)
//            .NotEmpty().WithMessage("Email is required")
//            .EmailAddress().WithMessage("Invalid email format")
//            .MaximumLength(100).WithMessage("Email must not exceed 100 characters");

//        RuleFor(x => x.PhoneNumber)
//            .NotEmpty().WithMessage("Phone number is required")
//            .Matches(@"^(84|0)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$")
//            .WithMessage("Invalid Vietnamese phone number format");

//        RuleFor(x => x.Address)
//            .NotEmpty().WithMessage("Address is required")
//            .Length(10, 500).WithMessage("Address must be between 10 and 500 characters");

//        RuleFor(x => x.Description)
//            .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Description))
//            .WithMessage("Description must not exceed 2000 characters");
//    }
//}
