// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace DigitaleDelta.ErrorHandling;

/// <summary>
/// Provides helper methods for creating standardized OData error responses in an API framework.
/// </summary>
public static class ODataErrorResponseHelper
{
    /// <summary>
    /// Creates an OData-compliant error response wrapped in an <see cref="ObjectResult"/>.
    /// </summary>
    /// <param name="code">A string representing the error code for the specific error type.</param>
    /// <param name="message">A description of the error that occurred.</param>
    /// <param name="statusCode">The HTTP status code associated with the error. Defaults to 400.</param>
    /// <param name="target">An optional parameter specifying the target of the error, which could be a property name or operation.</param>
    /// <param name="details">A list of <see cref="ODataErrorDetail"/> objects providing additional error details.</param>
    /// <param name="innerError">Optional additional error information for debugging purposes.</param>
    /// <param name="type">An optional URI identifier that provides more context about the error type.</param>
    /// <param name="instance">An optional instance URI identifying the request involved in the error.</param>
    /// <returns>Returns an <see cref="ObjectResult"/> containing the error information.</returns>
    public static ObjectResult Create(string code, string message, int statusCode = 400, string? target = null, List<ODataErrorDetail>? details = null, object? innerError = null, string? type = null, string? instance = null)
    {
        var response = new
        {
            error = new ODataErrorDetail
            {
                Code = code,
                Message = message,
                Target = target,
                Details = details,
                InnerError = innerError,
                Type = type,
                Status = statusCode,
                Instance = instance
            }
        };
        
        var result = new ObjectResult(response) { StatusCode = statusCode, DeclaredType = response.GetType() };
        
        result.ContentTypes.Add("application/json");
        
        return result;
    }
}