namespace DigitaleDelta.ErrorHandling;

/// <summary>
/// Represents an exception specific to OData API operations.
/// This exception is used to encapsulate errors that occur during OData API processing and provides additional
/// context through a status code and an optional error code.
/// </summary>
public class ODataApiException(string message, int statusCode, string? code = null, Exception? innerException = null) : Exception(message, innerException)
{
    public int StatusCode { get; } = statusCode;
    public string? Code { get; } = code;
}
