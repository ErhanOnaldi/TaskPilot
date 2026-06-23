using System.Net;
using System.Text.Json.Serialization;

namespace TaskPilot.Application;

public class ServiceResult
{
    public List<string>? ErrorMessages { get; init; }
    [JsonIgnore]
    public bool IsSuccess => ErrorMessages == null || ErrorMessages.Count == 0;
    [JsonIgnore] public bool IsFail => !IsSuccess ;
    [JsonIgnore] public HttpStatusCode Status { get; private init; } 
    [JsonIgnore] public string? UrlAsCreated { get; set; }

    public static ServiceResult Success(HttpStatusCode status = HttpStatusCode.OK)
    {
        return new ServiceResult
        {
            Status = status,
        };
    }
    
    public static ServiceResult Fail(List<string> errorMessages, HttpStatusCode status= HttpStatusCode.BadRequest)
    {
        return new ServiceResult
        {
            ErrorMessages = errorMessages,
            Status = status,
        };
    }
    
    public static ServiceResult Fail(string errorMessage, HttpStatusCode status= HttpStatusCode.BadRequest)
    {
        return new ServiceResult
        {
            ErrorMessages = [errorMessage],
            Status = status,
        };
    }
}

public class ServiceResult<T>
{
    public T? Data { get; set; }
    public List<string>? ErrorMessages { get; init; }
    [JsonIgnore]
    public bool IsSuccess => ErrorMessages == null || ErrorMessages.Count == 0;
    [JsonIgnore]
    public bool IsFail => !IsSuccess ;
    [JsonIgnore]
    public HttpStatusCode Status { get; private init; } 
    [JsonIgnore] public string? UrlAsCreated { get; private init; }

    public static ServiceResult<T> Success(T data, HttpStatusCode status= HttpStatusCode.OK)
    {
        return new ServiceResult<T>
        {
            Data = data,
            Status = status,
        };
    }
    public static ServiceResult<T> SuccessAsCreated(T data, string url)
    {
        return new ServiceResult<T>
        {
            Data = data,
            Status = HttpStatusCode.Created,
            UrlAsCreated = url
        };
    }
    public static ServiceResult<T> Fail(List<string> errorMessages, HttpStatusCode status= HttpStatusCode.BadRequest)
    {
        return new ServiceResult<T>
        {
            ErrorMessages = errorMessages,
            Status = status,
        };
    }
    
    public static ServiceResult<T> Fail(string errorMessage, HttpStatusCode status= HttpStatusCode.BadRequest)
    {
        return new ServiceResult<T>
        {
            ErrorMessages = [errorMessage],
            Status = status,
        };
    }
}