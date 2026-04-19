using FluentValidation;

namespace MAEMS.Application.Features.Programs.Queries.GetProgramById;

public class GetProgramByIdQueryValidator : AbstractValidator<GetProgramByIdQuery>
{
    public GetProgramByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Program ID must be greater than 0");
    }
}
