using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceAdvisory.Core.Api;

public static class ApiProblemDetails
{
    public static ProblemDetails Create(List<Error> errors, HttpContext httpContext)
    {
        var hasError = errors.Count > 0;
        Error error = default!;
        if (hasError)
        {
            error = errors[0];
        }

        var statusCode = hasError ? MapStatusCode(error.Type) : StatusCodes.Status500InternalServerError;

        var details = new ProblemDetails
        {
            Status = statusCode,
            Title = hasError ? error.Description : "An error occurred.",
            Detail = hasError ? error.Description : "An unexpected error occurred.",
            Type = BuildType(hasError ? error : default!, hasError),
            Instance = httpContext.Request.Path.Value
        };

        if (hasError)
        {
            details.Extensions["code"] = error.Code;
            details.Extensions["errors"] = errors
                .GroupBy(item => item.Code)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Description).ToArray());
        }

        return details;
    }

    public static ProblemDetails Create(Exception exception, HttpContext httpContext)
    {
        var statusCode = exception switch
        {
            FluentValidation.ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = exception.Message,
            Detail = GetDetailFor(exception),
            Type = $"https://api.marketplaceadvisory.com/errors/{statusCode}",
            Instance = httpContext.Request.Path.Value
        };

        problemDetails.Extensions["code"] = exception switch
        {
            FluentValidation.ValidationException => "validation.failed",
            _ => "unexpected_error"
        };

        return problemDetails;
    }

    private static int MapStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string BuildType(Error error, bool hasError)
    {
        if (!hasError)
        {
            return "https://api.marketplaceadvisory.com/errors/internal-error";
        }

        var suffix = error.Code.Replace(" ", "-").Replace("_", "-");
        return $"https://api.marketplaceadvisory.com/errors/{suffix}";
    }

    private static string GetDetailFor(Exception exception) => exception switch
    {
        FluentValidation.ValidationException => "One or more validation errors occurred.",
        _ => "An unexpected error occurred. Please try again later."
    };
}
