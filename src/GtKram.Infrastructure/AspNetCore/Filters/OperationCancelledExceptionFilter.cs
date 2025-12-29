using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GtKram.Infrastructure.AspNetCore.Filters;

public sealed class OperationCancelledExceptionFilter : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is OperationCanceledException)
        {
            var path = context.HttpContext.Request.Path;
            context.ExceptionHandled = true;
            context.Result = new BadRequestResult();
        }
    }
}
