using System.Reflection;
using System.Text.Json;
using DigitaleDelta.CoverageJSON.Models;

namespace DigitaleDelta.QueryService.Tests;

public class CoverageTransformerTests
{
    [Fact]
    public void Slice_OneDimensionalTimeRange()
    {
        var times = Times(0, 1, 2);
        var coverage = CreateCoverage(axisNames: ["t"], shape: [3], values: [10, 20, 30], times);

        Slice(coverage, times[1], times[2]);

        var axis = coverage.Domain.Axes["t"];
        var range = coverage.Ranges["value"];

        Assert.Equal(2, axis.Values!.Count);
        Assert.Equal(times[1], axis.Values[0].GetDateTimeOffset());
        Assert.Equal(times[2], axis.Values[1].GetDateTimeOffset());
        Assert.Equal([20d, 30d], range.Values);
        Assert.Equal([2], range.Shape);
    }

    [Fact]
    public void Slice_TDimensionFirstInTwoDimensionalRange()
    {
        var times = Times(0, 1, 2);
        var coverage = CreateCoverage(axisNames: ["t", "x"], shape: [3, 2], values: [10, 11, 20, 21, 30, 31], times);

        Slice(coverage, times[1], times[2]);

        var range = coverage.Ranges["value"];

        Assert.Equal([20d, 21d, 30d, 31d], range.Values);
        Assert.Equal([2, 2], range.Shape);
    }

    [Fact]
    public void Slice_TDimensionLastInTwoDimensionalRange()
    {
        var times = Times(0, 1, 2);
        var coverage = CreateCoverage(axisNames: ["x", "t"], shape: [2, 3], values: [10, 20, 30, 11, 21, 31], times);

        Slice(coverage, times[1], times[2]);

        var range = coverage.Ranges["value"];

        Assert.Equal([20d, 30d, 21d, 31d], range.Values);
        Assert.Equal([2, 2], range.Shape);
    }

    [Fact]
    public void Slice_WithOnlyStart_KeepsEverythingAfterStart()
    {
        var times = Times(0, 1, 2);
        var coverage = CreateCoverage(axisNames: ["t"], shape: [3], values: [10, 20, 30], times);

        Slice(coverage, times[1], null);

        Assert.Equal([20d, 30d], coverage.Ranges["value"].Values);
        Assert.Equal(2, coverage.Domain.Axes["t"].Values!.Count);
    }

    [Fact]
    public void Aggregate_Mean_OverOneHour()
    {
        var times = TimesMinutes(0, 30, 60, 90);
        var coverage = CreateCoverage(axisNames: ["t"], shape: [4], values: [10, 20, 30, 40], times);

        Aggregate(coverage, "mean", "PT1H");

        var range = coverage.Ranges["value"];

        Assert.Equal([15d, 35d], range.Values);
        Assert.Equal([2], range.Shape);

        var aggregatedTimes = coverage.Domain.Axes["t"].Values!;

        Assert.Equal(times[0], aggregatedTimes[0].GetDateTimeOffset());
        Assert.Equal(times[2], aggregatedTimes[1].GetDateTimeOffset());
    }

    [Fact]
    public void Aggregate_Mean_PreservesOtherDimensions()
    {
        var times = TimesMinutes(0, 30, 60, 90);
        var coverage = CreateCoverage(axisNames: ["t", "x"], shape: [4, 2], values: [1, 10, 3, 30, 5, 50, 7, 70], times);

        Aggregate(coverage, "mean", "PT1H");

        var range = coverage.Ranges["value"];

        Assert.Equal(
            [
                2d, 20d,
                6d, 60d
            ],
            range.Values);

        Assert.Equal([2, 2], range.Shape);
    }

    [Theory]
    [InlineData("min", 10d, 30d)]
    [InlineData("max", 20d, 40d)]
    [InlineData("sum", 30d, 70d)]
    [InlineData("mean", 15d, 35d)]
    [InlineData("count", 2d, 2d)]
    public void Aggregate_SupportedMethods(string method, double firstExpected, double secondExpected)
    {
        var times = TimesMinutes(0, 30, 60, 90);
        var coverage = CreateCoverage(axisNames: ["t"], shape: [4], values: [10, 20, 30, 40], times);

        Aggregate(coverage, method, "PT1H");

        Assert.Equal([firstExpected, secondExpected], coverage.Ranges["value"].Values);
    }

    [Fact]
    public void Shape_SelectRanges_RemovesUnselectedRanges()
    {
        var times = Times(0, 1, 2);
        var coverage = CreateCoverage(axisNames: ["t"], shape: [3], values: [10, 20, 30], times);

        coverage.Ranges["other"] = new CoverageRange { Name = "other", AxisNames = ["t"], Shape = [3], Values = [100, 200, 300] };
        Shape(coverage, ranges: ["value"]);

        var range = Assert.Single(coverage.Ranges);

        Assert.Equal("value", range.Key);
    }

