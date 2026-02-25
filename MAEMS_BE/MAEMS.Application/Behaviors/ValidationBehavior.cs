using FluentValidation;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            var errors = failures.Select(f => f.ErrorMessage).ToList();
            
            // Try to create a BaseResponse with validation errors
            var responseType = typeof(TResponse);
            
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(BaseResponse<>))
            {
                var dataType = responseType.GetGenericArguments()[0];
                var baseResponseType = typeof(BaseResponse<>).MakeGenericType(dataType);
                var failureMethod = baseResponseType.GetMethod("FailureResponse");
                
                var response = failureMethod?.Invoke(null, new object[] { "Validation failed", errors });
                return (TResponse)response!;
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
