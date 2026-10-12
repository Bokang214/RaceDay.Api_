using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Base class for the API controllers. Actions catch a general <see cref="Exception"/>
/// and return a consistent 500 response through <see cref="ServerError"/>.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ServerError(Exception ex)
    {
        var logger = HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(GetType());
        logger.LogError(ex, "Unhandled error in {Controller} while handling {Path}", GetType().Name, Request.Path);

        return Problem(
            title: "An unexpected error occurred.",
            detail: "The request could not be completed. Please try again later.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
}
