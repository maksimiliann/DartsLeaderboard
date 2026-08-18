using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Common;

public sealed record OperationResult(bool IsSuccess, string? ErrorMessage)
{
    public static OperationResult Success() => new(true, null);

    public static OperationResult Fail(string message) => new(false, message);

    public static OperationResult Fail(DomainErrorCode code) => new(false, ErrorText.For(code));
}

public sealed record OperationResult<T>(bool IsSuccess, T? Value, string? ErrorMessage)
{
    public static OperationResult<T> Success(T value) => new(true, value, null);

    public static OperationResult<T> Fail(string message) => new(false, default, message);

    public static OperationResult<T> Fail(DomainErrorCode code) => new(false, default, ErrorText.For(code));
}
