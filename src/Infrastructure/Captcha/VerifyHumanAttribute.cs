using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PIPDC.Infrastructure.Captcha;

public sealed class VerifyHumanAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var verifier = context.HttpContext.RequestServices.GetRequiredService<TurnstileVerifier>();
        var token = context.HttpContext.Request.Headers["X-Turnstile-Token"].ToString()
                    ?? context.HttpContext.Request.Form["cf-turnstile-response"].ToString();
        var idempotencyKey = context.HttpContext.Request.Headers["X-Turnstile-Idempotency-Key"].ToString();
        var ip = context.HttpContext.Request.Headers["CF-Connecting-IP"].ToString()
                 ?? context.HttpContext.Connection.RemoteIpAddress?.ToString();

        if (!await verifier.IsHumanAsync(token, ip, idempotencyKey, context.HttpContext.RequestAborted))
        {
            context.Result = new ObjectResult(new
            {
                code = "HUMAN_VERIFICATION_FAILED",
                message = "Please complete the verification and try again.",
                type = "Validation"
            })
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }
        await next();
    }
}
