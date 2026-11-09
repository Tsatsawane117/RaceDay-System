namespace RaceDay.Web.Services;


public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public int StatusCode { get; init; }
    public string Error { get; init; } = string.Empty;
}
