using DigitaleDelta.ODataTranslator.Helpers;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace DigitaleDelta.ODataTranslator.Tests;

public class CrsHelperTests
{
    [Fact]
    public void TransformGeometry_Etrs89ToRdNew_TransformsAllCoordinates()
    {
        const string wkt =
            "MULTIPOLYGON (" +
            "((4.9000 52.3700, 4.9100 52.3700, 4.9100 52.3800, " +
            "4.9000 52.3800, 4.9000 52.3700)), " +
            "((4.9200 52.3900, 4.9300 52.3900, 4.9300 52.4000, " +
            "4.9200 52.4000, 4.9200 52.3900))" +
            ")";

        var original = new WKTReader().Read(wkt);

        var (success, transformed) =
            CrsHelper.TransformGeometry(4258, 28992, original);

        Assert.True(success);
        Assert.NotNull(transformed);
        Assert.IsType<MultiPolygon>(transformed);

        Assert.Equal(original.NumPoints, transformed.NumPoints);

        foreach (var (source, target) in
                 original.Coordinates.Zip(transformed.Coordinates))
        {
            Assert.NotEqual(source.X, target.X);
            Assert.NotEqual(source.Y, target.Y);
        }
    }
}
