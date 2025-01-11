using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace GtKram.Ui.Controllers;

[AttributeUsage(validOn: AttributeTargets.Class)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string API_KEY_NAME = "X-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_NAME, out var extractedApiKey))
        {
            context.Result = CreateResult(HttpStatusCode.Unauthorized, "ApiKeyNotProvided"); 
            return;
        }

        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = config.GetValue<string>("ApiKey");

        if (string.IsNullOrEmpty(apiKey))
        {
            context.Result = CreateResult(HttpStatusCode.InternalServerError, "MissingApiKeyConfiguration");
            return;
        }

        if (!apiKey.Equals(extractedApiKey))
        {
            context.Result = CreateResult(HttpStatusCode.Forbidden, "InvalidApiKey");
            return;
        }

        await next();
    }

    private IActionResult CreateResult(HttpStatusCode statusCode, string message)
    {
        return new ContentResult()
        {
            StatusCode = (int)statusCode,
            Content = message
        };
    }

}
