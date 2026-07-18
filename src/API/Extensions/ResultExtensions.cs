using Microsoft.AspNetCore.Mvc;
using PIPDC.Domain.Common;
using PIPDC.Domain.Enums;

namespace PIPDC.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return result.Error.Type switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(result.Error),
            ErrorType.Validation => new BadRequestObjectResult(result.Error),
            ErrorType.Conflict => new ConflictObjectResult(result.Error),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(result.Error),
            _ => new ObjectResult(result.Error) { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return result.Error.Type switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(result.Error),
            ErrorType.Validation => new BadRequestObjectResult(result.Error),
            ErrorType.Conflict => new ConflictObjectResult(result.Error),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(result.Error),
            _ => new ObjectResult(result.Error) { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }
}
