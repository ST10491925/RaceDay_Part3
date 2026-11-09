namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public int Status { get; init; }

    public static ApiResult<T> Ok(T data, int status = 200) =>
        new() { Success = true, Data = data, Status = status };

    public static ApiResult<T> Fail(string error, int status = 400) =>
        new() { Success = false, Error = error, Status = status };
}

public class ApiUnauthorizedException : Exception
{
    public ApiUnauthorizedException() : base("The session is no longer valid.") { }
}
