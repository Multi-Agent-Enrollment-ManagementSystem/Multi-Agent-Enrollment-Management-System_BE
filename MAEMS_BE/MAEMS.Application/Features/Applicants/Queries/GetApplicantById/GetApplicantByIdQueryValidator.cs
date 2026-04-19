using FluentValidation;

namespace MAEMS.Application.Features.Applicants.Queries.GetApplicantById;

public class GetApplicantByIdQueryValidator : AbstractValidator<GetApplicantByIdQuery>
{
    public GetApplicantByIdQueryValidator()
    {
        RuleFor(x => x.ApplicantId)
            .GreaterThan(0).WithMessage("Applicant ID must be greater than 0");
    }
}
