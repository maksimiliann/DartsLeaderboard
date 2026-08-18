namespace DartsLeaderboard.Domain.Common;

public sealed class Result
{
    private Result(bool isSuccess, DomainErrorCode? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public DomainErrorCode? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(DomainErrorCode error) => new(false, error);
}

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, DomainErrorCode? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public DomainErrorCode? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(DomainErrorCode error) => new(false, default, error);
}
