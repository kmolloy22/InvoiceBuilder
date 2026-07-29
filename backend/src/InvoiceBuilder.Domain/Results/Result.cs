namespace InvoiceBuilder.Domain.Results;

/// <summary>
/// Used to indicate that a process has completed successfully without returning a specific value or data.
/// </summary>
public readonly record struct Success;

public sealed class Result<T>
{
	private Result(T? value, ResultError? error)
	{
		Value = value;
		Error = error;
	}

	public T? Value { get; }
	public ResultError? Error { get; }
	public bool IsSuccess => Error is null;
	public bool IsFailure => !IsSuccess;

	public static Result<T> Success(T value) => new(value, null);

	public static Result<T> Failure(ResultError error) => new(default, error);
}
