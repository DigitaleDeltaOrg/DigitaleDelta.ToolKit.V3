// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.ErrorHandling;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

/// <summary>
/// Encapsulates detailed error information following the OData error specification.
/// </summary>
/// <remarks>
/// This class provides a structured format for error responses, including error codes, descriptive
/// messages, and additional context to aid in diagnostics and resolution. It supports nested details
/// for complex error scenarios and optional metadata fields for enhanced tracing.
/// </remarks>
public class ODataErrorDetail
{
    /// <summary>
    /// Gets or sets a code that uniquely identifies the error type in the context of OData error handling.
    /// </summary>
    /// <remarks>
    /// This property is used to specify a standardised code that represents the nature of the error,
    /// often aligning with the OData specification or application-specific error identification conventions.
    /// It is intended to help in diagnosing and troubleshooting issues by providing a succinct reference to the type of error encountered.
    /// </remarks>
    public string Code { get; set; } = "ServerError";

    /// <summary>
    /// Gets or sets a message that provides a human-readable description of the error.
    /// </summary>
    /// <remarks>
    /// This property is intended to convey information about the nature of the error in a way
    /// that is meaningful and understandable to users or developers. It can include detailed
    /// explanations, guidance on how to address the issue, or other contextual information to
    /// assist in diagnosing and resolving the problem.
    /// </remarks>
    public string Message { get; set; } = "An unexpected error occurred.";

    /// <summary>
    /// Gets or sets the specific target of the error in the context of the operation that caused it.
    /// </summary>
    /// <remarks>
    /// This property provides additional context for the error by identifying the element
    /// (such as a property, operation, or resource) involved. It is particularly useful
    /// for troubleshooting or handling errors in a more precise manner.
    /// </remarks>
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets a collection of detailed errors associated with the current error.
    /// </summary>
    /// <remarks>
    /// This property provides access to additional error details that may give further context
    /// or granularity to the main error. Each item in the collection represents a specific aspect
    /// of the error, such as a child error or related issue. This can be useful especially when
    /// multiple issues are identified during processing.
    /// </remarks>
    public List<ODataErrorDetail>? Details { get; set; }

    /// <summary>
    /// Gets or sets additional information about the error, which can provide more context
    /// or assist in diagnosing the issue.
    /// </summary>
    /// <remarks>
    /// This property is used to include extended error details beyond the standard error properties.
    /// The content of this property can vary and may include implementation-specific data or objects
    /// to provide further insight into the cause or nature of the error.
    /// </remarks>
    public object? InnerError { get; set; }

    /// <summary>
    /// Gets or sets the type identifier or a reference URL providing additional context regarding the error.
    /// </summary>
    /// <remarks>
    /// This property is typically used to convey a URI or a string identifier that links to further
    /// documentation or resources explaining the specifics of the error type. It aids developers
    /// and users in understanding the nature and categorisation of the error.
    /// </remarks>
    public string? Type { get; set; } // URL naar documentatie

    /// <summary>
    /// Gets or sets the HTTP status code associated with the error response.
    /// </summary>
    /// <remarks>
    /// This property is used to indicate the HTTP status code that corresponds to the error encountered.
    /// It allows clients to identify the nature of the failure in accordance with standard HTTP response codes.
    /// Common values include 400 for Bad Request, 404 for Not Found, and 500 for Internal Server Error.
    /// </remarks>
    public int? Status { get; set; }

    /// <summary>
    /// Gets or sets the URI that identifies the specific occurrence of the error.
    /// </summary>
    /// <remarks>
    /// This property is used to provide a unique identifier or reference for the error instance,
    /// enabling better tracking or correlation of issues in distributed systems. It can point
    /// to a specific log entry, diagnostic page, or other resources that give additional context
    /// for troubleshooting the error.
    /// </remarks>
    public string? Instance { get; set; }
}
