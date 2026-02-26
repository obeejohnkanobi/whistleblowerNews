namespace WhistleblowerNews.Application.Common;

public enum ResultStatus
{
    Ok,
    Created,
    NoContent,
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound
}

public sealed record ServiceResult(ResultStatus Status, string? Error = null)
{
    public static ServiceResult Ok() => new(ResultStatus.Ok);
    public static ServiceResult Created() => new(ResultStatus.Created);
    public static ServiceResult NoContent() => new(ResultStatus.NoContent);
    public static ServiceResult BadRequest(string message) => new(ResultStatus.BadRequest, message);
    public static ServiceResult Unauthorized(string message) => new(ResultStatus.Unauthorized, message);
    public static ServiceResult Forbidden(string message) => new(ResultStatus.Forbidden, message);
    public static ServiceResult NotFound(string message) => new(ResultStatus.NotFound, message);
}

public sealed record ServiceResult<T>(ResultStatus Status, T? Value = default, string? Error = null)
{
    public static ServiceResult<T> Ok(T value) => new(ResultStatus.Ok, value);
    public static ServiceResult<T> Created(T value) => new(ResultStatus.Created, value);
    public static ServiceResult<T> BadRequest(string message) => new(ResultStatus.BadRequest, default, message);
    public static ServiceResult<T> Unauthorized(string message) => new(ResultStatus.Unauthorized, default, message);
    public static ServiceResult<T> Forbidden(string message) => new(ResultStatus.Forbidden, default, message);
    public static ServiceResult<T> NotFound(string message) => new(ResultStatus.NotFound, default, message);
}
