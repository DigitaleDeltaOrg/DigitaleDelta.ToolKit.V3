// Copyright (c)  2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.ODataWriter;

/// <summary>
/// Represents constant error messages used for exception handling
/// and logging within the ODataWriter namespace.
/// </summary>
public abstract record ErrorMessages
{
    /// <summary>
    /// Error message indicating that the "Add" method could not be found.
    /// Typically used in scenarios where an operation relies on the existence
    /// of a method to add items to a collection or dictionary, but it is unavailable.
    /// </summary>
    public const string addMethodNotFound = "Add method not found.";

    /// <summary>
    /// Error message indicating that the "Get" method could not be found.
    /// Commonly used in scenarios where an operation requires the existence
    /// of a "Get" method, such as when retrieving data or initializing objects,
    /// but the method is unavailable or unresolvable.
    /// </summary>
    public const string getMethodNotFound = "GetMethod not found.";
}