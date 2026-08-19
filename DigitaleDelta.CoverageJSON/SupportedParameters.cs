namespace DigitaleDelta.CoverageJSON;

public sealed record CoverageParameterDefinition(
    string Name,
    string Type,
    bool Multiple = false,
    IReadOnlySet<string>? AllowedValues = null);

public static class SupportedParameters
{
    public static IReadOnlyDictionary<string, CoverageParameterDefinition> All { get; } = new Dictionary<string, CoverageParameterDefinition>
        {
            ["slicebegin"] = new("slicebegin", "iso8610-datetimeoffset"),
            ["sliceend"] = new("sliceend", "iso8610-datetimeoffset"),
            ["aggregationfunction"] = new(
                    "aggregationfunction",
                    "string",
                    AllowedValues: new HashSet<string>
                    {
                        "mean", "min", "max", "sum", "count"
                    }),
            ["aggregationduration"] = new("aggregationduration", "iso8601-duration"),
            ["selectranges"] = new("selectranges", "string", true),
            ["selectaxes"] = new("selectaxes", "string", true)
        };
}

public static class ValidateParameters
{
    public static (bool valid, List<string>? errors) Validate(Dictionary<string, IReadOnlyList<string>> queryParameters)
    {
        queryParameters = queryParameters.Where(a => !a.Key.StartsWith('$')).ToDictionary(a => a.Key, a => a.Value);

        if (queryParameters.Count == 0)
        {
            return (true, null);
        }

        var invalidKeys = queryParameters.Keys.Where(a => !SupportedParameters.All.ContainsKey(a)).ToList();
        var errors = invalidKeys.Select(a => $"Invalid query parameter: {a}").ToList();

        if (errors.Count != 0)
        {
            return (false, errors);
        }

        ValidateTimeSlice(queryParameters, errors);
        ValidateAggregationMethodAndDuration(queryParameters, errors);

        return (true, null);
    }

    private static void ValidateAggregationMethodAndDuration(Dictionary<string, IReadOnlyList<string>> queryParameters, List<string> errors)
    {
        if (queryParameters.ContainsKey("aggregationmethod") && !queryParameters.ContainsKey("aggregationduration"))
        {
            errors.Add("Aggregationmethod and aggregationduration go together: none or both");

            return;
        }

        if (queryParameters.ContainsKey("aggregationduration") && !queryParameters.ContainsKey("aggregationmethod"))
        {
            errors.Add("Aggregationmethod and aggregationduration go together: none or both");

            return;
        }

        throw new NotImplementedException();
    }

    private static void ValidateTimeSlice(Dictionary<string, IReadOnlyList<string>> queryParameters, List<string> errors)
    {
        if (!queryParameters.ContainsKey("slicebegin") && !queryParameters.ContainsKey("sliceend"))
        {
            return;
        }

        var start = ValidateTime("slicebegin", queryParameters, errors);
        var end = ValidateTime("sliceend", queryParameters, errors);

        if (start == null || end == null)
        {
            return;
        }

        if (start > end)
        {
            errors.Add("Slicebegin must be before sliceend");
        }
    }

    private static DateTimeOffset? ValidateTime(string slice, Dictionary<string, IReadOnlyList<string>> queryParameters, List<string> errors)
    {
        // Check if queryParameters contains slicebegin and validate its value to be ISO8601.
        if (!queryParameters.TryGetValue(slice, out var parameter))
        {
            return null;
        }

        var valid = DateTimeOffset.TryParse(parameter[0], out var result);

        if (!valid)
        {
            errors.Add($"Invalid ISO8601 value for {slice}: {queryParameters[slice][0]}");
        }

        return result;
    }
}
