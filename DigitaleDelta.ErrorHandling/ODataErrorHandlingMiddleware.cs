// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DigitaleDelta.ErrorHandling;

/// <summary>
/// Middleware for global error handling in OData requests.
/// </summary>
/// <remarks>
/// This middleware captures both OData validation exceptions and general exceptions during the OData request pipeline execution.
/// It ensures consistent error responses by logging errors and returning structured OData-compliant error payloads with appropriate HTTP status codes.
/// </remarks>
public class ODataErrorHandlingMiddleware(RequestDelegate next, ILogger<ODataErrorHandlingMiddleware> logger)
{
    /// <summary>
    /// Handles incoming HTTP requests and ensures proper error handling within the OData request pipeline.
    /// Captures OData validation exceptions and general exceptions to log them and return standardized, structured error responses.
    /// </summary>
    /// <param name="context">The current HTTP context representing the details of the incoming request and response.</param>
    /// <returns>Returns a task that represents the asynchronous execution of the middleware logic.</returns>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ODataValidationException ex)
        {
            var requestId = context.TraceIdentifier;
            logger.LogWarning(ex, "RequestId: {RequestId} - Validation error: {Message}", requestId, ex.Message);
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var errorMessage = $"{ex.Message} (RequestId: {requestId})";
            var error = ODataErrorResponseHelper.Create(ex.Code ?? "ValidationError", errorMessage);

            await context.Response.WriteAsync(JsonSerializer.Serialize(error.Value));
        }
        catch (Exception ex)
        {
            var requestId = context.TraceIdentifier;
            logger.LogError(ex, "RequestId: {RequestId} - Unexpected error: {Message}", requestId, ex.Message);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var errorMessage = $"An unexpected error occurred. (RequestId: {requestId})";
            var error = ODataErrorResponseHelper.Create("ServerError", errorMessage, 500);

            await context.Response.WriteAsync(JsonSerializer.Serialize(error.Value));
        }
    }
}