    [Fact]
    public void Shape_SelectAxes_RemovesUnusedAxes()
    {
        var times = Times(0, 1, 2);
        var coverage = CreateCoverage(axisNames: ["t"], shape: [3], values: [10, 20, 30], times);

        Shape(coverage, axes: ["t"]);

        Assert.True(coverage.Domain.Axes.ContainsKey("t"));
        Assert.False(coverage.Domain.Axes.ContainsKey("x"));
    }

    [Fact]
    public void Shape_SelectAxes_ThrowsWhenRangeUsesUnselectedAxis()
    {
        var times = Times(0, 1, 2);
        var coverage = CreateCoverage(axisNames: ["t", "x"], shape: [3, 2], values: [10, 11, 20, 21, 30, 31], times);
        var exception = Assert.Throws<TargetInvocationException>(() => Shape(coverage, axes: ["t"]));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void Aggregate_Monthly_UsesCalendarMonths()
    {
        var times = new[]
        {
            UtcDate(2026, 1, 15),
            UtcDate(2026, 1, 31),
            UtcDate(2026, 2, 1),
            UtcDate(2026, 2, 15)
        };

        var coverage = CreateCoverage(axisNames: ["t"], shape: [4], values: [10, 20, 30, 40], times);

        Aggregate(coverage, "mean", "P1M");

        var range = coverage.Ranges["value"];
        var aggregatedTimes = coverage.Domain.Axes["t"].Values!;

        Assert.Equal([15d, 35d], range.Values);
        Assert.Equal([2], range.Shape);
        Assert.Equal(UtcDate(2026, 1, 1), aggregatedTimes[0].GetDateTimeOffset());
        Assert.Equal(UtcDate(2026, 2, 1), aggregatedTimes[1].GetDateTimeOffset());
    }

    [Fact]
    public void Aggregate_Yearly_UsesCalendarYears()
    {
        var times = new[]
        {
            UtcDate(2025, 12, 31),
            UtcDate(2026, 1, 1),
            UtcDate(2026, 6, 1)
        };

        var coverage = CreateCoverage(axisNames: ["t"], shape: [3], values: [10, 20, 30], times);

        Aggregate(coverage, "mean", "P1Y");

        var range = coverage.Ranges["value"];
        var aggregatedTimes = coverage.Domain.Axes["t"].Values!;

        Assert.Equal([10d, 25d], range.Values);
        Assert.Equal([2], range.Shape);
        Assert.Equal(UtcDate(2025, 1, 1), aggregatedTimes[0].GetDateTimeOffset());
        Assert.Equal(UtcDate(2026, 1, 1), aggregatedTimes[1].GetDateTimeOffset());
    }

    [Fact]
    public void Aggregate_TwoMonthPeriod_GroupsCalendarMonths()
    {
        var times = new[]
        {
            UtcDate(2026, 1, 10),
            UtcDate(2026, 2, 10),
            UtcDate(2026, 3, 10),
            UtcDate(2026, 4, 10)
        };

        var coverage = CreateCoverage(axisNames: ["t"], shape: [4], values: [10, 20, 30, 40], times);

        Aggregate(coverage, "sum", "P2M");

        var range = coverage.Ranges["value"];
        var aggregatedTimes = coverage.Domain.Axes["t"].Values!;

        Assert.Equal([30d, 70d], range.Values);
        Assert.Equal([2], range.Shape);
        Assert.Equal(UtcDate(2026, 1, 1), aggregatedTimes[0].GetDateTimeOffset());
        Assert.Equal(UtcDate(2026, 3, 1), aggregatedTimes[1].GetDateTimeOffset());
    }

    /// <summary>
    /// Creates a UTC date and time at midnight for the specified year, month, and day.
    /// </summary>
    /// <param name="year">The year of the date.</param>
    /// <param name="month">The month of the date (1 through 12).</param>
    /// <param name="day">The day of the month (1 through 31).</param>
    /// <returns>A <see cref="DateTimeOffset"/> representing the specified date in UTC at midnight.</returns>
    private static DateTimeOffset UtcDate(int year, int month, int day) => new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Performs an aggregation operation on the given coverage data using the specified method and duration.
    /// </summary>
    /// <param name="coverage">The coverage data to be aggregated. It contains the domain, parameters, ranges, and other metadata.</param>
    /// <param name="method">The aggregation method to be applied, such as "mean", "min", "max", "sum", or "count".</param>
    /// <param name="duration">The duration over which the aggregation should be calculated, specified in ISO 8601 duration format (e.g., "PT1H" for one hour).</param>
    private static void Aggregate(Coverage coverage, string method, string duration)
    {
        var aggregateMethod = typeof(QueryService).GetMethod("Aggregate", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(aggregateMethod);

        aggregateMethod.Invoke(null, [coverage, new CoverageConfiguration { AggregateFunction = method, AggregateDuration = duration }]);
    }

    /// <summary>
    /// Generates an array of <see cref="DateTimeOffset"/> values based on the provided minutes, starting from a fixed starting point.
    /// </summary>
    /// <param name="minutes">An array of integer values representing the minute offsets from the starting point.</param>
    /// <returns>An array of <see cref="DateTimeOffset"/> objects calculated by adding the specified minute offsets to a predefined starting time.</returns>
    private static DateTimeOffset[] TimesMinutes(params int[] minutes)
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        return [.. minutes.Select(minute => start.AddMinutes(minute))];
    }

    /// <summary>
    /// Modifies the structure of a given <see cref="Coverage"/> object by selecting specific axes and/or ranges.
    /// </summary>
    /// <param name="coverage">The <see cref="Coverage"/> object to be reshaped.</param>
    /// <param name="axes">An optional collection of axis names to retain in the coverage. If null, all axes are kept.</param>
    /// <param name="ranges">An optional name of the range to retain within the coverage. If null, all ranges are kept.</param>
    private static void Shape(Coverage coverage, IEnumerable<string>? axes = null, IEnumerable<string>? ranges = null)
    {
        var method = typeof(QueryService).GetMethod("Shape", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        method.Invoke(null, [coverage, new CoverageConfiguration { SelectAxes = axes?.ToList(), SelectRanges = ranges?.ToList() }]);
    }

    /// <summary>
    /// Creates a new coverage object with the specified axes, shape, values, and time information.
    /// </summary>
    /// <param name="axisNames">The names of the axes that define the coverage, such as "t" or "x".</param>
    /// <param name="shape">The shape of the coverage data, representing the number of elements for each axis.</param>
    /// <param name="values">The values associated with the coverage's ranges.</param>
    /// <param name="times">The array of time values corresponding to the "t" axis, represented as DateTimeOffset instances.</param>
    /// <returns>A new Coverage object initialized with the provided metadata, domain, and range values.</returns>
    private static Coverage CreateCoverage(List<string> axisNames, List<int> shape, double[] values, DateTimeOffset[] times)
    {
        return new Coverage
        {
            Type = "Coverage",
            Id = "test",
            Domain = new CoverageDomain
            {
                Type = "Domain",
                DomainType = "Grid",
                Axes = new Dictionary<string, CoverageAxis>
                {
                    ["t"] = new CoverageAxis
                    {
                        Name = "t",
                        Values = [.. times.Select(time => JsonSerializer.SerializeToElement(time))]
                    },
                    ["x"] = new CoverageAxis
                    {
                        Name = "x",
                        Values = [JsonSerializer.SerializeToElement(0), JsonSerializer.SerializeToElement(1)]
                    }
                }
            },
            Parameters = new Dictionary<string, CoverageParameter>(),
            Ranges = new Dictionary<string, CoverageRange>
            {
                ["value"] = new CoverageRange { Name = "value", AxisNames = axisNames, Shape = shape, Values = [.. values] }
            }
        };
    }

    /// <summary>
    /// Generates an array of DateTimeOffset instances, starting from a defined reference point, with each instance offset by the specified number of hours.
    /// </summary>
    /// <param name="hours">An array of integers representing the number of hours to offset from the reference start time for each DateTimeOffset instance.</param>
    /// <returns>An array of DateTimeOffset objects, each offset by the specified hours from the reference starting point.</returns>
    private static DateTimeOffset[] Times(params int[] hours)
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        return [.. hours.Select(hour => start.AddHours(hour))];
    }

    /// <summary>
    /// Extracts a subset of the given coverage data by slicing it along the time dimension
    /// using the specified start and end time boundaries.
    /// </summary>
    /// <param name="coverage">The coverage data to slice, containing the domain, parameters, ranges, and metadata.</param>
    /// <param name="start">The start time boundary for the slice. If null, slicing starts from the beginning of the time domain.</param>
    /// <param name="end">The end time boundary for the slice. If null, slicing continues to the end of the time domain.</param>
    private static void Slice(Coverage coverage, DateTimeOffset? start, DateTimeOffset? end)
    {
        var method = typeof(QueryService).GetMethod("Slice", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        method.Invoke(null, [coverage, new CoverageConfiguration { SliceBegin = start, SliceEnd = end }]);
    }
}
