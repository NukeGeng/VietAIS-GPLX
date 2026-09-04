using Gplx.BuildingBlocks;
using JasperFx;
using JasperFx.Events;
using Microsoft.AspNetCore.Diagnostics;

namespace Gplx.Api;

public static class ApiExceptionHandler
{
    public static async Task HandleAsync(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var (statusCode, title) = exception switch
        {
            DomainRuleViolationException => (StatusCodes.Status400BadRequest, "The request violates a business rule."),
            EventStreamUnexpectedMaxEventIdException => (StatusCodes.Status409Conflict, "The exam was updated by another request."),
            ConcurrencyException => (StatusCodes.Status409Conflict, "The resource was updated by another request."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();
        var detail = environment.IsDevelopment() ? exception?.Message : null;
        await Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            type: $"https://httpstatuses.com/{statusCode}").ExecuteAsync(context);
    }
}
