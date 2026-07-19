#nullable enable
namespace Inkboard.Application.Common;

public sealed class Result<T>
{
    public T? Data { get; set; }
    public string? Error { get; set; }
    public bool IsSuccess { get; set; }
}

public sealed class Result
{
    public string? Error { get; set; }
    public bool IsSuccess { get; set; }
}