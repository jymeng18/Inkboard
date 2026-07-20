#nullable enable
namespace Inkboard.Application.Common;

public enum ErrorType
{
    None,
    NotFound,
    Forbidden,
    Validation,
    Conflict,
    Unexpected,
}

public sealed class Result<T>
{
    public T? Data { get; set; }
    public string? Error { get; set; }
    public bool IsSuccess { get; set; }
    public ErrorType ErrorType { get; set; } = ErrorType.None;

    public static Result<T> Ok(T data) =>
        new() { IsSuccess = true, Data = data };

    public static Result<T> Fail(ErrorType errorType, string error) =>
        new() { IsSuccess = false, ErrorType = errorType, Error = error };
}

public sealed class Result
{
    public string? Error { get; set; }
    public bool IsSuccess { get; set; }
    public ErrorType ErrorType { get; set; } = ErrorType.None;

    public static Result Ok() =>
        new() { IsSuccess = true };

    public static Result Fail(ErrorType errorType, string error) =>
        new() { IsSuccess = false, ErrorType = errorType, Error = error };
}