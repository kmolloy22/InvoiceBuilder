using FluentValidation;

namespace InvoiceBuilder.Api.Shared.Infrastructure.Validation;

public class ValidationFilter<T> : IEndpointFilter
{
	private readonly IValidator<T> _validator;

	public ValidationFilter(IValidator<T> validator)
	{
		_validator = validator;
	}

	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		var request = context.Arguments.OfType<T>().FirstOrDefault();
		if (request is null)
		{
			return await next(context);
		}

		var validationResult = await _validator.ValidateAsync(request, context.HttpContext.RequestAborted);
		if (!validationResult.IsValid)
		{
			var errors = validationResult.Errors
				.GroupBy(x => x.PropertyName)
				.ToDictionary(
				g => g.Key,
				g => g.Select(x => x.ErrorMessage).ToArray());

			return Results.ValidationProblem(errors);
		}

		return await next(context);
	}
}
