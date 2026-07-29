namespace InvoiceBuilder.Domain.Results;

public enum ResultErrorType
{
	Validation,
	NotFound,
	Conflict,
	Forbidden,
	FailedDependency,
	Retryable,
	Unexpected
}

public sealed record ResultError(
	string Code,
	string Message,
	ResultErrorType Type = ResultErrorType.Unexpected);
