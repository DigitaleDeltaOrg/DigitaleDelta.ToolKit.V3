// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using DigitaleDelta.ODataTranslator.Helpers;
using DigitaleDelta.Contracts.Configuration;

namespace DigitaleDelta.ODataTranslator;

/// <summary>
/// ODataFilterValidator validates OData filter expressions against property and function maps.
/// </summary>
/// <param name="propertyMaps"></param>
/// <param name="functionMaps"></param>
public class ODataFilterValidator(Dictionary<string, ODataToSqlMap> propertyMaps, Dictionary<string, ODataFunctionMap> functionMaps)
{
    /// <summary>
    /// Validates a filter expression using the property and function maps.
    /// </summary>
    /// <param name="filterContext"></param>
    /// <param name="error"></param>
    /// <returns></returns>
    public bool TryValidate(ODataParser.FilterOptionContext? filterContext, out string? error)
    {
        if (filterContext == null)
        {
            error = ErrorMessages.filterContextIsNull;
            return false;
        }

        var (isValid, errorPart) = ValidateFilterExpression(filterContext.filterExpr());
        error = isValid ? null : string.Format(ErrorMessages.invalidFilterExpression, errorPart);
        return isValid;
    }

    /// <summary>
    /// Validates a filter expression recursively.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private (bool Valid, string? Error) ValidateFilterExpression(ODataParser.FilterExprContext context)
    {
        // Primary expression (property path or literal)
        if (context.primary() != null)
        {
            var propertyPath = context.primary().GetText();

            // Skip validation for literal values
            if (propertyPath.IsLiteralValue())
            {
                return (true, null);
            }

            // Check if the property exists and is allowed in filters
            if (!TryGetPropertyMap(propertyPath, out var propertyMap) || propertyMap == null)
            {
                return (false, string.Format(ErrorMessages.unknownProperty, propertyPath));
            }

            return propertyMap.DisallowInFilter ? (false, string.Format(ErrorMessages.propertyNotAllowedInFilter, propertyPath)) : (true, null);
        }

        // Function call
        if (context.function() != null)
        {
            return ValidateFunctionCall(context.function());
        }

        // Unary NOT
        if (context.filterExpr().Length == 1 && context.NOT() != null)
        {
            return ValidateFilterExpression(context.filterExpr(0));
        }

        // Parenthesized expression
        if (context.LPAREN() != null && context.RPAREN() != null)
        {
            return ValidateFilterExpression(context.filterExpr(0));
        }

        // Binary expression (AND, OR, comparison)
        if (context.filterExpr().Length == 2)
        {
            var leftValid = ValidateFilterExpression(context.filterExpr(0));
            if (!leftValid.Valid)
            {
                return leftValid;
            }

            var rightValid = ValidateFilterExpression(context.filterExpr(1));
            return rightValid;
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a function call expression.
    /// </summary>
    /// <param name="functionContext"></param>
    /// <returns></returns>
    private (bool Valid, string? Error) ValidateFunctionCall(ODataParser.FunctionContext functionContext)
    {
        var functionName = functionContext.Start.Text.ToLower();

        if (!functionMaps.TryGetValue(functionName, out var functionMap))
        {
            return (false, string.Format(ErrorMessages.unknownFunction, functionName));
        }

        var arguments = functionContext.filterExpr();

        // Handle special case: function with quoted string literal that parser doesn't recognize as argument
        if (arguments.Length == 0 && functionMap.ExpectedArgumentTypes.Count == 1)
        {
            var raw = functionContext.GetText();
            var lParen = raw.IndexOf('(');
            var rParen = raw.LastIndexOf(')');

            if (lParen >= 0 && rParen > lParen + 1)
            {
                var inner = raw.Substring(lParen + 1, rParen - lParen - 1).Trim();

                if (!string.IsNullOrWhiteSpace(inner) && IsQuoted(inner))
                {
                    if (!string.Equals(functionMap.ExpectedArgumentTypes[0], "string", StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, string.Format(ErrorMessages.invalidFunctionDataType, 1, functionName, "Edm.String", functionMap.ExpectedArgumentTypes[0]));
                    }

                    return (true, null);
                }
            }

            return (false, string.Format(ErrorMessages.functionParameterCountMismatch, functionName, functionMap.ExpectedArgumentTypes.Count, 0));
        }

        // Validate argument count
        if (arguments.Length != functionMap.ExpectedArgumentTypes.Count)
        {
            return (false, string.Format(ErrorMessages.functionParameterCountMismatch, functionName, functionMap.ExpectedArgumentTypes.Count, arguments.Length));
        }

        // Validate each argument type
        for (var i = 0; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            var expectedType = functionMap.ExpectedArgumentTypes[i];
            var actualType = GetExpressionEdmType(argument);

            if (!string.Equals(expectedType, MapEdmToExpected(actualType), StringComparison.OrdinalIgnoreCase))
            {
                return (false, string.Format(ErrorMessages.invalidFunctionDataType, i + 1, functionName, actualType, expectedType));
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Infers the EDM type of an expression.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private string GetExpressionEdmType(ODataParser.FilterExprContext context)
    {
        // Function returns its defined return type
        if (context.function() != null)
        {
            var functionName = context.function().Start.Text.ToLower();
            if (functionMaps.TryGetValue(functionName, out var functionMap) && !string.IsNullOrEmpty(functionMap.ReturnType))
            {
                return functionMap.ReturnType;
            }
            return "Edm.Unknown";
        }

        // Unary 'NOT' returns boolean
        if (context.NOT() != null)
        {
            return "Edm.Boolean";
        }

        // Comparison or logical operators return boolean
        if (context.comparison() != null || context.AND() != null || context.OR() != null)
        {
            return "Edm.Boolean";
        }

        // Primary expression (property or literal)
        if (context.primary() != null)
        {
            var primaryText = context.primary().GetText();

            // Try to infer from literal
            var literalType = InferEdmTypeFromLiteral(primaryText);
            if (literalType != null)
            {
                return literalType;
            }

            // Try to get the property type
            if (TryGetPropertyMap(primaryText, out var propertyMap) && propertyMap != null)
            {
                return propertyMap.EdmType;
            }
        }

        // Try full text as the property path
        var text = context.GetText();
        if (!IsQuoted(text) && TryGetPropertyMap(text, out var map) && map != null)
        {
            return map.EdmType;
        }

        // Try to infer from the text as literal
        var inferredType = InferEdmTypeFromLiteral(text);
        if (inferredType != null)
        {
            return inferredType;
        }

        // Recursive case for expressions with parentheses
        if (context.filterExpr().Length == 1)
        {
            return GetExpressionEdmType(context.filterExpr(0));
        }

        return "Edm.Unknown";
    }

    /// <summary>
    /// Tries to retrieve a property map for the given property path.
    /// </summary>
    /// <param name="propertyPath"></param>
    /// <param name="propertyMap"></param>
    /// <returns></returns>
    private bool TryGetPropertyMap(string propertyPath, out ODataToSqlMap? propertyMap)
    {
        if (string.IsNullOrWhiteSpace(propertyPath))
        {
            propertyMap = null;
            return false;
        }

        return propertyMaps.TryGetValue(propertyPath, out propertyMap);
    }

    /// <summary>
    /// Infers the EDM type from a literal value.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static string? InferEdmTypeFromLiteral(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // null literal
        if (string.Equals(text, "null", StringComparison.OrdinalIgnoreCase))
        {
            return "Edm.Null";
        }

        // boolean literal
        if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
        {
            return "Edm.Boolean";
        }

        // string literal: '...'
        if (text.Length >= 2 && text[0] == '\'' && text[^1] == '\'')
        {
            return "Edm.String";
        }

        // numeric detection
        if (int.TryParse(text, out _))
        {
            return "Edm.Int32";
        }

        if (long.TryParse(text, out _))
        {
            return "Edm.Int64";
        }

        if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            return "Edm.Double";
        }

        return null;
    }

    /// <summary>
    /// Determines if a string is enclosed in quotes.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private static bool IsQuoted(string s) =>
        s.Length >= 2 && ((s[0] == '\'' && s[^1] == '\'') || (s[0] == '"' && s[^1] == '"'));

    /// <summary>
    /// Maps EDM type to expected type for function validation.
    /// </summary>
    /// <param name="edmType"></param>
    /// <returns></returns>
    private static string MapEdmToExpected(string? edmType)
    {
        if (string.IsNullOrWhiteSpace(edmType))
        {
            return "unknown";
        }

        var t = edmType.StartsWith("Edm.", StringComparison.OrdinalIgnoreCase)
            ? edmType[4..]
            : edmType;

        return t.ToLowerInvariant() switch
        {
            "string" or "guid" or "binary" => "string",
            "boolean" => "boolean",
            "byte" or "sbyte" or "int16" or "int32" or "int64" => "integer",
            "decimal" or "double" or "single" or "float" => "decimal",
            "datetime" or "datetimeoffset" or "date" or "timeofday" or "duration" => "datetime",
            "geography" or "geographycollection" or "geographypoint" or "geographylinestring" or "geographypolygon" or "geographymultipoint" or "geographymultilinestring" or "geographymultipolygon" => "geography",
            _ => t.ToLowerInvariant()
        };
    }
}
