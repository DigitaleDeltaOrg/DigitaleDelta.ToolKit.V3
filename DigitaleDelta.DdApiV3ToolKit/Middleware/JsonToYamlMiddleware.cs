// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.DdApiV3ToolKit.Middleware;

using YamlDotNet.Serialization;

/// <summary>
/// Middleware class that converts JSON response to YAML format based on the request Accept header.
/// </summary>
public class JsonToYamlMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the middleware class that converts JSON response to YAML format based on the request Accept header.
    /// </summary>
    /// <param name="context">The HttpContext object.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();

        context.Response.Body = memoryStream;

        await next(context);

        memoryStream.Seek(0, SeekOrigin.Begin);

        var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

        memoryStream.Seek(0, SeekOrigin.Begin);

        if (context.Request.Headers.Accept.ToString().Contains("application/x-yaml"))
        {
            if (!string.IsNullOrEmpty(responseBody) && IsJson(responseBody))
            {
                var jsonElement = System.Text.Json.JsonDocument.Parse(responseBody).RootElement;
                var deserializer = new DeserializerBuilder().Build();
                var serializer = new SerializerBuilder().JsonCompatible().Build();
                var yamlObject = deserializer.Deserialize<object>(jsonElement.GetRawText());
                var yaml = serializer.Serialize(yamlObject);

                // Need to reset the response body as it was already written to once
                context.Response.Body = originalBodyStream;
                await context.Response.WriteAsync(yaml);
            }
            else
            {
                await memoryStream.CopyToAsync(originalBodyStream);
            }
        }
        else
        {
            await memoryStream.CopyToAsync(originalBodyStream);
        }
    }

    /// <summary>
    /// Determines whether the provided input string is in a valid JSON format.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <returns>True if the input string is a valid JSON object or array, otherwise false.</returns>
    private static bool IsJson(string input)
    {
        input = input.Trim();

        return (input.StartsWith('{') && input.EndsWith('}')) || (input.StartsWith('[') && input.EndsWith(']'));
    }
}
