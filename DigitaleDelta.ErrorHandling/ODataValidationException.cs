// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.ErrorHandling;

/// <summary>
/// Represents an exception that occurs during OData validation processes.
/// </summary>
public class ODataValidationException(string message, Exception? innerException = null, string? code = null) : Exception(message, innerException)
{
    public string? Code { get; } = code;
}