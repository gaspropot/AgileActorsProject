namespace AgileActorsProject.Domain.Common;

// <summary>
/// Represents the outcome of an operation that can either succeed or fail.
/// </summary>
public class Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess { get; }

    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    private Result(string error)
    {
        Error = error;
        IsSuccess = false;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
}

/// <summary>
/// Represents the outcome of an operation with no return value.
/// </summary>
public class Result
{
    public string? Error { get; }
    public bool IsSuccess { get; }

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}
