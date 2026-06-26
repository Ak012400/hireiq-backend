namespace HireIQ.Application.Common;

/// <summary>
/// Lightweight Result wrapper for service responses.
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    private Result(bool ok, T? value, string? error, string? code)
    {
        IsSuccess = ok; Value = value; Error = error; ErrorCode = code;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);
    public static Result<T> Failure(string error, string? code = null) => new(false, default, error, code);
}
