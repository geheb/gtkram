using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GtKram.Ui.Filters;

public sealed class OperationCancelledExceptionFilter : ExceptionFilterAttribute
{
    private readonly ILogger _logger;

    public OperationCancelledExceptionFilter(ILogger<OperationCancelledExceptionFilter> logger)
    {
        _logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is OperationCanceledException)
        {
            var path = context.HttpContext.Request.Path;
            _logger.LogInformation("Request for path {Path} was cancelled", path);
            context.ExceptionHandled = true;
            context.Result = new BadRequestResult();
        }
    }
}
